namespace RobinHood70.HoodBot.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Update Crown Crate Unknowns")]
internal sealed class UpdateCrownCrateUnknowns(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Constants
	private const string EsoCrateCard = "ESO Crate Card";
	private const string EsoCrateCardList = "ESO Crate Card List";
	#endregion

	#region Fields
	private readonly CardInfos cardInfos = [];
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update from Unused category";

	protected override void LoadPages()
	{
		var cardPages = new PageCollection(this.Site)
			.GetCategoryMembers("Online-Crown Crate Images-Unused", CategoryMemberTypes.File, false);
		foreach (var page in cardPages)
		{
			var parser = new SiteParser(page);
			var template = parser.FindTemplate("ESO Card Summary");
			if (template is null)
			{
				Debug.WriteLine("Template not found on " + page.Title.FullPageName());
				continue;
			}

			var type = template.GetValue("type");
			if (string.IsNullOrEmpty(type))
			{
				Debug.WriteLine("No type found for " + page.Title.FullPageName());
				continue;
			}

			var subtype = template.GetValue("subtype");
			if (string.IsNullOrEmpty(subtype))
			{
				subtype = "Unknown";
			}

			var cardName = page.Title.PageName[8..^4];
			if (subtype.OrdinalICEquals("Dwarven"))
			{
				subtype = "Dwarven Spiders";
			}

			this.cardInfos.Add(new CardInfo(cardName, type, subtype));
		}

		this.Pages.GetTitles("Online:Crown Crates/Unknown");
	}

	protected override void ParseText(SiteParser parser)
	{
		// Total hack, but it'll do.
		parser.RemoveTemplates("Online Crown Crates");

		var crateCard = TitleFactory.FromTemplate(this.Site, EsoCrateCard);
		var crateCardList = TitleFactory.FromTemplate(this.Site, EsoCrateCardList);
		this.RemoveExisting(parser);
		var dict = CreateDictionary(parser);
		foreach (var cardInfo in this.cardInfos)
		{
			var subsection = FindOrCreateSection(dict, parser, cardInfo);
			foreach (var crateCardTemplate in subsection.Content.FindTemplates(crateCard))
			{
				crateCardTemplate.SetTitle(EsoCrateCardList + "\n");
			}

			if (subsection.Content.FindTemplate(crateCardList) is not ITemplateNode template)
			{
				Debug.WriteLine($"Template not found on {cardInfo.Type}/{subsection.GetTitle()}.");
				continue;
			}

			var newParam = parser.Factory.ParameterNodeFromParts(cardInfo.CardName);
			var blank = parser.Factory.ParameterNodeFromParts("\n");
			template.Parameters.Add(newParam);
			template.Parameters.Add(blank);
		}

		parser.FromSections(dict.Values);
		SortTemplates(parser);

		parser.AddText("{{Online Crown Crates}}");
	}
	#endregion

	#region Private Static Methods
	private static Dictionary<string, Section> CreateDictionary(SiteParser parser)
	{
		var sectionDict = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
		var sections = parser.ToSections(2);
		foreach (var section in sections)
		{
			var title = section.Header.GetTitle(true) ?? string.Empty;
			sectionDict.Add(title, section);
		}

		return sectionDict;
	}

	private static Section FindOrCreateSection(Dictionary<string, Section> dict, SiteParser parser, CardInfo cardInfo)
	{
		if (!dict.TryGetValue(cardInfo.Type, out var typeSection))
		{
			typeSection = Section.FromText(parser.Factory, cardInfo.Type, "\n");
			dict.Add(cardInfo.Type, typeSection);
		}

		var subsections = typeSection.Content.ToSections(3);
		if (subsections.FindFirst([cardInfo.SubType, "Other"]) is Section subsection)
		{
			return subsection;
		}

		subsection = Section.FromText(parser.Factory, 3, cardInfo.SubType, $"\n{{{{{EsoCrateCardList}\n}}}}\n\n");
		subsections.Add(subsection);
		typeSection.Content.FromSections(subsections);

		return subsection;
	}

	private static void SortTemplates(SiteParser parser)
	{
		foreach (var template in parser.FindTemplates(EsoCrateCardList))
		{
			var lines = template.ToRaw().Split(TextArrays.NewLineChars);
			var realLines = new List<string>(lines[1..^1]);
			realLines.Sort(NaturalSort.Instance);
			template.Parameters.Clear();
			foreach (var line in realLines)
			{
				var paramSplit = line.Split(TextArrays.Pipe);
				if (paramSplit.Length != 3)
				{
					throw new InvalidOperationException("WTF?");
				}

				var param1 = parser.Factory.ParameterNodeFromParts(paramSplit[1]);
				var param2 = parser.Factory.ParameterNodeFromParts(paramSplit[2] + '\n');
				template.Parameters.AddRange(param1, param2);
			}
		}
	}
	#endregion

	#region Private Methods
	private void RemoveExisting(SiteParser parser)
	{
		foreach (var cluster in parser
			.FindTemplates(EsoCrateCardList)
			.SelectMany(template => template.ParameterCluster(2)))
		{
			var name = cluster[0].GetValue();
			this.cardInfos.Remove(name);
		}

		foreach (var template in parser.FindTemplates(EsoCrateCard))
		{
			if (template.GetValue(1) is string param1)
			{
				this.cardInfos.Remove(param1);
			}
		}
	}
	#endregion

	#region Private Records
	private record CardInfo(string CardName, string Type, string SubType);
	#endregion

	#region Private Classes
	private sealed class CardInfos : KeyedCollection<string, CardInfo>
	{
		protected override string GetKeyForItem(CardInfo item) => item.CardName;
	}
	#endregion
}