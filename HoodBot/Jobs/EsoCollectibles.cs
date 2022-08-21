namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
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
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "ESO Collectibles";
		#endregion

		#region Protected Override Properties

		protected override Tristate CreateOnly => Tristate.True;

		protected override string EditSummary => "Create collectible page";

		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages()
		{
			this.Pages.RemoveChanged(false);
			this.StatusWriteLine($"{this.Pages.Count} pages will be created.");
			this.Pages.Shuffle();
			foreach (var page in this.Pages)
			{
				Debug.WriteLine(page.FullPageName);
			}
		}

		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Getting wiki info");
			if (this.Site.LoadPage($"Template:{TemplateName}/Blank") is Page page)
			{
				var parser = new ContextualParser(page);
				var pre = parser.FindSiteTemplate("Pre");
				if (pre?.Find(1)?.Value is NodeCollection nodes &&
					nodes.Count > 0 &&
					nodes[0] is ITagNode tag &&
					string.Equals(tag.Name, "nowiki", StringComparison.Ordinal))
				{
					this.blankText = tag.InnerText?.Trim();
				}
			}

			if (this.blankText is null)
			{
				throw new InvalidOperationException();
			}

			var allTitles = new TitleCollection(this.Site);
			allTitles.GetNamespace(UespNamespaces.Online);

			var site = (UespSite)this.Site;
			var pages = site.CreateMetaPageCollection(PageModules.None, false, "id");
			pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);

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

			this.StatusWriteLine("Getting crown crates");
			this.GetCrownCrates();

			this.StatusWriteLine("Getting collectibles from database");
			var (allCollectibles, dupeNames) = GetDBCollectibles();
			foreach (var item in allCollectibles)
			{
				var title = this.GetGoodTitle(dupeNames, item);
				this.AddItem(item, title, allTitles, knownIds, pages);
			}
		}

		protected override void LoadPages()
		{
			foreach (var collectible in this.collectibles)
			{
				this.Pages.Add(this.Site.CreatePage(collectible.Key));
			}
		}

		protected override string NewPageText(Page page) => this.blankText.NotNull();

		protected override void ParseText(object sender, ContextualParser parser)
		{
			var page = parser.Page;
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
				retval.Add(item);
				if (!singleNames.Add(item.Name))
				{
					dupeNames.Add(item.Name);
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

		private void GetCrownCrates()
		{
			PageCollection crownCrates = new(this.Site);
			crownCrates.GetCategoryMembers("Online-Crown Crates");
			foreach (var crate in crownCrates)
			{
				this.ParseCrate(crate);
			}
		}

		private Title GetGoodTitle(HashSet<string> dupeNames, Collectible item)
		{
			if (dupeNames.Contains(item.Name))
			{
				var cat = item.Subcategory.Length == 0 ? item.Category : item.Subcategory;
				return TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], $"{item.Name} ({cat})");
			}

			return TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], item.Name);
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

			public long Id { get; }

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
