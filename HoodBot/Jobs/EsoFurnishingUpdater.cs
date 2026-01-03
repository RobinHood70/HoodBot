namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
	private const string CollectiblesQuery = "SELECT description, furnCategory, furnSubCategory, id, name, nickname, tags FROM collectibles WHERE furnLimitType = 2";
	private const string MinedItemsQuery = "SELECT abilityDesc, bindType, description, furnCategory, furnLimitType, itemId, name, quality, resultitemLink, tags FROM uesp_esolog.minedItemSummary WHERE type = 61 AND itemId NOT IN(115083, 119706, 120853, 152141, 152142, 152143, 152144, 152145, 152146, 152147, 152148, 152149, 153552, 153553, 153554, 153555, 153556, 153557, 153558, 153559, 153560, 153561, 153562, 183198, 220297, 220318, 220288, 220300, 220320, 220323) AND name NOT LIKE '% Station (%'"; // 61 = Furnishings
	#endregion

	#region Static Fields
	private static readonly Dictionary<string, int> CommentCounts = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> FurnishingKeep = new(StringComparer.Ordinal)
	{
		"cat", "subcat", "id"
	};

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
	private readonly TitleCollection deprecatedTitles;
	private readonly Dictionary<long, Furnishing> furnishings = [];
	private readonly List<string> fileMessages = [];
	private readonly List<string> pageMessages = [];
	private readonly bool removeUnneededStuff;
	private string? blank;
	#endregion

	#region Constructors
	[JobInfo("Furnishings", "ESO Update")]
	public EsoFurnishingUpdater(JobManager jobManager, bool removeUnneededStuff)
		: base(jobManager)
	{
		//// this.Shuffle = true;
		//// jobManager.ShowDiffs = false;
		var title = TitleFactory.FromUnvalidated(this.Site, jobManager.WikiInfo.ResultsPage + "/ESO Furnishings");
		this.SetTemporaryResultHandler(new PageResultHandler(title, false));
		this.deprecatedTitles = new(this.Site)
		{
			"Online:Dock Pulleys, Mounted",
			"Online:Goblin Totem",
			"Online:Orcish Shrine, Malacath",
		};
		this.removeUnneededStuff = removeUnneededStuff;
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

	protected override string? GetDisambiguator(Furnishing item) => item.Disambiguator;

	protected override string GetEditSummary(Page page) => "Update info from ESO database; remove cruft";

	protected override TitleDictionary<Furnishing> GetExistingItems()
	{
		var retval = new TitleDictionary<Furnishing>();
		var knownIds = new Dictionary<long, Title>();
		var pages = this.Site.CreateMetaPageCollection(PageModules.None, false, "collectible", "id");
		pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
		foreach (var title in this.deprecatedTitles)
		{
			pages.Remove(title);
		}

		foreach (var varPage in pages.Cast<VariablesPage>())
		{
			var collectible = varPage.GetVariable("collectible")?.Length > 0;
			if (varPage.GetVariable("id") is string idText &&
				long.TryParse(idText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, this.Site.Culture, out var id))
			{
				var furnId = Furnishing.GetKey(id, collectible);
				if (this.furnishings.TryGetValue(furnId, out var item))
				{
					if (!knownIds.TryAdd(Furnishing.GetKey(id, collectible), varPage.Title))
					{
						this.Warn($"Same id found on multiple pages: {id} on {varPage.Title} and {knownIds[id]}");
					}

					item.PageName = varPage.Title.PageName; // Make sure the item knows what page it belongs to.
					var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], item.PageName);
					retval.Add(title, item);
				}
				else
				{
					Debug.WriteLine($"{varPage.Title} has an unrecognized id");
				}
			}
			else
			{
				Debug.WriteLine($"{varPage.Title} has a non-numeric or missing id");
			}
		}

		return retval;
	}

	protected override void GetExternalData()
	{
		this.blank = this.LoadBlankFurnishingPage();
		this.GetFurnishingsFromDb();
		this.GetCollectiblesFromDb();
		this.CheckForDupes();
	}

	protected override TitleDictionary<Furnishing> GetNewItems()
	{
		var fileName = LocalConfig.BotDataSubPath("NewFurnishings.txt");
		if (!File.Exists(fileName))
		{
			return [];
		}

		var newItems = new TitleDictionary<Furnishing>();
		var file = new CsvFile(fileName);
		foreach (var row in file.ReadRows())
		{
			var id = long.Parse(row["id"], CultureInfo.InvariantCulture);
			var name = row["name"];

			var furnishing = this.furnishings[id];
			if (name.OrdinalEquals(furnishing.Name))
			{
				throw new InvalidOperationException("Furnishing name mismatch.");
			}

			var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], furnishing.PageName);
			newItems.Add(title, furnishing);
		}

		return newItems;
	}

	protected override string GetNewPageText(Title title, Furnishing item) => this.blank ?? throw new InvalidOperationException("Blank page text is null.");

	protected override bool IsValidPage(SiteParser parser, Furnishing item) => parser.FindTemplate(TemplateName) is not null;

	protected override void LoadPages()
	{
		base.LoadPages();
		var newPages = new TitleCollection(this.Site);
		foreach (var page in this.Pages)
		{
			if (page.IsMissing)
			{
				newPages.Add(page.Title);
			}
		}

		newPages.Sort();
		foreach (var title in newPages)
		{
			Debug.WriteLine("New Page: " + title.FullPageName());
		}

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
		var template = parser.FindTemplate(TemplateName) ?? throw new InvalidOperationException();
		template.UpdateIfEmpty("id", item.Id.ToStringInvariant(), ParameterFormat.OnePerLine);
		this.GenericTemplateFixes(template);
		if (template.Parameters.Any(p => p.Anonymous))
		{
			this.Warn("Template has anonymous parameter on " + parser.Title);
		}

		this.FurnishingFixes(template, parser.Page, item);
		if (this.removeUnneededStuff)
		{
			template.RemoveEmpties(FurnishingKeep);
			CheckComments(parser, template);
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

	private static string CheckName(Page page, ITemplateNode template, Furnishing item)
	{
		var labelName = page.Title.LabelName();

		// Prioritize existing name in template.
		if (template.GetValue("name") is string nameValue)
		{
			if (nameValue.OrdinalEquals(labelName))
			{
				template.Remove("name");
			}

			return nameValue;
		}

		// Otherwise, make sure the name is correct.
		if (!item.Name.OrdinalEquals(labelName))
		{
			template.Update("name", item.Name, ParameterFormat.OnePerLine, true);
			return item.Name;
		}

		// Failing that, just return the label name.
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
	private void CheckForDupes()
	{
		var test = new TitleDictionary<Furnishing>();
		var dupes = new Dictionary<long, Title>();
		foreach (var furnishing in this.furnishings.Values)
		{
			var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], furnishing.PageName);
			if (!test.TryAdd(title, furnishing))
			{
				// Leave item in list after check so additional dupes can be detected. Remove all dupes later.
				var dupe = test[title];
				if (furnishing.PageName.OrdinalEquals(dupe.PageName) &&
					furnishing.Disambiguator.OrdinalEquals(dupe.Disambiguator))
				{
					var key = Furnishing.GetKey(furnishing.Id, furnishing.Collectible);
					var key2 = Furnishing.GetKey(dupe.Id, dupe.Collectible);
					Debug.WriteLine($"{key} / {key2}: {title}");
					dupes.Add(key, title);
				}
			}
		}

		if (dupes.Count > 0)
		{
			this.StatusWriteLine("DUPES FOUND - see Debug output.");
		}
	}

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
		if (labelName.OrdinalEquals(furnishing.PageName) ||
			labelName.OrdinalEquals(Title.ToLabelName(furnishing.PageName)))
		{
			return;
		}

		this.pageMessages.Add($"[[{title.FullPageName()}|{labelName}]] ''should be''<br>\n" +
		  $"{furnishing.PageName}");
		if (!title.PageName.Contains(':', StringComparison.Ordinal) &&
			furnishing.PageName.Contains(':', StringComparison.Ordinal) &&
			title.PageName.Replace(',', ':').OrdinalEquals(furnishing.PageName))
		{
			Debug.WriteLine($"Page Replace Needed: {title.FullPageName()}\tOnline:{furnishing.PageName}");
		}
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
			var curText = param.GetValue().AsSpan();
			var splitOn = curText.Contains('~') ? '~' : ',';
			var list = new List<(string Name, int Value)>();
			var isName = true;
			var paramName = string.Empty;
			foreach (var paramValue in curText.Split(splitOn))
			{
				var text = curText[paramValue].ToString();
				if (isName)
				{
					paramName = text;
				}
				else
				{
					var stripped = string.Concat(text.Split(' ', '(', ')'));
					var intValue = stripped.Length == 0 ? 1 : int.Parse(stripped, this.Site.Culture);
					list.Add((paramName, intValue));
				}

				isName = !isName;
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

	private void FurnishingFixes(ITemplateNode template, Page page, Furnishing item)
	{
		ArgumentNullException.ThrowIfNull(page);
		var rawTemplate = template.ToRaw();
		var name = CheckName(page, template, item);
		CheckIcon(template, name);
		this.CheckImage(template, name, SiteLink.ToText(page, LinkFormat.LabelName));
		this.CheckTitle(page.Title, name, item);

		if (item.Collectible)
		{
			template.Update("nickname", item.NickName, ParameterFormat.OnePerLine, true);
		}

		if (item.Size is not null)
		{
			template.Update("size", item.Size, ParameterFormat.OnePerLine, false);
		}

		var desc = item.Description?.Replace("and and", "{{sic|and and|and}}", StringComparison.Ordinal);
		template.Update("desc", desc, ParameterFormat.OnePerLine, false);
		template.Update("cat", item.FurnishingCategory, ParameterFormat.OnePerLine, false);
		template.Update("subcat", item.FurnishingSubcategory, ParameterFormat.OnePerLine, false);

		CheckBehavior(template, item);

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

			var expectedName = craftWord + ": " + name;
			var planname = template.GetValue("planname");
			if (expectedName.OrdinalEquals(planname))
			{
				template.Remove("planname");
			}
		}

		if (template.GetValue("planquality") is string planquality && template.GetValue("quality").OrdinalEquals(planquality))
		{
			template.Remove("planquality");
		}

		if (item.Materials.Count > 0)
		{
			template.Update("materials", string.Join('~', item.Materials), ParameterFormat.OnePerLine, true);
		}

		if (item.Skills.Count > 0)
		{
			template.Update("skills", string.Join('~', item.Skills), ParameterFormat.OnePerLine, true);
		}

		var bindTypeValue = template.GetValue("bindtype");
		var bindType = (item.Collectible ||
			bindTypeValue.OrdinalEquals("0"))
				? null
				: item.BindType;
		if (bindType is not null)
		{
			template.Update("bindtype", bindType, ParameterFormat.OnePerLine, true);
		}

		if (item.FurnishingLimitType is not FurnishingType.SpecialCollectibles and not FurnishingType.CollectibleFurnishings)
		{
			template.Remove("collectible");
		}

		if (!rawTemplate.OrdinalEquals(template.ToRaw()))
		{
			// Only update these if something else changed.
			var wantsToBe = Furnishing.GetFurnishingLimitType(item.FurnishingLimitType);
			template.Update("furnLimitType", wantsToBe);
			template.Update("quality", item.Quality, ParameterFormat.OnePerLine, true);
		}
	}

	private void GenericTemplateFixes(ITemplateNode template)
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
	}

	private void GetCollectiblesFromDb()
	{
		var rows = Database.RunQuery(EsoLog.Connection, CollectiblesQuery, FurnishingFactory.FromCollectibleRow);
		foreach (var collectible in rows)
		{
			this.furnishings.Add(Furnishing.GetKey(collectible.Id, true), collectible);
		}
	}

	private void GetFurnishingsFromDb()
	{
		var rows = Database.RunQuery(EsoLog.Connection, MinedItemsQuery, FurnishingFactory.FromRow);
		foreach (var furnishing in rows)
		{
			this.furnishings.Add(Furnishing.GetKey(furnishing.Id, false), furnishing);
		}
	}

	private string LoadBlankFurnishingPage()
	{
		var blankPage = this.Site.LoadPage($"Template:{TemplateName}/Blank");
		var parser = new SiteParser(blankPage);
		var paramValue = parser.FindTemplate("Pre")?.Find(1)?.Value;
		return paramValue?.Count == 1 && paramValue[0] is ITagNode nowiki && nowiki.InnerText is string text
			? text.Trim()
			: throw new InvalidOperationException("Template blank not in expected format.");
	}
	#endregion
}