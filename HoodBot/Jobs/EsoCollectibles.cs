namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

internal sealed class EsoCollectibles : CreateOrUpdateJob<Collectible>
{
	#region Private Constants

	// Includes only collectibles that have a referenceId (i.e., are obtainable) or are Upgrades.
	private const string CollectibleQuery = """
		SELECT id, name, nickname, description, categoryName, subCategoryName
		FROM collectibles
		WHERE
			((categoryName IN ('Appearance', 'Customized Actions', 'Mementos', 'Mounts', 'Non-Combat Pets', 'Patrons', 'Tools') AND referenceId != 0) OR
			(categoryName = 'Allies' AND subCategoryName != 'Companions') OR
			(categoryName = 'Furnishings' AND subCategoryName = 'Houseguests') OR
			(categoryName = 'Upgrade'))
		""";

	private const string TemplateName = "Online Collectible Summary";
	#endregion

	#region Static Fields
	private static readonly HashSet<long> ExcludedIds =
	[
		271, // Gold Coast 1
		272, // Gold Coast 2
		273, // Gold Coast 3
		274, // Red Sail 1
		275, // Red Sail 2
		276, // Red Sail 3
		370, // Test 05 Default Idle
		371, // Test 06 Injured
		372, // Test 07 Goblin
		1156, // NOT IN USE
		1306, // NAME ME Summer Robe
		6017, // Summerset (inactive duplicate, give or take a few words)
		9369, // Prairie Dog (clearly a test/deprecated item, real is at 10626)
	];
	#endregion

	#region Fields
	private readonly Dictionary<long, Collectible> allCollectibles = [];
	private readonly Dictionary<string, List<string>> crateTiers = new(StringComparer.OrdinalIgnoreCase);
	private string? blankPage;
	#endregion

	#region Constructors
	[JobInfo("Collectibles", "ESO Update")]
	public EsoCollectibles(JobManager jobManager)
		: base(jobManager)
	{
		var title = TitleFactory.FromUnvalidated(this.Site, jobManager.WikiInfo.ResultsPage + "/ESO Collectibles");
		this.SetTemporaryResultHandler(new PageResultHandler(title, false));
		this.StatusWriteLine("DON'T FORGET TO UPDATE MOD HEADER!");
	}
	#endregion

	#region Public Override Properties
	public override string LogName => "ESO Collectibles";
	#endregion

	#region Protected Override Methods
	protected override string? GetDisambiguator(Collectible item) => item.SubCategory.Length == 0
		? CatToFileSubcat(item.CollectibleType) ?? item.CollectibleType
		: SubcatToSingular(item.SubCategory);

	protected override string GetEditSummary(Page page) => (page.Exists ? "Update" : "Create") + " collectible";

	protected override TitleDictionary<Collectible> GetExistingItems()
	{
		var retval = new TitleDictionary<Collectible>();
		var site = (UespSite)this.Site;
		var pages = site.CreateMetaPageCollection(PageModules.None, false, "id");
		pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);
		var untracked = new TitleCollection(this.Site);
		foreach (var page in pages)
		{
			if (page is not VariablesPage variablesPage)
			{
				continue;
			}

			var idText = variablesPage.GetVariable("id");
			if (long.TryParse(idText, NumberStyles.Integer, this.Site.Culture, out var id) &&
				this.allCollectibles.Remove(id, out var item))
			{
				retval.Add(page.Title, item);
			}
			else
			{
				untracked.Add(page.Title);
			}
		}

		var untrackedPages = untracked.Load();
		foreach (var page in untrackedPages)
		{
			var parser = new SiteParser(page);
			var text = $"Page found on wiki but not in collectibles: {page.Title}";
			if (parser.FindTemplate(TemplateName) is ITemplateNode template)
			{
				text += " -> " + template.GetValue("collectibletype") + ", " + template.GetValue("type");
			}

			Debug.WriteLine(text);
		}

