namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed partial class CastlesRulings : CreateOrUpdateJob<CastlesRulings.Ruling>
{
	#region Private Constants
	private const string RulingTemplate = "Castles Ruling";
	#endregion

	#region Static Fields
	private static readonly CultureInfo GameCulture = new("en-US");
	private static readonly string[] RulingsGroupNames = ["_requiredRulings", "_randomRulings", "_personalRulings", "_instantRulings", "_rewardRulings"];
	private static readonly Regex StartingLetters = new("^[A-Z]+", RegexOptions.None, Globals.DefaultRegexTimeout);
	private static readonly Dictionary<string, string> StyleReplacements = new(StringComparer.Ordinal)
	{
		/*
			<style=dropcap>
			<style=epic>
			<style=health>
			<style=link>
			<style=magicka>
			<style=quote>
			<style=stamina>
			<style=subject>
			<style=trait>
		*/
		["fire"] = "BF4A26",
		["frost"] = "385D82",
		["highlight"] = "A7762E",
		["item"] = "A7762E",
		["joke"] = "7B2235",
		["negative"] = "BC322E",
		["positive"] = "49790C",
		["prop"] = "C69C5F",
		["shock"] = "512E55",
	};

	private static readonly Dictionary<string, string> TxInfoOverrides = new(StringComparer.Ordinal)
	{
		["BookTitles"] = "{{Hover|{0}|<random book title>}}",
		["FirstEdition_Variations"] = "{{Hover|{0}|first edition}}",
		["INN001_EstablishmentName"] = "<random inn name>",
		["PR006_Joke_Variations"] = "<random joke>",
		["PR020_Dish_Variations"] = "{{Hover|{0}|<random dish>}}",
		["RoyalAddress"] = "{{Hover|{0}|<Royal Address>}}",
		["SR030_RulerFamilyMember"] = "{{Hover|{0}|<random family member>}}",
	};
	#endregion

	#region Fields
	private readonly Context context;
	private readonly CastlesData data = new(GameCulture);
	private readonly StringComparer subComparer;
	#endregion

	#region Constructors
	[JobInfo("Castles Rulings", "|Castles")]
	public CastlesRulings(JobManager jobManager)
		: base(jobManager)
	{
		this.context = new Context(this.Site);
		this.NewPageText = GetNewPageText;
		this.OnUpdate = this.UpdateRuling;
		this.subComparer = this.Site.GetStringComparer(false);
	}
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => null;
	#endregion

	#region Public Static Methods
	public static string WikiModeReplace(string text) => CastlesStyleReplacer()
		.Replace(text, StyleReplacer)
		.Replace('’', '\'');
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		var sorted = new SortedSet<string>(this.context.UnhandledMagicWords, StringComparer.Ordinal);
		foreach (var item in sorted)
		{
			Debug.WriteLine("Unhandled: " + item);
		}
	}

	protected override string GetEditSummary(Page page) => "Update rulings";

	protected override bool IsValid(SiteParser parser, Ruling item) => parser.FindSiteTemplate(RulingTemplate) is not null;

	protected override IDictionary<Title, Ruling> LoadItems()
	{
		foreach (var (key, value) in TxInfoOverrides)
		{
			this.data.Translator.ParserOverrides[key] = value;
		}

		var rulings = this.GetRulingGroups();
		this.UpdateRulingsPage(rulings);

		return rulings;
	}
	#endregion

	#region Private Static Methods
	[GeneratedRegex(@"<style=(?<style>\w+)>(?<content>.*?)</style>", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, Globals.DefaultGeneratedRegexTimeout)]
	private static partial Regex CastlesStyleReplacer();

	private static string CreateSectionText(TitleCollection titles)
	{
		if (titles.Count == 0)
		{
			return "\n";
		}

		titles.Sort();
		var sb = new StringBuilder();
		var lastTitle = string.Empty;
		foreach (var title in titles)
		{
			var startingLetters = StartingLetters.Match(title.SubPageName()).Value;
			if (startingLetters.OrdinalEquals("R"))
			{
				startingLetters = "RR";
			}

			if (!startingLetters.OrdinalEquals(lastTitle))
			{
				lastTitle = startingLetters;
				if (sb.Length > 0)
				{
					sb.Append('\n');
				}

				sb
					.Append("\n====")
					.Append(startingLetters)
					.Append("====");
			}

			sb
				.Append("\n{{")
				.Append(title.FullPageName())
				.Append("}}");
		}

		sb.Append("\n\n");
		return sb.ToString();
	}

	private static ITemplateNode UpdateChoiceTemplate(Choice choice, SiteParser parser, Dictionary<int, ITemplateNode> choiceDictionary, FlatteningComparer comparer)
	{
		if (!choiceDictionary.TryGetValue(choice.Id, out var template))
		{
			template = parser.Factory.TemplateNodeFromParts(
				"Castles Ruling/Choice",
				true,
				("id", choice.Id.ToStringInvariant()),
				("text", string.Empty),
				("conditions", string.Empty),
				("effects", string.Empty),
				("flageffects", string.Empty));
		}

		template.LooseUpdate("text", string.Join(" ''or''<br>\n", choice.SubChoices), ParameterFormat.OnePerLine, comparer);
		if (choice.Conditions.Count > 0)
		{
			template.LooseUpdate("conditions", string.Join("<br>\n", choice.Conditions), ParameterFormat.OnePerLine, comparer);
		}

		if (choice.EffectFlags.Count > 0)
		{
			template.LooseUpdate("flageffects", string.Join("<br>\n", choice.EffectFlags), ParameterFormat.OnePerLine, comparer);
		}

		return template;
	}

	private static string GetNewPageText(Title title, Ruling ruling) =>
		"{{Castles Ruling\n" +
		"|text=\n" +
		"|conditions=\n" +
		"|choices=\n" +
		"}}";

	private static List<Ruling> GetNewRulingsList(Dictionary<Title, Ruling> rulings, IReadOnlyList<Title> templates)
	{
		var retval = new List<Ruling>();
		foreach (var ruling in rulings)
		{
			if (!templates.Contains(ruling.Key))
			{
				retval.Add(ruling.Value);
			}
		}

		retval.Sort((r1, r2) => r1.Name.CompareTo(r2.Name));

		return retval;
	}

	private static Section GetUnsortedSection(SectionCollection sections)
	{
		if (sections.FindFirst("Unsorted") is Section section)
		{
			return section;
		}

		var insertLoc = sections.IndexOf("Gallery");
		if (insertLoc == -1)
		{
			insertLoc = sections.Count;
		}

		section = Section.FromText(sections.Factory, "Unsorted", string.Empty);
		sections.InsertWithSpaceBefore(insertLoc, section);
		return section;
	}

	private static Dictionary<string, List<Ruling>> RulingsToGroups(List<Ruling> rulings)
	{
		var retval = new Dictionary<string, List<Ruling>>(StringComparer.Ordinal);
		foreach (var ruling in rulings)
		{
			if (!retval.TryGetValue(ruling.Group, out var rulingList))
			{
				rulingList = [];
				retval[ruling.Group] = rulingList;
			}

			rulingList.Add(ruling);
		}

		return retval;
	}

	private static string StyleReplacer(Match match)
	{
		var style = match.Groups["style"].Value;
		return !StyleReplacements.TryGetValue(style, out var colour)
			? match.Value
			: $"{{{{FC|#{colour}|{match.Groups["content"].Value}}}}}";
	}
	#endregion

	#region Private Methods
	private void AddNewRulings(Dictionary<string, TitleCollection> subSectionDict, List<Ruling> newRulings)
	{
		var groups = RulingsToGroups(newRulings);
		foreach (var (groupName, rulingList) in groups)
		{
			var friendly = groupName
				.Replace("_", string.Empty, StringComparison.Ordinal)
				.Replace("Rulings", string.Empty, StringComparison.Ordinal)
				.UpperFirst(GameCulture);
			if (!subSectionDict.TryGetValue(friendly, out var titles))
			{
				titles = new TitleCollection(this.Site);
				subSectionDict.Add(friendly, titles);
			}

			foreach (var ruling in rulingList)
			{
				titles.Add(ruling.PageName);
			}
		}
	}

	private Dictionary<Title, Ruling> GetRulingGroups()
	{
		var retval = new Dictionary<Title, Ruling>(TitleComparer.Instance);
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "RulingsDefault2.json");
		foreach (var rulingsGroupName in RulingsGroupNames)
		{
			if (obj[rulingsGroupName] is JToken group)
			{
				foreach (var rulingObject in group)
				{
					var localRuling = new Ruling(rulingsGroupName, rulingObject, this.data);
					retval.Add(TitleFactory.FromUnvalidated(this.Site, localRuling.PageName), localRuling);
				}
			}
		}

		return retval;
	}

	private Page GetRulingsPage()
	{
		var pages = PageCollection.Unlimited(this.Site, PageModules.Default | PageModules.Templates, false);
		pages.GetTitles("Castles:Rulings");
		var rulingsPage = pages[0];
		return rulingsPage;
	}

	private Dictionary<string, TitleCollection> GetSubSections(Section unsortedSection)
	{
		var retval = new Dictionary<string, TitleCollection>(StringComparer.Ordinal);
		foreach (var section in unsortedSection.Content.ToSections(3))
		{
			var titles = new TitleCollection(this.Site);
			var transclusions = section.Content.FindAll<ITemplateNode>();
			foreach (var transclusion in transclusions)
			{
				var titleText = transclusion.TitleNodes.ToValue();
				titles.Add(titleText);
			}

			retval.Add(section.GetTitle() ?? string.Empty, titles);
		}

		return retval;
	}

	private string RemoveCruftBeforeCompare(string arg) => arg
		.Replace("<br>", string.Empty, StringComparison.Ordinal)
		.Trim();

	private void UpdateRuling(SiteParser parser, Ruling ruling)
	{
		this.context.Page = parser.Page;
		var comparer = new FlatteningComparer(this.context, this.subComparer)
		{
			ParseBeforeStringCompare = this.RemoveCruftBeforeCompare
		};

		var template = parser.FindSiteTemplate(RulingTemplate) ?? throw new InvalidOperationException("Template not found.");

		template.LooseUpdate("text", ruling.Text, ParameterFormat.OnePerLine, comparer);

		if (ruling.Conditions.Count > 0)
		{
			var newConditions = string.Join("<br>\n", ruling.Conditions);
			template.LooseUpdate("conditions", newConditions, ParameterFormat.OnePerLine, comparer);
		}

		var choiceParam = template.Find("choices") ?? throw new InvalidOperationException();
		var choiceDictionary = new Dictionary<int, ITemplateNode>();
		foreach (var choiceTemplate in choiceParam.Value.FindAll<SiteTemplateNode>(t => t.Title.PageNameEquals("Castles Ruling/Choice")))
		{
			if (choiceTemplate.GetValue("id") is string choiceId)
			{
				choiceDictionary.Add(int.Parse(choiceId, this.Site.Culture), choiceTemplate);
			}
		}

		var newNodes = new WikiNodeCollection(parser.Factory);
		foreach (var choice in ruling.Choices)
		{
			var choiceTemplate = UpdateChoiceTemplate(choice, parser, choiceDictionary, comparer);
			newNodes.Add(choiceTemplate);
			newNodes.AddText("\n");
		}

		choiceParam.Value.Clear();
		choiceParam.Value.AddRange(newNodes);
	}

	private void UpdateRulingsPage(Dictionary<Title, Ruling> rulings)
	{
		var rulingsPage = this.GetRulingsPage();
		this.Pages.Add(rulingsPage);

		var oldPage = new SiteParser(rulingsPage);
		var parser = new SiteParser(rulingsPage);
		var sections = parser.ToSections(2);
		var unsortedSection = GetUnsortedSection(sections);
		var subSectionDict = this.GetSubSections(unsortedSection);
		var newRulings = GetNewRulingsList(rulings, rulingsPage.Templates);
		this.AddNewRulings(subSectionDict, newRulings);

		var newSections = new SectionCollection(parser.Factory, 3);
		foreach (var (sectionName, titles) in subSectionDict)
		{
			var sectionText = CreateSectionText(titles);
			var nullName = sectionName.Length == 0 ? null : sectionName;
			var newSection = Section.FromText(parser.Factory, 3, nullName, sectionText);
			newSections.Add(newSection);
		}

		unsortedSection.Content.FromSections(newSections);
		parser.FromSections(sections);
		parser.UpdatePage();
		var replacer = new UespReplacer(this.Site, oldPage, parser);
		foreach (var warning in replacer.Compare(parser.Title.FullPageName()))
		{
			this.Warn(warning);
		}
	}
	#endregion

	#region Internal Classes
	internal sealed class Choice
	{
		#region Fields
		private readonly CastlesData data;
		#endregion

		#region Constructors
		public Choice(JToken choiceObject, CastlesData data)
		{
			this.data = data;
			var uid = choiceObject.MustHave("_rulingChoiceTemplateUid");
			this.Id = (int?)uid["id"] ?? 0;
			var choiceShort = choiceObject.MustHaveString("_rulingChoiceDescription") ?? throw new InvalidDataException();
			if (data.Translator.GetSentence(choiceShort) is not string choiceDesc)
			{
				choiceDesc = choiceShort + CastlesData.NotFound;
			}

			var text = data.Translator.Parse(choiceDesc, true);
			text = WikiModeReplace(text);

			this.SubChoices.AddRange(text.Split("<newline>"));
			var conditions = new CastlesConditions(this.data, GameCulture);
			conditions.AddChoiceInfo(choiceObject);
			this.Conditions = conditions;
			this.EffectFlags = conditions.EffectFlags;
		}
		#endregion

		#region Public Properties
		public List<string> Conditions { get; }

		public List<string> EffectFlags { get; }

		public int Id { get; }

		public List<string> SubChoices { get; } = [];
		#endregion
	}

	internal sealed class Choices : KeyedCollection<int, Choice>
	{
		protected override int GetKeyForItem(Choice item) => item.Id;
	}

	internal sealed class Ruling
	{
		public Ruling(string group, JToken rulingObject, CastlesData data)
		{
			this.Group = group;
			this.Name = WikiTextUtilities.DecodeAndNormalize(rulingObject.MustHaveString("_debugRulingName") ?? string.Empty);

			var descId = rulingObject.MustHaveString("_rulingDescription");
			if (data.Translator.GetSentence(descId) is not string translated)
			{
				translated = descId + CastlesData.NotFound;
			}

			this.Text = WikiModeReplace(data.Translator.Parse(translated, true));
			this.Conditions = new CastlesConditions(data, GameCulture);
			this.Conditions.AddRulingInfo(rulingObject);
			var rulingChoices = rulingObject.MustHave("_rulingChoices");
			foreach (var choiceObject in rulingChoices)
			{
				this.Choices.Add(new Choice(choiceObject, data));
			}
		}

		public Choices Choices { get; } = [];

		public CastlesConditions Conditions { get; }

		public string Group { get; }

		public string Name { get; }

		public string PageName => "Castles:Rulings/" + this.Name;

		public string Text { get; }
	}
	#endregion
}