namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class FurnishingsToOnline : EditJob
	{
		#region Static Fields
		private static readonly SortedSet<string> Unknowns = new(StringComparer.Ordinal);
		private static readonly Dictionary<string, string> MaterialsLookup = new(StringComparer.OrdinalIgnoreCase)
		{
			["bast"] = "Bast",
			["clean pelt"] = "Clean Pelt",
			["heartwood"] = "Heartwood",
			["rune"] = "Mundane Rune",
			["ochre"] = "Ochre",
			["pelt"] = "Clean Pelt",
			["regulus"] = "Regulus",
			["resin"] = "Alchemical Resin",
			["wax"] = "Decorative Wax",
		};

		private static readonly Dictionary<string, string> SkillsLookup = new(StringComparer.OrdinalIgnoreCase)
		{
			["alchemy"] = "Solvent Proficiency",
			["blacksmithing"] = "Metalworking",
			["clothing"] = "Tailoring",
			["enchanting"] = "Potency Improvement",
			["jewelry"] = "Engraver",
			["provisioning"] = "Recipe Improvement",
			["woodworking"] = "Woodworking (skill)",
		};

		private static readonly Regex OtherMat = new(@"\s*(?<material>.*?)\s*\((?<count>\d+)\)", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly PageCollection filePages;
		private readonly Dictionary<Title, string> nameLookup = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("Furnishings to Online")]
		public FurnishingsToOnline(JobManager jobManager)
			: base(jobManager)
		{
			this.filePages = new PageCollection(this.Site);
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			File.Delete(UespSite.GetBotDataFolder("Furnishing Moves.txt"));
			TitleCollection oldTitles = new(this.Site, this.Pages);
			oldTitles.GetBacklinks("Template:Furnishing Summary", BacklinksTypes.EmbeddedIn);
			TitleCollection newTitles = new(this.Site);
			Dictionary<Title, Title> reverse = new();
			foreach (var title in oldTitles)
			{
				var pageName = title.PageName
					.Replace("ON-", string.Empty, StringComparison.Ordinal)
					.Replace("furnishing-", string.Empty, StringComparison.Ordinal)
					.Replace("item-", string.Empty, StringComparison.Ordinal)
					.Replace(".jpg", string.Empty, StringComparison.Ordinal)
					.Replace(".png", string.Empty, StringComparison.Ordinal);
				if (pageName.EndsWith(')'))
				{
					Debug.WriteLine("Disambiguation: " + title.FullPageName);
				}

				this.nameLookup.Add(title, pageName);
				Title newTitle = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], pageName);
				newTitles.Add(newTitle);
				reverse.Add(newTitle, title);
			}

			var newPages = newTitles.Load(PageModules.Info);
			newPages.Sort();
			foreach (var page in newPages)
			{
				if (page.Exists)
				{
					var filePage = reverse[page];
					this.nameLookup[filePage] += " (furnishing)";
				}
			}

			PageCollection files = new(this.Site);
			files.PageLoaded += this.FilesPageLoaded;
			files.GetBacklinks("Template:Furnishing Summary", BacklinksTypes.EmbeddedIn);
			files.PageLoaded -= this.FilesPageLoaded;

			if (Unknowns.Count > 0)
			{
				foreach (var item in Unknowns)
				{
					Debug.WriteLine(item);
				}

				throw new InvalidOperationException();
			}
		}

		protected override void Main()
		{
			this.StatusWriteLine("Creating Pages");
			this.Pages.RemoveChanged(false);
			this.SavePages("Create Furniture Summary page", false, this.FilesPageLoaded);

			SaveInfo fileSaveInfo = new("Check for Existing Furnishings", true);
			this.filePages.RemoveChanged(false);
			this.SavePages(this.filePages, "Removing Furnishing Summaries", fileSaveInfo, null);
		}
		#endregion

		#region Private Static Methods
		private static string RemoveTemplate(NodeCollection nodes, int templateIndex)
		{
			nodes.RemoveAt(templateIndex);
			var placeHolder = nodes.FindIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals("Placeholder"));
			if (placeHolder != -1)
			{
				nodes.RemoveAt(placeHolder);
				nodes.Insert(0, nodes.Factory.TextNode("<noinclude>{{Prod|Unneeded Placeholder|bot=1}}\n</noinclude>"));
			}

			var text = nodes.ToRaw();
			return Regex.Replace(text, @"==\s*Summary\s*==\s*\n(?===|\Z|\[\[Category:)", string.Empty, RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		}
		#endregion

		#region Private Methods
		private void FilesPageLoaded(object sender, Page page)
		{
			if (page.Namespace != MediaWikiNamespaces.File || page.PageName.Contains("-crown store-", StringComparison.Ordinal))
			{
				return;
			}

			ContextualParser originalParser = new(page);
			SortedDictionary<string, string> skills = new(StringComparer.Ordinal);
			SortedDictionary<string, string> materials = new(StringComparer.Ordinal);
			var templateIndex = originalParser.FindIndex<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("Furnishing Summary"));
			if (templateIndex > -1)
			{
				var originalTemplate = (SiteTemplateNode)originalParser[templateIndex];
				var collectible = originalTemplate.Find("collectible");
				var originalParams = originalTemplate.Parameters.ToKeyValue();

				page.Text = RemoveTemplate(originalParser, templateIndex);
				this.filePages.Add(page);
				ContextualParser parser = new(page, string.Empty);
				var newTemplate = (SiteTemplateNode)parser.Factory.TemplateNodeFromParts("Online Furnishing Summary\n");
				parser.Add(parser.Factory.TemplateNodeFromParts("Minimal"));
				parser.Add(newTemplate);
				var autoPagename = "ON-" + (collectible is null ? "item-" : string.Empty) + "furnishing-";
				string? stylemat = null;
				string? stylematcount = null;
				if (page.PageName.EndsWith(".png", StringComparison.Ordinal) || !page.PageName.Contains(autoPagename, StringComparison.Ordinal))
				{
					newTemplate.Add("image", page.PageName, ParameterFormat.OnePerLine);
				}

				foreach (var (key, value) in originalParams)
				{
					switch (key)
					{
						case "bindtype":
						case "cat":
						case "collectible":
						case "craft":
						case "desc":
						case "id":
						case "priceap":
						case "pricecg":
						case "pricecrowns":
						case "priceet":
						case "pricegold":
						case "pricetv":
						case "pricewv":
						case "quality":
						case "recipeid":
						case "recipequality":
						case "size":
						case "subcat":
						case "type":
							// Straight copy (`bindtype` and `type` are not in the old template but leftovers from testing the new template):
							newTemplate.Add(key, value, ParameterFormat.OnePerLine);
							break;
						case "creature":
						case "interactable":
						case "light":
						case "lightcolor":
						case "master":
						case "nickname":
						case "readable":
						case "sittable":
						case "titlename":
						case "visualfx":
							// Straight copy, but currently unused (needs template programming):
							newTemplate.Add(key, value, ParameterFormat.OnePerLine);
							break;
						case "bast":
						case "clean pelt":
						case "heartwood":
						case "ochre":
						case "pelt":
						case "regulus":
						case "resin":
						case "rune":
						case "wax":
							// Move into catchall `materials` parameter:
							materials[MaterialsLookup[key]] = value.Trim();
							break;
						case "alchemy":
						case "blacksmithing":
						case "clothing":
						case "enchanting":
						case "jewelry":
						case "provisioning":
						case "woodworking":
							// Move into catchall `skills` parameter.
							skills[SkillsLookup[key]] = value.Trim();
							break;
						case "stylemat":
							stylemat = value.Trim();
							break;
						case "stylematcount":
							stylematcount = value.Trim();
							break;
						case "othermat1":
						case "othermat2":
						case "othermat3":
						case "othermat4":
						case "othermats":
							var otherMatch = OtherMat.Match(value.Trim());
							if (otherMatch.Success)
							{
								var otherMaterial = otherMatch.Groups["material"].Value;
								var otherCount = otherMatch.Groups["count"].Value;
								try
								{
									var siteLink = SiteLink.FromText(page.Site, otherMaterial);
									var otherKey = siteLink.Text ?? siteLink.PageName;
									materials[otherKey] = otherCount;
								}
								catch (ArgumentException)
								{
									materials[otherMaterial] = otherCount;
								}
							}

							break;
						case "animated":
						case "audible":
						case "crime":
						case "fish":
						case "harvest":
						case "loot":
						case "quest":
							// Straight copy, used for categorization only in template. Also used in body text:
							newTemplate.Add(key, value, ParameterFormat.OnePerLine);
							break;
						case "achievement":
						case "book1":
						case "book2":
						case "book3":
						case "book4":
						case "book5":
						case "book6":
						case "book7":
						case "book8":
						case "book9":
						case "book10":
						case "book11":
						case "book12":
						case "book13":
						case "book14":
						case "book15":
						case "book16":
						case "book17":
						case "book18":
						case "book19":
						case "book20":
						case "book21":
						case "book22":
						case "book23":
						case "book24":
						case "book25":
						case "book26":
						case "book27":
						case "book28":
						case "book29":
						case "book30":
						case "book31":
						case "book32":
						case "book33":
						case "book34":
						case "book35":
						case "book36":
						case "bookcollection":
						case "bound":
						case "first":
						case "houses":
						case "itemicon":
						case "name":
						case "note":
						case "other":
						case "questalt":
						case "skills":
						case "source":
						case "tags":
						case "vendor":
						case "vendorap":
						case "vendorcg":
						case "vendorcgpreview":
						case "vendorcity":
						case "vendorcity2":
						case "vendorcity3":
						case "vendorcityap":
						case "vendorcrowns":
						case "vendoret":
						case "vendorgold":
						case "vendorother":
						case "vendorother2":
						case "vendorother3":
						case "vendortv":
						case "vendorwv":
							// Body text (in theory, no actual body text defined yet...someone tell me what to do.
							newTemplate.Add(key, value, ParameterFormat.OnePerLine);
							break;
						case "achievecat":
						case "achievecat2":
						case "achievementalt":
						case "luxury":
						case "materials":
						case "nocat":
						case "questcat":
						case "recipename":
						case "species":
						case "style":
						case "trainingdummy":
							// Unused and will be discarded if nothing else changes (let me know if you want a list of values and where they're used):
							break;
						default:
							Debug.WriteLine($"Unknown \"{key}\" on page: {page.FullPageName}");
							Unknowns.Add(key ?? string.Empty);
							break;
					}
				}

				if (stylemat != null)
				{
					materials[stylemat] = stylematcount ?? throw new InvalidOperationException();
				}
				else if (stylematcount != null)
				{
					throw new InvalidOperationException();
				}

				var pageName = this.nameLookup[page];
				File.AppendAllText(UespSite.GetBotDataFolder("Furnishing Moves.txt"), $"{page.FullPageName}\t{pageName}\t{WikiTextVisitor.Raw(originalTemplate)}~\n");
				var newPage = this.Site.CreatePage(UespNamespaces.Online, pageName, parser.ToRaw());
				this.Pages.Add(newPage);
			}
		}
		#endregion
	}
}
