namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoCollectibles : ParsedPageJob
	{
		#region Private Constants
		private const string TemplateName = "Online Collectible Summary";
		#endregion

		#region Fields
		private readonly Dictionary<Title, Collectible> collectibles = new(SimpleTitleComparer.Instance);
		private readonly Dictionary<string, List<string>> crateTiers = new(StringComparer.OrdinalIgnoreCase);
		private string? blankText;
		#endregion

		#region Constructors
		[JobInfo("Create Collectibles", "ESO Update")]
		public EsoCollectibles(JobManager jobManager)
			: base(jobManager)
		{
			this.CreateOnly = Tristate.True;
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "ESO Collectibles";
		#endregion

		#region Protected Override Properties

		protected override string EditSummary => "Create collectible page";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Getting wiki info");
			if (this.Site.LoadPage($"Template:{TemplateName}/Blank") is Page page)
			{
				var extract = new ContextualParser(page);
				var pre = extract.FindSiteTemplate("Pre");
				if (pre?.Find(1)?.Value is NodeCollection nodes &&
					nodes.Count > 0 &&
					nodes[0] is ITagNode tag &&
					string.Equals(tag.Name, "nowiki", StringComparison.Ordinal))
				{
					if (tag.InnerText?.Trim() is string blankTemplate)
					{
						this.blankText = blankTemplate;
					}
				}
			}

			this.blankText.ThrowNull();
			var allTitles = new TitleCollection(this.Site);
			allTitles.GetNamespace(UespNamespaces.Online);
			var pages = this.GetBacklinks();
			var knownIds = this.GetKnown(pages);
			this.GetCollectibles(allTitles, pages, knownIds);

			this.StatusWriteLine("Getting crown crates");
			this.GetCrownCrates();
		}

		protected override void LoadPages()
		{
			this.blankText.ThrowNull();
			foreach (var collectible in this.collectibles)
			{
				var page = this.Site.CreatePage(collectible.Key, this.blankText);
				this.Pages.Add(page);
				this.PageLoaded(this, page);
			}
		}

		protected override void ParseText(object sender, ContextualParser parser)
		{
			var page = parser.Page;
			parser.ReplaceText(
				"<gallery>\n</gallery>",
				$"<gallery>\nON-crown store-{page.PageName}.jpg\n</gallery>",
				StringComparison.Ordinal);
			var collectible = this.collectibles[page];
			if (collectible is null)
			{
				throw new InvalidOperationException();
			}

			if (this.crateTiers.TryGetValue(collectible.Name, out var crateInfo))
			{
				collectible.Crates.AddRange(crateInfo);
			}

			if (parser.FindSiteTemplate(TemplateName) is SiteTemplateNode template)
			{
				template.Update("collectibletype", CategorySingular(collectible.Category));
				template.Update("type", CategorySingular(collectible.Subcategory));
				template.UpdateIfEmpty("image", $"<!--{collectible.ImageName}-->");
				template.UpdateIfEmpty("icon", $"<!--{collectible.IconName}-->");
				template.Update("id", collectible.Id.ToStringInvariant());
				template.UpdateIfEmpty("description", collectible.Description);
				template.RenameParameter("name", "nickname");
				if (string.IsNullOrEmpty(collectible.NickName))
				{
					template.Remove("nickname");
				}
				else
				{
					template.Update("nickname", collectible.NickName);
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
			}
		}
		#endregion

		#region Private Static Methods
		private static (List<Collectible> Collectibles, HashSet<string> Duplicates) GetDBCollectibles()
		{
			HashSet<string> dupeNames = new(StringComparer.Ordinal);
			HashSet<string> singleNames = new(StringComparer.Ordinal);
			var retval = new List<Collectible>();
			foreach (var item in Database.RunQuery(EsoLog.Connection, Collectible.Query, row => new Collectible(row)))
			{
				// These IDs correspond to Mount and Pet. It's unknown what purpose these serve, but one way or another, they should not be created.
				if (item.Id is not 10387 and not 10388)
				{
					retval.Add(item);
					if (!singleNames.Add(item.Name))
					{
						dupeNames.Add(item.Name);
					}
				}
			}

			return (retval, dupeNames);
		}

		private static string CategorySingular(string category) => category switch
		{
			"Allies" => "Ally",
			"Non-Combat Pets" => "Pet",
			"Personalities" => "Personality",
			"Stories" => "Story",
			"Undaunted Trophies" => "Undaunted Trophy",
			_ => category.TrimEnd('s'),
		};
		#endregion

		#region Private Methods
		private void AddItem(Collectible item, Title title, TitleCollection allTitles, HashSet<long> knownIds, PageCollection pages)
		{
			Title titleDisambig = TitleFactory.FromValidated(title.Namespace, title.PageName + " (collectible)");
			if (!knownIds.Contains(item.Id) && !pages.Contains(title) && !pages.Contains(titleDisambig))
			{
				if (allTitles.Contains(title))
				{
					title = !allTitles.Contains(titleDisambig)
						? titleDisambig
						: throw new InvalidOperationException($"Couldn't find a usable page for {item.Name}.");
				}

				this.collectibles.Add(title, item);
			}
		}

		private PageCollection GetBacklinks()
		{
			var site = (UespSite)this.Site;
			var pages = site.CreateMetaPageCollection(PageModules.None, false, "id");
			pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);

			return pages;
		}

		private void GetCollectibles(TitleCollection allTitles, PageCollection pages, HashSet<long> knownIds)
		{
			this.StatusWriteLine("Getting collectibles from database");
			var (allCollectibles, dupeNames) = GetDBCollectibles();
			foreach (var item in allCollectibles)
			{
				var titleText = item.Name;
				if (dupeNames.Contains(item.Name))
				{
					var cat = item.Subcategory.Length == 0 ? item.Category : item.Subcategory;
					titleText += $" ({cat})";
				}

				this.AddItem(item, TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], titleText), allTitles, knownIds, pages);
			}
		}

		private void GetCrownCrates()
		{
			PageCollection crownCrates = new(this.Site);
			crownCrates.GetCategoryMembers("Online-Crown Crates");
			foreach (var crate in crownCrates)
			{
				this.ParseCrate(crate);
			}
		}

		private HashSet<long> GetKnown(PageCollection pages)
		{
			var knownIds = new HashSet<long>();
			foreach (var item in pages)
			{
				if (item is VariablesPage vPage)
				{
					var idText = vPage.GetVariable("id");
					if (long.TryParse(idText, NumberStyles.Integer, this.Site.Culture, out var id))
					{
						knownIds.Add(id);
					}
				}
			}

			return knownIds;
		}

		private void ParseCrate(Page crate)
		{
			ContextualParser parser = new(crate);
			//// var tier = string.Empty;
			foreach (var node in parser)
			{
				if (node is IHeaderNode)
				{
					//// tier = GetSectionTitle(header);
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
		#endregion

		#region Private Classes
		private sealed class Collectible
		{
			#region Constructors
			internal Collectible(IDataRecord row)
			{
				this.Id = (long)row["id"];
				this.Name = this.Id == 6117
					? "Honor Guard Jack"
					: (string)row["name"];
				this.NickName = (string)row["nickname"];
				this.Description = (string)row["description"];
				this.Category = (string)row["categoryName"];
				this.Subcategory = (string)row["subCategoryName"];
				var fileCategory = this.Category switch
				{
					"Appearance" => string.Equals(this.Subcategory, "Hair Style", StringComparison.Ordinal) ? "hairstyle" : this.Subcategory,
					"Memento" or "Mount" or "Pet" => this.Category,
					_ => this.Subcategory
				};

				fileCategory = fileCategory.ToLowerInvariant();
				this.ImageName = $"ON-{fileCategory}-{this.Name}";
				this.IconName = $"ON-icon-{fileCategory}-{this.Name}";
			}
			#endregion

			#region Public Static Properties
			public static string Query { get; } = "SELECT " +
					"id, " +
					"name, " +
					"nickname, " +
					"description, " +
					"categoryName, " +
					"subCategoryName " +
				"FROM collectibles " +
				"WHERE " +
					"categoryName NOT IN ('Armor Styles' , 'Emotes', 'Fragments', 'Furnishings', 'Housing', 'Stories', 'Weapon Styles') AND " +
					"subCategoryName != 'Companions' AND " +
					"referenceId != 0 AND " +
					"id NOT IN (248, 271, 272, 273, 274, 275, 276, 370, 371, 372, 1156, 1306, 1480, 8202)";
			#endregion

			#region Public Properties
			public string Category { get; }

			public List<string> Crates { get; } = new List<string>();

			public string Description { get; }

			public string IconName { get; }

			public long Id { get; }

			public string ImageName { get; }

			public string Name { get; }

			public NodeCollection? NewContent { get; private set; }

			public string NickName { get; }

			public string Subcategory { get; }

			public string? Tier { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}
		#endregion
	}
}
