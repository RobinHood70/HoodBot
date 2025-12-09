namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.Design;
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
	private const string CollectibleQuery =
		"SELECT id, name, nickname, description, categoryName, subCategoryName " +
		"FROM collectibles " +
		"WHERE " +
			"categoryName NOT IN ('Armor Styles' , 'Emotes', 'Fragments', 'Furnishings', 'Housing', 'Stories', 'Weapon Styles') AND " +
			"subCategoryName != 'Companions' AND " +
			"referenceId != 0 AND " +
			"id NOT IN (248, 271, 272, 273, 274, 275, 276, 370, 371, 372, 1156, 1306, 1480, 8202, 10387, 10388)";

	private const string TemplateName = "Online Collectible Summary";
	#endregion

	#region Static Fields
	private static readonly List<long> DeprecatedIds =
	[
		9369, // Prairie Dog
		11242, // Almalexia deck
	];

	private static readonly Dictionary<long, string> NameOverrides = new()
	{
		[6117] = "Honor Guard Jack"
	};
	#endregion

	#region Fields
	private readonly Dictionary<Title, Collectible> collectibles = [];
	private readonly Dictionary<string, List<string>> crateTiers = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region Constructors
	[JobInfo("Create Collectibles", "ESO Update")]
	public EsoCollectibles(JobManager jobManager)
		: base(jobManager)
	{
		this.CreateOnly = Tristate.True;
		var title = TitleFactory.FromUnvalidated(this.Site, jobManager.WikiInfo.ResultsPage + "/ESO Collectibles");
		this.SetTemporaryResultHandler(new PageResultHandler(title, false));

		this.StatusWriteLine("DON'T FORGET TO UPDATE MOD HEADER!");
	}
	#endregion

	#region Public Override Properties
	public override string LogName => "ESO Collectibles";
	#endregion

	#region Protected Override Methods
	protected override void BeforeLoadPages()
	{
		var allTitles = new TitleCollection(this.Site);
		allTitles.GetNamespace(UespNamespaces.Online, Filter.Any);
		var pages = this.GetBacklinks();
		this.GetCollectibles(allTitles, pages);

		this.StatusWriteLine("Getting crown crates");
		this.GetCrownCrates();
	}

	protected override string GetEditSummary(Page page) => "Create collectible page";

	protected override void LoadPages()
	{
		var blankText = this.GetBlankText();
		foreach (var collectible in this.collectibles)
		{
			var page = this.Site.CreatePage(collectible.Key, blankText!);
			this.Pages.Add(page);
			this.PageLoaded(page);
		}
	}

	protected override void ParseText(SiteParser parser)
	{
		var page = parser.Page;
		parser.ReplaceText(
			"<gallery>\n</gallery>",
			$"<gallery>\nON-crown store-{page.Title.PageName}.jpg\n</gallery>",
			StringComparison.Ordinal,
			ReplaceLocations.Comments | ReplaceLocations.Text);
		var collectible = this.collectibles[page.Title] ?? throw new InvalidOperationException();
		if (this.crateTiers.TryGetValue(collectible.Name, out var crateInfo))
		{
			collectible.Crates.AddRange(crateInfo);
		}

		if (parser.FindTemplate(TemplateName) is ITemplateNode template)
		{
			template.Update("collectibletype", CategorySingular(collectible.CollectibleType));
			template.Update("type", CategorySingular(collectible.SubCategory));
			template.UpdateIfEmpty("image", $"<!--{collectible.ImageName}-->");
			template.UpdateIfEmpty("icon", $"{collectible.IconName}");
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
		foreach (var item in Database.RunQuery(EsoLog.Connection, CollectibleQuery, CollectibleFromRow))
		{
			retval.Add(item);
			if (!singleNames.Add(item.Name))
			{
				dupeNames.Add(item.Name);
			}
		}

		return (retval, dupeNames);
	}

	private static Collectible CollectibleFromRow(IDataRecord row)
	{
		var id = (long)row["id"];
		var name = NameOverrides.GetValueOrDefault(id, EsoLog.ConvertEncoding((string)row["name"]));
		var collectibleType = EsoLog.ConvertEncoding((string)row["categoryName"]);
		var subCategory = EsoLog.ConvertEncoding((string)row["subCategoryName"]);
		var colTypeSingular = CategorySingular(collectibleType);
		var subCatSingular = CategorySingular(subCategory);
		var fileCategory = colTypeSingular switch
		{
			"Appearance" => subCatSingular.OrdinalEquals("Hair Style")
				? "hairstyle"
				: subCatSingular.ToLowerInvariant(),
			"Customized Action" => "action",
			"Memento" or "Tool" => "memento",
			"Ally" or "Mount" or "Pet" => colTypeSingular.ToLowerInvariant(),
			_ => subCatSingular
		};

		return new Collectible(
			id: id,
			name: name,
			nickName: EsoLog.ConvertEncoding((string)row["nickname"]),
			description: EsoLog.ConvertEncoding((string)row["description"]),
			collectibleType: collectibleType,
			subCategory: subCategory,
			imageName: $"ON-{fileCategory}-{name}",
			iconName: $"ON-icon-{fileCategory}-{name}");
	}

	private static string CategorySingular(string category) => category switch
	{
		"Allies" => "Ally",
		"Nix-Oxen" => "Nix-Ox",
		"Non-Combat Pets" => "Pet",
		"Personalities" => "Personality",
		"Stories" => "Story",
		"Undaunted Trophies" => "Undaunted Trophy",
		"Wolves" => "Wolf",
		_ => category[^1] == 's' ? category[0..^1] : category
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
				if (allTitles.Contains(titleDisambig))
				{
					this.StatusWriteLine($"Couldn't find a usable page for {item.Name}.");
					return;
				}

				title = titleDisambig;
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

	private string GetBlankText()
	{
		this.StatusWriteLine("Getting wiki info");
		var page = this.Site.LoadPage($"Template:{TemplateName}/Blank");
		if (page.IsMissing)
		{
			throw new InvalidOperationException("Blank Template not found.");
		}

		var extract = new SiteParser(page);
		var pre = extract.FindTemplate("Pre");
		if (pre?.Find(1) is IParameterNode param)
		{
			var value = param.Value;
			if (value.Count > 0 &&
				value[0] is ITagNode tag &&
				tag.Name.OrdinalEquals("nowiki") &&
				tag.InnerText?.Trim() is string blankTemplate)
			{
				return GameInfo.Eso.ModTemplate.Length == 0
					? blankTemplate
					: GameInfo.Eso.ModHeader + blankTemplate;
			}
		}

		throw new InvalidOperationException("Blank template not in expected format.");
	}

	private void GetCollectibles(TitleCollection allTitles, PageCollection pages)
	{
		var knownIds = this.GetKnown(pages);
		this.StatusWriteLine("Getting collectibles from database");
		var (allCollectibles, dupeNames) = GetDBCollectibles();
		foreach (var item in allCollectibles)
		{
			var titleText = item.Name;
			if (dupeNames.Contains(item.Name))
			{
				var cat = item.SubCategory.Length == 0 ? item.CollectibleType : item.SubCategory;
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
		var knownIds = new HashSet<long>(pages.Count + DeprecatedIds.Count);
		foreach (var id in DeprecatedIds)
		{
			knownIds.Add(id);
		}

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
		SiteParser parser = new(crate);
		//// var tier = string.Empty;
		var cardListTemplate = TitleFactory.FromTemplate(this.Site, "ESO Crate Card List");
		foreach (var node in parser)
		{
			if (node is IHeaderNode)
			{
				//// tier = GetSectionTitle(header);
			}
			else if (node is ITemplateNode template && template.GetTitle(this.Site) == cardListTemplate)
			{
				foreach (var parameter in template.ParameterCluster(2))
				{
					var title = parameter[0].GetValue();
					if (!this.crateTiers.TryGetValue(title, out var allTiers))
					{
						allTiers = [];
						this.crateTiers.Add(title, allTiers);
					}

					// allTiers.Add($"[[Online:{crate.PageName}#{tier}|{crate.PageName}]]");
					allTiers.Add(crate.Title.PageName);
				}
			}
		}
	}
	#endregion

	#region Private Classes
	private sealed class Collectible(long id, string name, string nickName, string description, string collectibleType, string subCategory, string imageName, string iconName)
	{
		#region Public Properties
		public string CollectibleType { get; } = collectibleType;

		public List<string> Crates { get; } = [];

		public string Description { get; } = description;

		public string IconName { get; } = iconName;

		public long Id { get; } = id;

		public string ImageName { get; } = imageName;

		public string Name { get; } = name;

		public IList<IWikiNode>? NewContent { get; private set; }

		public string NickName { get; } = nickName;

		public string SubCategory { get; } = subCategory;

		public string? Tier { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
	#endregion
}