namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

internal sealed partial class EsoFurnishingUpdater : CreateOrUpdateJob<Furnishing>
{
	#region Private Constants
	private const string CollectiblesQuery = $"SELECT description, furnCategory, furnLimitType, furnSubCategory, id itemId, itemLink resultitemLink, name, nickname, tags FROM collectibles WHERE furnCategory != ''";
	private const string MinedItemsQuery = "SELECT abilityDesc, bindType, description, furnCategory, furnLimitType, itemId, name, quality, resultitemLink, tags, type FROM uesp_esolog.minedItemSummary WHERE type = 61"; // 61 = Furnishings
	#endregion

	#region Static Fields
	private static readonly Dictionary<string, int> CommentCounts = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> FurnishingKeep = new(StringComparer.Ordinal)
	{
		"cat", "subcat", "id"
	};

	private static readonly HashSet<long> IgnoreIds = [194537];

	private static readonly HashSet<string> NoHousingCats = new(StringComparer.OrdinalIgnoreCase)
	{
		"Miscellaneous", "Mounts", "Non-Combat Pets", "Services"
	};

	private static readonly Dictionary<string, HashSet<string>> Pruneables = new(StringComparer.Ordinal)
	{
		["Online Furnishing Antiquity/Start"] = new HashSet<string>(StringComparer.Ordinal) { "leads" },
		["Online Furnishing Antiquity/Row"] = [],
		["Online Furnishing Crafting"] = new HashSet<string>(StringComparer.Ordinal) { "craft" },
		["Online Furnishing Purchase"] = []
	};

	private static readonly string TemplateName = "Online Furnishing Summary";
	#endregion

	#region Fields
	private readonly Context context;
	private readonly Dictionary<long, Furnishing> furnishings = [];

	// nameLookup has a string key so that pagenames that only differ by case count as duplicates.
	private readonly List<string> fileMessages = [];
	private readonly TitleDictionary<long> idLookup = [];
	private readonly TitleCollection missingIdExceptions;
	private readonly List<string> pageMessages = [];

	//// private readonly Dictionary<Title, Furnishing> furnishingDictionary = new();
	#endregion

	#region Constructors
	[JobInfo("Furnishings", "ESO Update")]
	public EsoFurnishingUpdater(JobManager jobManager)
		: base(jobManager)
	{
		//// this.Shuffle = true;
		//// jobManager.ShowDiffs = false;
		var title = TitleFactory.FromUnvalidated(this.Site, jobManager.WikiInfo.ResultsPage + "/ESO Furnishings");
		this.SetTemporaryResultHandler(new PageResultHandler(title, false));
		this.context = new Context(this.Site);
		this.missingIdExceptions = new TitleCollection(this.Site, "Online:Orcish Shrine, Malacath", "Online:Goblin Totem");
		this.NewPageText = this.CreatePage;
	}
	#endregion

	#region Public Override Properties
	public override string LogName { get; } = "ESO Furnishing Update";

	protected override string? GetDisambiguator(Furnishing item) => item.Collectible ? "collectible" : "furnishing";
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		if (this.pageMessages.Count == 0 && this.fileMessages.Count == 0)
		{
			return;
		}

		this.WriteLine("__FORCETOC__");
		if (this.pageMessages.Count > 0)
		{
			this.WriteLine("== Online Page Name Issues ==");
			this.pageMessages.Sort(StringComparer.Ordinal);
			foreach (var message in this.pageMessages)
			{
				this.WriteLine(message);
				this.WriteLine(string.Empty);
			}
		}

