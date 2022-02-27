namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class EsoCollections : EditJob
	{
		#region Private Constants
		private const string TemplateName = "Online Collectible Summary";
		#endregion

		#region Static Fields
		private static readonly Regex DescriptionFinder = new(@":\s*''", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Regex DescriptionReplacer = new(@":\s*''.+?''\s*\n?", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Dictionary<string, string> NameSubstitutes = new(StringComparer.OrdinalIgnoreCase)
		{
			["Body Markings:Dwemer Body Marking"] = "Body Markings:Dwemer Body Markings",
			["Body Markings:Ysgramor's Chosen Body Marking"] = "Body Markings:Ysgramor's Chosen Body Markings",
			["Hats:Dibella's Doll Mask, Human/Elf"] = "Hats:Dibella's Doll Mask, Human / Elf",
			["Head Markings:Dwemer Face Marking"] = "Head Markings:Dwemer Face Markings",
			["Major Adornments:Fancy Eyepatch"] = "Major Adornments:Fancy Eye Patch",
			["Major Adornments:Midyear Victor's Laurel Wreath"] = "Major Adornments:Mayhem Victor's Laurel Wreath",
			["Minor Adornments:No Minor Adornments"] = "Minor Adornments:No Minor Adornment",
			["Minor Adornments:Sabre Cat Ear Spike"] = "Minor Adornments:Sabre Cat Ear-Fang",
			["Pets:Silver Rose Senche-Serval"] = "Mounts:Silver Rose Senche-Serval",
			["Polymorphs:Wraith of Crows"] = "Polymorphs:Wraith-of-Crows",
			["Skins:Arctic Rime"] = "Skins:Arctic Rime Skin",
		};
		#endregion

		#region Fields
		private readonly Dictionary<string, List<string>> crateTiers = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, DbCollectible> dbCollectibles = new(StringComparer.OrdinalIgnoreCase);
		private string? blankText;
		#endregion

		#region Constructors
		[JobInfo("Create Collectibles", "ESO Update")]
		public EsoCollections(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "ESO Collections";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Loading blank template page");
			this.blankText = this.GetPageTemplate();

			this.StatusWriteLine("Getting collectibles from database");
			this.GetCollectiblesFromDatabase();

			this.StatusWriteLine("Getting crown crates from wiki");
			this.GetCrownCrates();

			this.StatusWriteLine("Loading collections");
			var titles = this.LoadCollections();

			this.StatusWriteLine("Loading list pages");
			this.Pages.PageLoaded += this.PageLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.PageLoaded;
		}

		protected override void Main() => this.SavePages("Update collections");
		#endregion

		#region Private Static Methods
		private static string CategorySingular(string category) => category switch
		{
			"Allies" => "Ally",
			"Non-Combat Pets" => "Pet",
			"Personalities" => "Personality",
			"Stories" => "Story",
			"Undaunted Trophies" => "Undaunted Trophy",
			_ => category.TrimEnd('s'),
		};

		private static Page FindPage(PageCollection pages, CollectibleInfo newPage)
		{
			if ((pages.TryGetValue(newPage.DisambigName, out var page) && page.Exists) ||
				(pages.TryGetValue(newPage.PageName, out page) && page.Exists))
			{
				ContextualParser? parser = new(page);
				if (parser.Nodes.Has<SiteTemplateNode>(node => node.TitleValue.PageNameEquals(TemplateName)))
				{
					return page;
				}
			}

			return
				(pages.TryGetValue(newPage.PageName, out page) && (!page.Exists || page.IsRedirect)) ||
				(pages.TryGetValue(newPage.DisambigName, out page) && (!page.Exists || page.IsRedirect))
					? page
					: throw new InvalidOperationException($"Both {newPage.PageName} and {newPage.DisambigName} are in use and not collectible pages.");
		}

		private static string GetLookupName(string catName, Section section)
		{
			var header = section.Header!;
			var name = GetSectionTitle(header);
			var localCat = catName;
			if (localCat[^2] == ' ')
			{
				localCat = localCat[0..^2];
			}

			localCat = localCat switch
			{
				"Hats (collectible)" => "Hats",
				"Mementos" => name switch
				{
					"Breda's Bottomless Mead Mug" => "Tools",
					"Fire Rock" => "Tools",
					"Jubilee Cake 2021" => "Tools",
					"The Pie of Misrule" => "Tools",
					"Witchmother's Whistle" => "Tools",
					_ => "Mementos"
				},
				_ => localCat,
			};

			return NameSubstitutes.Substitute(localCat + ':' + name);
		}

		private static string GetSectionTitle(IHeaderNode header)
		{
			StringBuilder sb = new();
			foreach (var node in header.Title)
			{
				switch (node)
				{
					case ITextNode text:
						sb.Append(text.Text);
						break;
					case SiteLinkNode link:
						sb.Append(WikiTextVisitor.Raw(link.Parameters)[1..]);
						break;
					case SiteTemplateNode template:
						sb.Append(TrimHeader(template));
						break;
				}
			}

			return sb
				.ToString()
				.Trim()
				.Trim(TextArrays.EqualsSign)
				.Trim();
		}

		private static void ParseCollectible(CollectibleInfo collectible, Page page)
		{
			ContextualParser parser = new(page);
			var templateIndex = parser.Nodes.FindIndex<SiteTemplateNode>(template => template.TitleValue.PageNameEquals(TemplateName));
			SiteTemplateNode template = (SiteTemplateNode)parser.Nodes[templateIndex];
			var removeParens = page.PageName.Split(" (", 2)[0];
			template.Update("collectibletype", CategorySingular(collectible.CollectibleType));
			template.Update("type", CategorySingular(collectible.Type));
			if ((template.Find("image") ?? template.Add("image")) is IParameterNode parameter)
			{
				if (parameter.Value.ToRaw().Trim().Length == 0)
				{
					parameter.SetValue(collectible.Image ?? string.Empty);
					if (collectible.ImageDescription != null && !page.PageNameEquals(collectible.ImageDescription))
					{
						template.UpdateIfEmpty("imgdesc", collectible.ImageDescription ?? string.Empty);
					}
				}
			}

			template.UpdateIfEmpty("icon", collectible.Icon ?? string.Empty);
			template.Update("id", collectible.Id.ToStringInvariant());
			template.UpdateIfEmpty("description", collectible.Description);
			template.Update("name", collectible.NickName);
			if (collectible.Acquisition != null)
			{
				template.UpdateIfEmpty("acquisition", collectible.Acquisition);
			}

			if (collectible.Crates is not null)
			{
				var crates = string.Join(", ", collectible.Crates);
				template.Update("crate", crates);
			}

			if (collectible.Tier is not null)
			{
				template.Update("tier", collectible.Tier);
			}

			if (collectible.Price is not null)
			{
				var price = template.Find("price") ?? template.Add("price", string.Empty);
				var value = price.Value;
				if (value.Count == 1 && value[0] is ITextNode text && text.Text.Trim().Length == 0)
				{
					value.Clear();
					value.AddRange(collectible.Price);
					value.AddText("\n");
				}
			}

			if (page.IsRedirect || !page.Exists)
			{
				parser.Nodes.InsertRange(templateIndex + 1, collectible.NewContent);
			}

			page.Text = parser.ToRaw();
		}

		private static string TrimHeader(SiteTemplateNode template) => template.TitleValue.PageName switch
		{
			"Anchor" or "Item Link" => template.GetValue(1) ?? string.Empty,
			"ESO Quality Color" => template.GetValue(2) ?? string.Empty,
			_ => throw new InvalidOperationException(),
		};

		private static bool ValidateSection(Section section) =>
			section.Content is var content
			&& content.Has<ITextNode>(text => DescriptionFinder.IsMatch(text.Text))
			&& (content.Has<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("Icon"))
				|| content.Has<SiteLinkNode>(link => link.TitleValue.Namespace == MediaWikiNamespaces.File));
		#endregion

		#region Private Methods
		private void GetCollectiblesFromDatabase()
		{
			var queryResults = DbCollectible.RunQuery();
			this.dbCollectibles.AddRange(queryResults);
		}

		private void GetCrownCrates()
		{
			PageCollection crownCrates = new(this.Site);
			crownCrates.GetCategoryMembers("Online-Crown Crates");
			foreach (var crate in crownCrates)
			{
				ContextualParser parser = new(crate);
				var tier = string.Empty;
				foreach (var node in parser.Nodes)
				{
					if (node is IHeaderNode header)
					{
						tier = GetSectionTitle(header);
					}
					else if (node is SiteTemplateNode template && template.TitleValue.PageNameEquals("ESO Crate Card List"))
					{
						foreach (var parameter in template.ParameterCluster(2))
						{
							var title = parameter[0].Value.ToRaw();
							if (!this.crateTiers.TryGetValue(title, out var allTiers))
							{
								allTiers = new List<string>();
								this.crateTiers.Add(title, allTiers);
							}

							// allTiers.Add($"[[Online:{crate.PageName}#{tier}|{crate.PageName}]]");
							allTiers.Add(crate.PageName);
						}
					}
				}
			}
		}

		private string GetPageTemplate()
		{
			var retval = this.Site.LoadPageText("Template:" + TemplateName + "/Blank");
			var nowiki = retval
				.NotNull(ValidationType.Value, nameof(retval))
				.IndexOf("<nowiki>", StringComparison.Ordinal);
			if (nowiki >= 0)
			{
				nowiki += 8;
				var lastNowiki = retval.LastIndexOf("</nowiki>", StringComparison.Ordinal);
				retval = retval[nowiki..lastNowiki].Trim();
			}

			return retval;
		}

		private PageCollection GetSectionPages(IEnumerable<CollectibleInfo> collectibles)
		{
			TitleCollection loadTitles = new(this.Site);
			foreach (var collectible in collectibles)
			{
				loadTitles.Add(collectible.PageName);
				loadTitles.Add(collectible.DisambigName);
			}

			var pages = loadTitles.Load();
			return pages;
		}

		private TitleCollection LoadCollections()
		{
			TitleCollection titles = new(this.Site);
			titles.GetCategoryMembers("Online-Items-Collections");
			titles.Remove("Online:Fragments");
			titles.Remove("Online:Nascent Indrik");
			titles.Remove("Online:Unstable Morpholith");
			return titles;
		}

		private void ParseCollectibles(IEnumerable<CollectibleInfo> collectibles)
		{
			var pages = this.GetSectionPages(collectibles);
			foreach (var collectible in collectibles)
			{
				var newPage = FindPage(pages, collectible);
				if (newPage.IsRedirect || !newPage.Exists)
				{
					newPage.Text = this.blankText;
				}

				ParseCollectible(collectible, newPage);
				this.Pages.Add(newPage);

				if (collectible.AssociatedSection.Header is IHeaderNode header)
				{
					var equalsSigns = new string('=', header.Level);
					var title = header.Title;
					title.Clear();
					title.AddText(equalsSigns);
					title.Add(title.Factory.LinkNodeFromParts(newPage.FullPageName, collectible.Name));
					title.AddText(equalsSigns);
				}
			}
		}

		private void PageLoaded(object sender, Page page)
		{
			ContextualParser parser = new(page);
			var sectionInfo = this.ParseSections(page, parser);

			if (sectionInfo.Collectibles.Count == 0)
			{
				Debug.WriteLine($"* {page.AsLink(true)} does not appear to be a list page.");
				return;
			}

			this.ReportInvalidSections(page, sectionInfo.InvalidHeaders);
			this.ReportNotFoundSections(page, sectionInfo.NotFound);
			this.ParseCollectibles(sectionInfo.Collectibles);
			parser.FromSections(sectionInfo.Sections);
			page.Text = parser.ToRaw();
		}

		private SectionInfo ParseSections(Page page, ContextualParser parser)
		{
			SectionInfo sectionInfo = new();
			var sections = parser.ToSections();
			foreach (var section in sections)
			{
				sectionInfo.Sections.Add(section);
				if (section.Header is not null &&
					(section.Content.Count != 1 ||
					section.Content[0] is not ITextNode textNode ||
					textNode.Text.Trim().Length != 0))
				{
					if (ValidateSection(section))
					{
						var lookup = GetLookupName(page.BasePageName, section);
						if (this.dbCollectibles.TryGetValue(lookup, out var collectible))
						{
							CollectibleInfo templateInfo = new(this.Site, collectible, lookup.Split(TextArrays.Colon, 2)[1], section, this.crateTiers);
							sectionInfo.Collectibles.Add(templateInfo);
						}
						else
						{
							sectionInfo.NotFound.Add(section.Header);
						}
					}
					else
					{
						sectionInfo.InvalidHeaders.Add(section.Header);
					}
				}
			}

			return sectionInfo;
		}

		private void ReportInvalidSections(Page page, List<IHeaderNode> invalidHeaders)
		{
			if (invalidHeaders.Count > 0)
			{
				this.WriteLine($"* {page.AsLink(true)} has invalid sections:");
				foreach (var header in invalidHeaders)
				{
					this.WriteLine(':' + header.GetInnerText(true));
				}
			}
		}

		private void ReportNotFoundSections(Page page, List<IHeaderNode> notFound)
		{
			if (notFound.Count > 0)
			{
				this.WriteLine($"* {page.AsLink(true)} has sections that could not be matched with anything in the database:");
				foreach (var header in notFound)
				{
					this.WriteLine(':' + header.GetInnerText(true));
				}
			}
		}
		#endregion

		#region Private Classes
		private sealed class CollectibleInfo
		{
			#region Constructors
			internal CollectibleInfo(Site site, DbCollectible dbData, string sectionName, Section section, IDictionary<string, List<string>> tierList)
			{
				this.CollectibleType = dbData.Category;
				this.Type = dbData.Subcategory;
				this.PageName = "Online:" + dbData.Name;
				this.Description = dbData.Description;
				this.Id = dbData.Id;
				this.NickName = dbData.NickName;
				this.Name = dbData.Name;
				if (tierList.TryGetValue(sectionName, out var crateInfo) || tierList.TryGetValue(dbData.Name, out crateInfo))
				{
					this.Crates = crateInfo;
				}

				this.AssociatedSection = section;
				var content = section.Content;
				this.NewContent = new(content.Factory);
				var iconCount = 0;
				var iconOffset = 0;
				var fileCount = 0;
				var fileOffset = 0;
				for (var i = 0; i < content.Count; i++)
				{
					var node = content[i];
					switch (node)
					{
						case ITextNode text:
							this.NewContent.AddText(DescriptionReplacer.Replace(text.Text, string.Empty));
							break;
						case SiteLinkNode link when
							link.TitleValue.Namespace == MediaWikiNamespaces.File &&
							link.TitleValue.PageName.StartsWith("ON-icon-", StringComparison.Ordinal):
							iconCount++;
							iconOffset = this.NewContent.Count;
							this.NewContent.Add(node); // Will be removed later if appropriate
							break;
						case SiteLinkNode link when
							link.TitleValue.Namespace == MediaWikiNamespaces.File:
							fileCount++;
							fileOffset = this.NewContent.Count;
							this.NewContent.Add(node); // Will be removed later if appropriate
							break;
						case SiteTemplateNode template when template.TitleValue.PageNameEquals("ESO Crowns"):
							this.Acquisition = "Crown Store";
							this.Price = template;
							this.NewContent.Add(node);
							break;
						case SiteTemplateNode template when template.TitleValue.PageNameEquals("Icon"):
							iconCount++;
							iconOffset = this.NewContent.Count;
							this.NewContent.Add(node); // Will be removed later if appropriate
							break;
						case SiteTemplateNode template when
							template.TitleValue.PageNameEquals("NewLeft") ||
							template.TitleValue.PageNameEquals("NewLine"):
							// Do nothing
							break;
						default:
							this.NewContent.Add(node);
							break;
					}
				}

				if (fileCount == 1 && this.NewContent[fileOffset] is SiteLinkNode imageLink)
				{
					this.Image = imageLink.TitleValue.PageName[0..^4];
					SiteLink link = SiteLink.FromLinkNode(site, imageLink);
					this.ImageDescription = link.Text;
					this.NewContent.RemoveAt(fileOffset);
					if (iconOffset > fileOffset)
					{
						// Fugly hack, but I can't think of a better way around it without restructuring everything.
						iconOffset--;
					}
				}

				if (iconCount == 1)
				{
					var fileName = this.NewContent[iconOffset] switch
					{
						SiteTemplateNode iconTemplate => UespFunctions.IconAbbreviation("ON", iconTemplate),
						SiteLinkNode iconLink => iconLink.TitleValue.PageName,
						_ => throw new InvalidOperationException(),
					};
					fileName = fileName[0..^4];
					this.Icon = fileName;
					this.NewContent.RemoveAt(iconOffset);
				}

				this.NewContent.MergeText(true);
				this.NewContent.Trim();
				this.NewContent.Insert(0, this.NewContent.Factory.TextNode("\n"));
			}
			#endregion

			#region Public Properties
			public string? Acquisition { get; }

			public Section AssociatedSection { get; }

			public string CollectibleType { get; }

			public List<string>? Crates { get; }

			public string Description { get; }

			public string DisambigName => this.PageName + $" ({CategorySingular(this.CollectibleType).ToLowerInvariant()})";

			public string? Icon { get; }

			public long Id { get; }

			public string? Image { get; }

			public string? ImageDescription { get; }

			public string Name { get; }

			public NodeCollection NewContent { get; }

			public string NickName { get; }

			public string PageName { get; }

			public SiteTemplateNode? Price { get; }

			public string? Tier { get; }

			public string Type { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}

		private sealed class DbCollectible
		{
			#region Constructors
			internal DbCollectible(IDataRecord row)
			{
				this.Id = (long)row["id"];
				this.Name = (string)row["name"];
				this.NickName = (string)row["nickname"];
				this.Description = (string)row["description"];
				this.Category = (string)row["categoryName"];
				this.Subcategory = (string)row["subCategoryName"];

				var cat = this.Category switch
				{
					"Allies" => this.Subcategory,
					"Appearance" => this.Subcategory,
					"Furnishings" => this.Subcategory,
					"Non-Combat Pets" => "Pets",
					_ => this.Category
				};

				this.LookupName = cat + ':' + this.Name;

				// The following are duplicates within the same category, so are effectively removed by renaming them.
				switch (this.Id)
				{
					case 4993:
					case 6017:
					case 6117:
					case 6457:
						this.LookupName += $" ({this.Id})";
						break;
				}
			}
			#endregion

			#region Public Static Properties
			public static string Query { get; } = "SELECT id, name, nickname, description, categoryName, subCategoryName FROM collectibles ORDER BY name";
			#endregion

			#region Public Properties
			public string Category { get; }

			public string Description { get; }

			public long Id { get; }

			public string LookupName { get; }

			public string Name { get; }

			public string NickName { get; }

			public string Subcategory { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion

			#region Internal Methods
			internal static Dictionary<string, DbCollectible> RunQuery()
			{
				Dictionary<string, DbCollectible>? retval = new(StringComparer.Ordinal);
				foreach (var item in Database.RunQuery(EsoLog.Connection, Query, row => new DbCollectible(row)))
				{
					retval.Add(item.LookupName, item);
				}

				return retval;
			}
			#endregion
		}

		private sealed class SectionInfo
		{
			#region Public Properties
			public List<CollectibleInfo> Collectibles { get; } = new();

			public List<IHeaderNode> InvalidHeaders { get; } = new();

			public List<IHeaderNode> NotFound { get; } = new();

			public List<Section> Sections { get; } = new();
			#endregion
		}
		#endregion
	}
}