		return retval;
	}

	protected override void GetExternalData()
	{
		this.StatusWriteLine("Getting collectibles from database");
		foreach (var item in Database.RunQuery(EsoLog.Connection, CollectibleQuery, CollectibleFromRow))
		{
			this.allCollectibles.Add(item.Id, item);
		}

		foreach (var id in ExcludedIds)
		{
			this.allCollectibles.Remove(id);
		}

		this.GetCrownCrates();
	}

	protected override TitleDictionary<Collectible> GetNewItems()
	{
		var retval = new TitleDictionary<Collectible>();
		var comparer = StringComparer.Ordinal;
		var dupes = this.allCollectibles.Values
			.GroupBy(c => c.Name, comparer)
			.Where(g => g.Skip(1).Any()) // => Count() > 1
			.Select(g => g.Key)
			.ToList();
		foreach (var item in this.allCollectibles.Values)
		{
			var titleText = item.Name;
			if (dupes.Contains(item.Name, comparer))
			{
				titleText += $" ({this.GetDisambiguator(item)})";
			}

			var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], titleText);
			retval.Add(title, item);
		}

		return retval;
	}

	protected override string GetNewPageText(Title title, Collectible item) => this.blankPage ??= this.LoadBlankPage();

	protected override void ItemPageLoaded(SiteParser parser, Collectible item)
	{
		if (parser.Page.IsMissing)
		{
			parser.ReplaceText(
				"<gallery>\n</gallery>",
				$"<gallery>\nON-crown store-{parser.Title.PageName}.jpg\n</gallery>",
				StringComparison.Ordinal,
				ReplaceLocations.Comments | ReplaceLocations.Text);
		}

		var template = parser.FindTemplate(TemplateName) ?? throw new InvalidOperationException(TemplateName + " template not found.");
		template.Update("collectibletype", CatToTemplateType(item.CollectibleType));
		if (parser.Page.IsMissing)
		{
			template.Update("description", item.Description);
			template.Update("id", item.Id.ToStringInvariant());
			template.Update("icon", $"<!--{item.IconName}-->");
			template.Update("image", $"<!--{item.ImageName}-->");
		}

		if (!parser.Page.Title.LabelName().OrdinalEquals(item.Name))
		{
			template.UpdateIfEmpty("titlename", item.Name);
		}

		if (template.Find("name") is IParameterNode nameParam &&
			nameParam.GetValue().Length != 0)
		{
			template.RenameParameter("name", "nickname");
		}

		template.Update("nickname", item.NickName, ParameterFormat.OnePerLine, true);

		var typeVal = SubcatToSingular(item.SubCategory);
		if (typeVal.Length > 0)
		{
			template.Update("type", typeVal, ParameterFormat.OnePerLine, true);
		}

		if (template.GetValue("crate").OrdinalEquals("Unknown"))
		{
			template.Update("crate", string.Empty);
		}

		if (this.crateTiers.TryGetValue(item.Name, out var crateInfo))
		{
			var crates = string.Join(", ", crateInfo);
			template.Update("crate", crates);
		}

		if (item.Tier is not null)
		{
			template.Update("tier", item.Tier);
		}
	}
	#endregion

	#region Private Static Methods
	private static string CatToTemplateType(string category) => category switch
	{
		"Allies" => "Ally",
		"Non-Combat Pets" => "Pet",
		_ => category.TrimEnd('s')
	};

	private static string? CatToFileSubcat(string collectibleType) => collectibleType switch
	{
		"Allies" => "ally",
		"Customized Action" => "action",
		"Memento" => "memento",
		"Mount" => "mount",
		"Non-Combat Pets" => "pet",
		"Patrons" => "patron",
		"Tools" => "tool",
		_ => null
	};

	private static Collectible CollectibleFromRow(IDataRecord row)
	{
		var id = (long)row["id"];
		var name = ReplacementData.CollectibleNameOverrides.GetValueOrDefault(id, EsoLog.ConvertEncoding((string)row["name"]));
		var collectibleType = EsoLog.ConvertEncoding((string)row["categoryName"]);
		var subCategory = EsoLog.ConvertEncoding((string)row["subCategoryName"]);
		var fileCategory = CatToFileSubcat(collectibleType) ?? SubcatToSingular(subCategory);
		fileCategory = fileCategory.OrdinalEquals("Hair Style")
			? "hairstyle"
			: fileCategory.ToLowerInvariant();
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

	private static string SubcatToSingular(string subCategory) => subCategory switch
	{
		"" => string.Empty,
		"Nix-Oxen" => "Nix-Ox",
		"Personalities" => "Personality",
		"Stories" => "Story",
		"Undaunted Trophies" => "Undaunted Trophy",
		"Wolves" => "Wolf",
		_ => subCategory[^1] == 's' ? subCategory[0..^1] : subCategory
	};
	#endregion

	#region Private Methods
	private void GetCrownCrates()
	{
		PageCollection crownCrates = new(this.Site);
		crownCrates.GetCategoryMembers("Online-Crown Crates");
		crownCrates.Remove("Online:Crown Crates/Unknown");
		foreach (var crate in crownCrates)
		{
			this.ParseCrate(crate);
		}
	}

	private string LoadBlankPage()
	{
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

	private void ParseCrate(Page crate)
	{
		SiteParser parser = new(crate);
		foreach (var template in parser.FindTemplates(this.Site, "ESO Crate Card List"))
		{
			foreach (var parameter in template.ParameterCluster(2))
			{
				var title = parameter[0].GetValue();
				if (!this.crateTiers.TryGetValue(title, out var allTiers))
				{
					allTiers = [];
					this.crateTiers.Add(title, allTiers);
				}

				allTiers.Add(crate.Title.PageName.Replace("Crown Crates/", string.Empty, StringComparison.Ordinal));
			}
		}
	}
	#endregion
}