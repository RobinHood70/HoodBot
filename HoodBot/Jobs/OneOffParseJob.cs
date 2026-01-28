namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Fields
	private readonly TitleDictionary<IDictionary<string, string>> data = [];
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update Item";

	protected override void LoadPages()
	{
		var fileName = LocalConfig.BotDataSubPath("Blades - Item Stats.csv");
		var csv = new CsvFile(fileName);
		foreach (var row in csv.ReadRows())
		{
			var page = row["page"];
			if (page.StartsWith("Divine ", StringComparison.Ordinal))
			{
				page = page[7..] + " (divine)";
			}

			var fields = row.ToDictionary();
			var newDict = TrimDictionary(fields);
			var title = TitleFactory.FromUnvalidated(this.Site, "Blades:" + page);
			this.data.Add(title, newDict);
		}

		this.Pages.GetTitles(this.data.ToTitleCollection(this.Site));
	}

	protected override void ParseText(SiteParser parser)
	{
		if (parser.Page.IsMissing)
		{
			Debug.WriteLine("Page does not exist: " + parser.Title);
			return;
		}

		parser.RemoveTemplates("Stub");
		parser.RemoveTemplates("Minimal");
		this.InsertItemStats(parser);
		TrimBeforeNoinclude(parser);
	}
	#endregion

	#region Private Static Methods
	private static Section CreateItemStatsSection(SiteParser parser, IDictionary<string, string> fields)
	{
		var template = parser.Factory.TemplateNodeFromParts("Blades Item Stats Table");
		foreach (var (key, value) in fields)
		{
			template.Add(key, value, ParameterFormat.OnePerLine);
		}

		var itemStats = Section.FromText(parser.Factory, 3, "Item Stats", "\n");
		itemStats.Content.Add(template);
		itemStats.Content.AddText("\n\n");

		return itemStats;
	}

	private static void TrimBeforeNoinclude(SiteParser parser)
	{
		if (parser.Count >= 2 &&
			parser[^1] is IIgnoreNode &&
			parser[^2] is ITextNode text)
		{
			text.Text = text.Text.TrimEnd();
		}
	}

	private static Dictionary<string, string> TrimDictionary(IDictionary<string, string> fields)
	{
		fields.Remove("page");
		var newDict = new Dictionary<string, string>(fields.Count, StringComparer.Ordinal);
		foreach (var (key, value) in fields)
		{
			if (value.Trim().Length != 0)
			{
				newDict.Add(key, value);
			}
		}

		return newDict;
	}
	#endregion

	#region Private Methods
	private void InsertItemStats(SiteParser parser)
	{
		var sections = parser.ToSections();
		var index = sections.IndexOf("Tempering Recipe");
		if (index == -1)
		{
			index = 1;
		}

		var fields = this.data[parser.Title];
		var itemStats = CreateItemStatsSection(parser, fields);
		sections.Insert(index, itemStats);
		parser.FromSections(sections);
	}
	#endregion
}