		if (this.fileMessages.Count > 0)
		{
			this.WriteLine("== File Page Name Issues ==");
			this.fileMessages.Sort(StringComparer.Ordinal);
			foreach (var message in this.fileMessages)
			{
				this.WriteLine(message);
				this.WriteLine(string.Empty);
			}
		}
	}

	protected override string GetEditSummary(Page page) => "Update info from ESO database; remove cruft";

	protected override bool IsValidPage(SiteParser parser, Furnishing item) => parser.FindTemplate(TemplateName) is not null;

	protected override void GetKnownPages()
	{
		var knownPages = this.Site.CreateMetaPageCollection(PageModules.Default, false, "id");
		knownPages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
		var removals = new TitleCollection(this.Site);
		foreach (var page in knownPages)
		{
			var testTitle = TitleFactory.FromValidated(this.Site[UespNamespaces.Online], "Bushes, Withered Cluster");
			if (this.Items.TryGetValue(testTitle, out var test))
			{
			}

			if (page.Title.PageNameEquals("Bushes, Withered Cluster"))
			{
			}

			if (page is not VariablesPage varPage)
			{
				continue;
			}

			if (varPage.GetVariable("id") is string idText &&
				long.TryParse(idText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, this.Site.Culture, out var id) &&
				this.furnishings.TryGetValue(id, out var item))
			{
				if (page.Title != item.Title)
				{
					this.Items.Remove(item.Title);
					if (!this.Items.TryAdd(page.Title, item))
					{
						removals.Add(item.Title);
					}
				}

				this.Pages.TryAddAsLoaded(page);
			}
			else
			{
				Debug.WriteLine($"{page.Title} has a missing or invalid id");
			}
		}

		foreach (var title in removals)
		{
			this.Items.Remove(title);
		}
	}

	protected override void LoadItems()
	{
		foreach (var collectible in Database.RunQuery(EsoLog.Connection, CollectiblesQuery, record => new Furnishing(record, this.Site, true)))
		{
			this.furnishings.Add(Furnishing.GetKey(collectible.Id, true), collectible);
		}

		foreach (var furnishing in Database.RunQuery(EsoLog.Connection, MinedItemsQuery, record => new Furnishing(record, this.Site, false)))
		{
			this.furnishings.Add(Furnishing.GetKey(furnishing.Id, false), furnishing);
		}

		var exceptions = new (long Id, bool Collectible, string? Title)[]
		{
			// Collectibles
			(1185, true, "Dwarven Spider (mount)"),
			(1155, true, "Dwarven Spider (pet)"),
			(4671, true, "Frostbane Bear (mount)"),
			(4746, true, "Frostbane Bear (pet)"),
			(1457, true, "Frostbane Wolf (mount)"),
			(4747, true, "Frostbane Wolf (pet)"),
			(1413, true, "Frostbane Sabre Cat (mount)"),
			(4745, true, "Frostbane Sabre Cat (pet)"),
			(1395, true, "Shadowghost Guar (mount)"),
			(1396, true, "Shadowghost Guar (pet)"),

			// Mined Items
			(183198, false, null), // Identical: Bushes, Withered Cluster

		};

		foreach (var (id, collectible, titleText) in exceptions)
		{
			var key = Furnishing.GetKey(id, collectible);
			if (titleText is not null)
			{
				var title = TitleFactory.FromValidated(this.Site[UespNamespaces.Online], titleText);
				this.furnishings[key].Title = title;
			}
			else
			{
				this.furnishings.Remove(key);
			}
		}

		var dupes = new Dictionary<long, Title>();
		foreach (var (id, furnishing) in this.furnishings)
		{
			if (!this.Items.TryAdd(furnishing.Title, furnishing))
			{
				// Leave item in list after check so additional dupes can be detected. Remove all dupes later.
				var dupe = this.Items[furnishing.Title];
				var key = Furnishing.GetKey(furnishing.Id, furnishing.Collectible);
				var key2 = Furnishing.GetKey(dupe.Id, dupe.Collectible);
				Debug.WriteLine($"{key} / {key2}: {furnishing.Title}");
				dupes.Add(key, furnishing.Title);
			}
		}

		if (dupes.Count > 0)
		{
			this.StatusWriteLine("DUPES FOUND - see Debug output.");
			foreach (var (id, title) in dupes)
			{
				this.Items.Remove(title);
			}
		}
	}

	protected override void LoadPages()
	{
		base.LoadPages();
		var top10 = CommentCounts.OrderByDescending(entry => entry.Value).Take(10);
		foreach (var comment in top10)
		{
			Debug.Write(comment.Value.ToStringInvariant() + ":  ");
			Debug.WriteLine(comment.Key);
			Debug.WriteLine(string.Empty);
		}
	}

	protected override void ValidPageLoaded(SiteParser parser, Furnishing item)
	{
		base.ValidPageLoaded(parser, item);
		var template = parser.FindTemplate(TemplateName) ?? throw new InvalidOperationException();
		if (this.GenericTemplateFixes(template))
		{
			this.Warn("Template has anonymous parameter on " + parser.Title);
		}

		this.FurnishingFixes(template, parser.Page);
		template.RemoveEmpties(FurnishingKeep);
		CheckComments(parser, template);
	}
	#endregion

	#region Private Static Methods
	private static void CheckBehavior(ITemplateNode template, Furnishing furnishing)
	{
		if (furnishing.Behavior is not null && furnishing.Behavior.Length > 0)
		{
			var behavior = EsoSpace.TrimBehavior(template.GetValue("behavior"));
			if (behavior is null || behavior.Length == 0)
			{
				template.Remove("behavior");
			}
			else
			{
				template.AddIfNotExists("behavior", furnishing.Behavior, ParameterFormat.OnePerLine);
			}
		}
	}

	private static void CheckComments(SiteParser parser, ITemplateNode template)
	{
		var cat = template.GetValue("cat");
		var doHousing = cat is not null && NoHousingCats.Contains(cat);
		RemoveComments(parser, doHousing);
		PruneSecondaryTemplates(parser);

		foreach (var comment in parser.FindAll<ICommentNode>(null, false, false, 0))
		{
			CommentCounts[comment.Comment] = CommentCounts.TryGetValue(comment.Comment, out var value)
				? ++value
				: 1;
		}
	}

	private static void CheckIcon(ITemplateNode template, string labelName)
	{
		labelName = labelName.Replace(':', ',');
		var defaultName = $"ON-icon-furnishing-{labelName}.png";
		//// pageName = pageName.Replace(':', ',');
		//// var defaultPageName = $"ON-icon-furnishing-{pageName}.png";
		if (template.GetValue("icon").OrdinalEquals(defaultName))
		{
			template.Remove("icon");
		}/*
		else if (!defaultName.OrdinalEquals(defaultPageName))
		{
			template.UpdateIfEmpty("icon", defaultPageName);
		}*/
	}

	private static string CheckName(ITemplateNode template, string labelName)
	{
		if (template.GetValue("name") is string nameValue)
		{
			if (!nameValue.OrdinalEquals(labelName))
			{
				return nameValue;
			}

			template.Remove("name");
		}

		return labelName;
	}

	private static void FixBehavior(ITemplateNode template)
	{
		if (template.Find("behavior") is IParameterNode behavior)
		{
			var list = new List<string>(behavior.GetValue().Split(TextArrays.Comma));
			for (var i = list.Count - 1; i >= 0; i--)
			{
				list[i] = list[i].Trim();
				if (list[i].Length == 0)
				{
					list.RemoveAt(i);
				}
				else if (list[i].StartsWith("Light ", StringComparison.OrdinalIgnoreCase))
				{
					list[i] = "Light";
				}
			}

			behavior.SetValue(string.Join(", ", list), ParameterFormat.OnePerLine);
		}
	}

	private static void PruneSecondaryTemplates(SiteParser parser)
	{
		foreach (var subTemplate in parser.FindTemplates(Pruneables.Keys))
		{
			var templateName = subTemplate.GetTitleText();
			var exceptions = Pruneables[templateName];
			subTemplate.RemoveEmpties(exceptions);
		}
	}

	private static void RemoveComments(SiteParser parser, bool doHousing)
	{
		for (var i = parser.Count - 2; i >= 0; i--)
		{
			if (parser[i] is ICommentNode comment)
			{
				if (TemplateFinder().IsMatch(comment.Comment))
				{
					parser.RemoveAt(i);
				}
				else if (HeaderFinder().IsMatch(comment.Comment))
				{
					parser.RemoveAt(i);
				}
				else if (comment.Comment.StartsWith("<!--Instructions: ", StringComparison.Ordinal))
				{
					var remainder = comment.Comment[18..];
					if (remainder.StartsWith("Fill in antiquity information", StringComparison.Ordinal) ||
						remainder.StartsWith("Add book information here", StringComparison.Ordinal) ||
						remainder.StartsWith("Use the template below to fill out the crafting details.", StringComparison.Ordinal) ||
						remainder.StartsWith("Add vendor information here", StringComparison.Ordinal) ||
						remainder.StartsWith("List the sources from", StringComparison.Ordinal) ||
						(doHousing && remainder.StartsWith("Add this section to the page if the main category is NOT", StringComparison.Ordinal)))
					{
						parser.RemoveAt(i);
					}
				}
			}
		}

		parser.MergeText(false);
		foreach (var text in parser.RootTextNodes)
		{
			text.Text = VerticalSpaceFinder().Replace(text.Text, "\n\n");
		}
	}

	private static void RemoveInstructions(SiteParser parser, int i, ICommentNode comment, string commentStart)
	{
		if (comment.Comment.StartsWith("<!--Instructions: " + commentStart, StringComparison.Ordinal))
		{
			parser.RemoveAt(i);
		}
	}
	#endregion

	#region Private Static Partial Methods
	[GeneratedRegex(@"^==\s*Available From\s*==", RegexOptions.Multiline, Globals.DefaultGeneratedRegexTimeout)]
	private static partial Regex HeaderFinder();

	[GeneratedRegex(@"{{\s*Online Furnishing (Antiquity/(Row|Start)|Books|Crafting|Purchase)\b", RegexOptions.None, Globals.DefaultGeneratedRegexTimeout)]
	private static partial Regex TemplateFinder();

	[GeneratedRegex("(\r?\n){3,}", RegexOptions.None, Globals.DefaultGeneratedRegexTimeout)]
	private static partial Regex VerticalSpaceFinder();
	#endregion

	#region Private Methods
	private void CheckImage(ITemplateNode template, string name, string link)
	{
		var fileSpace = this.Site[MediaWikiNamespaces.File];
		var imageName = Furnishing.ImageName(name);
		if (template.GetValue("image") is string imageValue)
		{
			imageValue = imageValue.Trim();
			if (imageValue.Length != 0 &&
				!imageValue.OrdinalEquals(imageName))
			{
				imageName = imageValue;
			}
			else
			{
				template.Remove("image");
			}
		}

		var nameFix = imageName.Replace(':', ',');
		var oldTitle = TitleFactory.FromUnvalidated(fileSpace, imageName).Title;
		var newTitle = TitleFactory.FromUnvalidated(fileSpace, nameFix).Title;

		if (!oldTitle.LabelName().OrdinalEquals(newTitle.LabelName()))
		{
			this.fileMessages.Add($"{SiteLink.ToText(oldTitle, LinkFormat.LabelName)} on {link} ''should be''<br>\n{newTitle.PageName}");

			var noItem1 = oldTitle.PageName.Replace("-item-", "-", StringComparison.Ordinal);
			var noItem2 = newTitle.PageName.Replace("-item-", "-", StringComparison.Ordinal);
			if ((oldTitle.PageName.Contains("-item-", StringComparison.Ordinal) ||
				newTitle.PageName.Contains("-item-", StringComparison.Ordinal)) &&
				noItem1.OrdinalEquals(noItem2))
			{
				Debug.WriteLine($"File Replace Needed:\n  {oldTitle.FullPageName()} with\n  {newTitle.FullPageName()}");
			}
		}
	}

	private void CheckTitle(Title title, string labelName, Furnishing furnishing)
	{
		var compareName = Furnishing.PageNameExceptions.GetValueOrDefault(labelName, furnishing.Title.LabelName());
		if (!labelName.OrdinalEquals(compareName))
		{
			this.pageMessages.Add($"[[{title.FullPageName()}|{labelName}]] ''should be''<br>\n" +
			  $"{compareName}");
			if (!title.PageName.Contains(':', StringComparison.Ordinal) &&
				compareName.Contains(':', StringComparison.Ordinal) &&
				title.PageName.Replace(',', ':').OrdinalEquals(furnishing.Title.PageName))
			{
				Debug.WriteLine($"Page Replace Needed: {title.FullPageName()}\t{furnishing.Title}");
			}
		}
	}

	private string CreatePage(Title title, Furnishing furnishing) => "{{Online Furnishing Summary}}";

	private Furnishing? FindFurnishing(ITemplateNode template, Page page)
	{
		Furnishing? retval = null;
		var idText = template.GetValue("id") ?? string.Empty;
		var isCollectible = (template.GetValue("collectible") ?? string.Empty).Length > 0;
		if (long.TryParse(idText, NumberStyles.None, page.Site.Culture, out var id))
		{
			var key = Furnishing.GetKey(id, isCollectible);
			if (IgnoreIds.Contains(key))
			{
				return null;
			}

			if (!this.furnishings.TryGetValue(key, out retval))
			{
				Debug.WriteLine($"Furnishing ID {id} not found on page {SiteLink.ToText(page)}.");
			}
		}
		else if (!this.missingIdExceptions.Contains(page.Title))
		{
			Debug.WriteLine($"Furnishing ID on {SiteLink.ToText(page)} is missing or nonsensical.");
		}

		if (retval is null && this.idLookup.TryGetValue(page.Title, out var recoveredId))
		{
			Debug.WriteLine($"  Recovered ID {recoveredId} from {page.Title.PageName}.");
			if (this.furnishings.TryGetValue(Furnishing.GetKey(recoveredId, isCollectible), out retval))
			{
				template.Update("id", recoveredId.ToStringInvariant());
			}
		}

		return retval;
	}

	private void FixBundles(ITemplateNode template)
	{
		if (template.Find("bundles") is IParameterNode bundles)
		{
			var value = bundles.Value;
			var factory = template.Factory;
			for (var i = 0; i < value.Count; i++)
			{
				if (value is ILinkNode link)
				{
					var siteLink = SiteLink.FromLinkNode(this.Site, link);
					value.RemoveAt(i);
					if (siteLink.Text is string text)
					{
						value.Insert(i, factory.TextNode(text));
					}
				}
			}
		}
	}

	private void FixList(ITemplateNode template, string parameterName)
	{
		var plural = parameterName + "s";
		if (template.Find(plural, parameterName) is IParameterNode param)
		{
			param.SetName(plural);
			var curText = param.GetValue();
			var splitOn = curText.Contains('~', StringComparison.Ordinal) ? '~' : ',';
			var split = curText.Split(splitOn, StringSplitOptions.None);
			var list = new List<(string Name, int Value)>(split.Length / 2);
			for (var i = 0; i < split.Length; i += 2)
			{
				split[i + 1] = split[i + 1]
					.Replace(" ", string.Empty, StringComparison.Ordinal)
					.Replace("(", string.Empty, StringComparison.Ordinal)
					.Replace(")", string.Empty, StringComparison.Ordinal);
				var intValue = split[i + 1].Length == 0 ? 1 : int.Parse(split[i + 1], this.Site.Culture);
				list.Add((split[i], intValue));
			}

			if (parameterName.OrdinalEquals("material"))
			{
				list.Sort((item1, item2) =>
					item2.Value.CompareTo(item1.Value) is int result && result == 0
						? string.Compare(item1.Name, item2.Name, false, this.Site.Culture)
						: result);
			}

			var sb = new StringBuilder(list.Count * 10);
			foreach (var (name, value) in list)
			{
				sb
					.Append(name)
					.Append('~')
					.Append(value.ToStringInvariant())
					.Append('~');
			}

			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
			}

			param.SetValue(sb.ToString(), ParameterFormat.OnePerLine);
		}
	}

	private void FurnishingFixes(ITemplateNode template, Page? page)
	{
		ArgumentNullException.ThrowIfNull(page);
		var labelName = page.Title.LabelName();
		var name = CheckName(template, labelName);
		CheckIcon(template, name);
		if (this.FindFurnishing(template, page) is not Furnishing furnishing)
		{
			return;
		}

		if (template.GetValue("name") is string wikiTitle)
		{
			wikiTitle = ParseToText.Build(wikiTitle, this.context);
		}
		else
		{
			wikiTitle = labelName;
		}

		if (!furnishing.Title.PageNameEquals(wikiTitle))
		{
			this.StatusWriteLine($"Page title != game title. Check for invalid ID or name change.\nPage: {page.Title.PageName}\nGame: {furnishing.Title.PageName}\n");
		}

		this.CheckImage(template, name, SiteLink.ToText(page, LinkFormat.LabelName));
		this.CheckTitle(page.Title, labelName, furnishing);

		if (!furnishing.Title.PageNameEquals(labelName))
		{
			template.Update("titlename", furnishing.Title.PageName, ParameterFormat.OnePerLine, true);
		}

		if (furnishing.Collectible)
		{
			template.Update("nickname", furnishing.NickName, ParameterFormat.OnePerLine, true);
		}

		template.Update("quality", furnishing.Quality, ParameterFormat.OnePerLine, true);

		if (furnishing.Size is not null)
		{
			template.Update("size", furnishing.Size, ParameterFormat.OnePerLine, false);
		}

		template.Update("desc", furnishing.Description, ParameterFormat.OnePerLine, false);
		if (!string.IsNullOrEmpty(furnishing.FurnishingCategory))
		{
			template.Update("cat", furnishing.FurnishingCategory, ParameterFormat.OnePerLine, false);
		}

		if (!string.IsNullOrEmpty(furnishing.FurnishingSubcategory))
		{
			template.Update("subcat", furnishing.FurnishingSubcategory, ParameterFormat.OnePerLine, false);
		}

		CheckBehavior(template, furnishing);

		var craft = template.GetValue("craft");
		if (craft is not null)
		{
			var craftWord = craft switch
			{
				"Alchemy" => "Formula",
				"Blacksmithing" => "Diagram",
				"Clothing" => "Pattern",
				"Enchanting" => "Praxis",
				"Jewelry Crafting" => "Sketch",
				"Provisioning" => "Design",
				"Woodworking" => "Blueprint",
				_ => throw new InvalidOperationException()
			};

			var expectedNmae = craftWord + ": " + (template.GetValue("name") ?? page.Title.LabelName());
			var planname = template.GetValue("planname");
			if (expectedNmae.OrdinalEquals(planname))
			{
				template.Remove("planname");
			}
		}

		if (template.GetValue("planquality") is string planquality && template.GetValue("quality").OrdinalEquals(planquality))
		{
			template.Remove("planquality");
		}

		if (furnishing.Materials.Count > 0)
		{
			template.Update("materials", string.Join('~', furnishing.Materials), ParameterFormat.OnePerLine, true);
		}

		if (furnishing.Skills.Count > 0)
		{
			template.Update("skills", string.Join('~', furnishing.Skills), ParameterFormat.OnePerLine, true);
		}

		var bindTypeValue = template.GetValue("bindtype");
		var bindType = (furnishing.Collectible ||
			bindTypeValue.OrdinalEquals("0"))
				? null
				: furnishing.BindType;
		if (bindType is not null)
		{
			template.Update("bindtype", bindType, ParameterFormat.OnePerLine, true);
		}

		if (furnishing.FurnishingLimitType == FurnishingType.None && string.IsNullOrEmpty(furnishing.Behavior))
		{
			template.Remove("collectible");
		}
		else if (template.GetValue("furnLimitType") is string furnLimitType)
		{
			var wantsToBe = Furnishing.FurnishingLimitTypes[furnishing.FurnishingLimitType];
			if (!(furnLimitType + 's').OrdinalEquals(wantsToBe))
			{
				template.Update("furnLimitType", wantsToBe);
			}

			var showCollectible = furnishing.FurnishingLimitType switch
			{
				FurnishingType.TraditionalFurnishings => furnishing.Collectible,
				FurnishingType.SpecialFurnishings => furnishing.Collectible,
				FurnishingType.CollectibleFurnishings => !furnishing.Collectible,
				FurnishingType.SpecialCollectibles => !furnishing.Collectible,
				FurnishingType.None => throw new InvalidOperationException(),
				_ => throw new InvalidOperationException()
			};

			if (showCollectible)
			{
				template.Update("collectible", furnishing.Collectible ? "1" : "0");
			}
		}
	}

	private bool GenericTemplateFixes(ITemplateNode template)
	{
		template.Remove("animated");
		template.Remove("audible");
		template.Remove("collectible");
		template.Remove("creature");
		template.Remove("houses");
		template.Remove("interactable");
		template.Remove("light");
		template.Remove("lightcolor");
		template.Remove("lightcolour");
		template.Remove("luxury");
		template.Remove("master");
		template.Remove("readable");
		template.Remove("sittable");
		template.Remove("visualfx");

		template.RenameParameter("other", "source");
		template.RenameParameter("recipeid", "planid");
		template.RenameParameter("recipename", "planname");
		template.RenameParameter("recipequality", "planquality");
		template.RenameParameter("style", "theme");
		template.RenameParameter("tags", "behavior");
		template.RenameParameter("type", "furnLimitType");
		template.RenameParameter("description", "desc");
		template.RemoveDuplicates();

		FixBehavior(template);
		this.FixBundles(template);
		this.FixList(template, "material");
		this.FixList(template, "skill");

		return template.Find(1) is not null;
	}
	#endregion
}