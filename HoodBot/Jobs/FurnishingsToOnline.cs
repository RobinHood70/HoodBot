namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
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

		#region Constructors
		[JobInfo("Furnishings to Online")]
		public FurnishingsToOnline(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			PageCollection files = new(this.Site);
			files.PageLoaded += FilesPageLoaded;
			files.GetBacklinks("Template:Furnishing Summary", BacklinksTypes.EmbeddedIn);
			files.PageLoaded -= FilesPageLoaded;

			foreach (var item in Unknowns)
			{
				Debug.WriteLine(item);
			}
		}

		protected override void Main() => this.SavePages("Create Furniture Summary page", false, FilesPageLoaded);
		#endregion

		#region Private Methods
		private static void FilesPageLoaded(object sender, Page page)
		{
			ContextualParser originalParser = new(page);
			SortedDictionary<string, string> skills = new(StringComparer.Ordinal);
			SortedDictionary<string, string> materials = new(StringComparer.Ordinal);
			if (originalParser.FindTemplate("Furnishing Summary") is SiteTemplateNode originalTemplate)
			{
				ContextualParser parser = new(page, string.Empty);
				parser.Nodes.Add(parser.Factory.TemplateNodeFromParts("Minimal"));
				var template = parser.Factory.TemplateNodeFromParts("Online Furnishing Summary");
				string? stylemat = null;
				string? stylematcount = null;
				foreach (var (key, value) in originalTemplate.Parameters.ToKeyValue())
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
							template.Add(key, value);
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
							template.Add(key, value);
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
							materials[MaterialsLookup[key]] = value;
							break;
						case "alchemy":
						case "blacksmithing":
						case "clothing":
						case "enchanting":
						case "jewelry":
						case "provisioning":
						case "woodworking":
							// Move into catchall `skills` parameter.
							skills[SkillsLookup[key]] = value;
							break;
						case "stylemat":
							stylemat = value;
							break;
						case "stylematcount":
							stylematcount = value;
							break;
						case "othermat1":
						case "othermat2":
						case "othermat3":
						case "othermat4":
						case "othermats":
							var otherMatch = OtherMat.Match(value);
							if (otherMatch.Success)
							{
								var otherMaterial = otherMatch.Groups["material"].Value;
								var otherCount = otherMatch.Groups["count"].Value;
								try
								{
									SiteLink siteLink = SiteLink.FromText(page.Site, otherMaterial);
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
							template.Add(key, value);
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
						case "first":
						case "houses":
						case "note":
						case "other":
						case "questalt":
						case "vendor":
						case "vendorap":
						case "vendorcg":
						case "vendorcgpreview":
						case "vendorcity":
						case "vendorcity2":
						case "vendorcity3":
						case "vendorcityap":
						case "vendorcrowns":
						case "vendorgold":
						case "vendorother":
						case "vendorother2":
						case "vendorother3":
						case "vendortv":
						case "vendorwv":
							// Body text (in theory, no actual body text defined yet...someone tell me what to do.
							template.Add(key, value);
							break;
						case "achievecat":
						case "achievecat2":
						case "achievementalt":
						case "luxury":
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
			}
		}
		#endregion
	}
}
