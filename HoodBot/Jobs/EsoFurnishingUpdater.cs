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
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

#region Internal Enumerations
internal enum ItemType
{
	Container = 18,
	Recipes = 29,
	Furnishing = 61,
}
#endregion

internal sealed partial class EsoFurnishingUpdater : ParsedPageJob
{
	#region Private Constants
	private const string CollectiblesQuery = $"SELECT convert(cast(convert(description using latin1) as binary) using utf8) description, furnCategory, furnLimitType, furnSubCategory, id itemId, itemLink resultitemLink, name, nickname, tags FROM collectibles WHERE furnCategory != ''";
	#endregion

	#region Static Fields
	private static readonly Dictionary<string, int> CommentCounts = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> FurnishingKeep = new(StringComparer.Ordinal)
	{
		"cat", "subcat", "id"
	};

	private static readonly HashSet<long> IgnoreIds = [194537];
	private static readonly string MinedItemsQuery = $"SELECT abilityDesc, bindType, convert(cast(convert(description using latin1) as binary) using utf8) description, furnCategory, furnLimitType, itemId, name, quality, resultitemLink, tags, type FROM uesp_esolog.minedItemSummary WHERE type IN({(int)ItemType.Container}, {(int)ItemType.Recipes}, {(int)ItemType.Furnishing})";

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
	private readonly Dictionary<long, Furnishing> collectibles = [];
	private readonly Dictionary<long, Furnishing> furnishings = [];
	private readonly Dictionary<string, long> nameLookup = new(StringComparer.Ordinal);
	private readonly List<string> fileMessages = [];
	private readonly List<string> pageMessages = [];
	private readonly TitleCollection missingIdExceptions;

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
	}
	#endregion

	#region Public Override Properties
	public override string LogName { get; } = "ESO Furnishing Update";
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

	protected override void BeforeLoadPages()
	{
		/*
		TitleCollection furnishingFiles = new(this.Site);
		furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-furnishing-");
		furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-item-furnishing-");
		*/

		foreach (var furnishing in Database.RunQuery(EsoLog.Connection, CollectiblesQuery, record => new Furnishing(record, this.Site, true)))
		{
			this.collectibles.Add(furnishing.Id, furnishing);
		}

		foreach (var furnishing in Database.RunQuery(EsoLog.Connection, MinedItemsQuery, record => new Furnishing(record, this.Site, false)))
		{
			this.furnishings.Add(furnishing.Id, furnishing);
		}

		var dupes = new HashSet<string>(StringComparer.Ordinal);
		this.FindDupes(this.collectibles, dupes);
		this.FindDupes(this.furnishings, dupes);
	}

	protected override string GetEditSummary(Page page) => "Update info from ESO database; remove cruft";

	protected override void LoadPages()
	{
		var title = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], TemplateName);
		this.Pages.GetBacklinks(title.FullPageName(), BacklinksTypes.EmbeddedIn);
		//// this.Pages.GetTitles("Online:10-Year Anniversary Banner, Medium");

		var top10 = CommentCounts.OrderByDescending(entry => entry.Value).Take(10);
		foreach (var comment in top10)
		{
			Debug.Write(comment.Value.ToStringInvariant() + ":  ");
			Debug.WriteLine(comment.Key);
			Debug.WriteLine(string.Empty);
		}
	}

	protected override void ParseText(SiteParser parser)
	{
		var template = this.ProcessMainTemplate(parser);
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

	private void FindDupes(Dictionary<long, Furnishing> items, HashSet<string> dupes)
	{
		foreach (var item in items)
		{
			var furnishing = item.Value;
			var labelName = furnishing.Title.LabelName();
			if (!dupes.Contains(labelName) && !this.nameLookup.TryAdd(labelName, item.Key))
			{
				dupes.Add(labelName);
				this.nameLookup.Remove(labelName);
			}
		}
	}

	private Furnishing? FindFurnishing(ITemplateNode template, Page page, string labelName)
	{
		Furnishing? retval = null;
		if (long.TryParse(template.GetValue("id"), NumberStyles.None, page.Site.Culture, out var id))
		{
			if (IgnoreIds.Contains(id))
			{
				return null;
			}

			if (!this.furnishings.TryGetValue(id, out retval) && !this.collectibles.TryGetValue(id, out retval))
			{
				Debug.WriteLine($"Furnishing ID {id} not found on page {SiteLink.ToText(page)}.");
			}
		}
		else if (!this.missingIdExceptions.Contains(page.Title))
		{
			Debug.WriteLine($"Furnishing ID on {SiteLink.ToText(page)} is missing or nonsensical.");
		}

		if (retval is null && this.nameLookup.TryGetValue(labelName, out var recoveredId))
		{
			Debug.WriteLine($"  Recovered ID {recoveredId} from {labelName}.");
			if (this.collectibles.TryGetValue(recoveredId, out retval) || this.furnishings.TryGetValue(recoveredId, out retval))
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
		if (this.FindFurnishing(template, page, labelName) is not Furnishing furnishing)
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
			Debug.WriteLine($"Page title != game title. Check for invalid ID or name change.\nPage: {page.Title}\nGame: {furnishing.Title.PageName}\n");
		}

		this.CheckImage(template, name, SiteLink.ToText(page, LinkFormat.LabelName));
		this.CheckTitle(page.Title, labelName, furnishing);

		template.Update("titlename", furnishing.TitleName, ParameterFormat.OnePerLine, true);
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

	private ITemplateNode ProcessMainTemplate(SiteParser parser)
	{
		var template = parser.FindTemplate(TemplateName) ?? throw new InvalidOperationException();
		if (this.GenericTemplateFixes(template))
		{
			this.Warn("Template has anonymous parameter on " + parser.Title);
		}

		this.FurnishingFixes(template, parser.Page);
		template.RemoveEmpties(FurnishingKeep);
		return template;
	}
	#endregion
}