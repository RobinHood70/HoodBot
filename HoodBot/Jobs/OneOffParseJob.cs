namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Constants
	private const string FileName = "SalvagingRecipeList - Sheet1.csv";
	private const string HeaderText = "Salvaging Recipe";
	private const string ItemSummary = "Blades Item Summary";
	private const string TemplateName = "Blades " + HeaderText;
	#endregion

	#region Static Fields
	private static readonly HashSet<string> NoLevelFields = new(StringComparer.Ordinal)
	{
		"material01", "material02", "minquantity01", "minquantity02", "maxquantity01", "maxquantity02"
	};
	#endregion

	#region Fields
	private readonly Dictionary<string, Dictionary<string, string>> rows = new(StringComparer.Ordinal);
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Add Blades recipes";
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		foreach (var row in this.rows)
		{
			Debug.WriteLine(row.Key + " not on wiki");
		}
	}

	protected override void BeforeLoadPages()
	{
		var csvFile = new CsvFile(LocalConfig.BotDataSubPath(FileName));
		List<string>? header = null;
		foreach (var row in csvFile.ReadRows())
		{
			header ??= [.. csvFile.Header!];
			var divine = row["Divine/Decoration"].Trim();
			var id = row["ID"];
			if (id.Length == 0)
			{
				continue;
			}

			var key = GetKey(id, divine.OrdinalEquals("1"));
			var newRow = new Dictionary<string, string>(StringComparer.Ordinal);
			var noLevel = divine.OrdinalEquals("2") || divine.OrdinalEquals("3");
			if (noLevel)
			{
				newRow.Add("nolevel", "1");
			}

			for (var cellNum = 2; cellNum < row.Count; cellNum++)
			{
				if (!noLevel || NoLevelFields.Contains(header[cellNum]))
				{
					var cellValue = row[cellNum];
					if (!string.IsNullOrWhiteSpace(cellValue))
					{
						newRow.Add(header[cellNum], cellValue);
					}
				}
			}

			if (!this.rows.TryAdd(key, newRow))
			{
				Debug.WriteLine("There are multiple rows with the key: " + key);
			}
		}
	}

	protected override string GetEditSummary(Page page) => "Add recipes";

	protected override void LoadPages()
	{
		this.Pages.GetBacklinks("Template:" + ItemSummary, BacklinksTypes.EmbeddedIn);
		foreach (var page in this.Pages)
		{
			page.Text = page.Text.Replace("</includeonly>\n<noinclude>", "</includeonly><noinclude>", StringComparison.OrdinalIgnoreCase);
		}
	}

	protected override void ParseText(SiteParser parser)
	{
		if (parser.FindTemplate(ItemSummary) is not ITemplateNode template)
		{
			Debug.WriteLine($"{ItemSummary} not found on page {parser.Title}.");
			return;
		}

		var id = template.GetValue("id");
		if (string.IsNullOrEmpty(id))
		{
			Debug.WriteLine($"No ID on {parser.Title}");
			return;
		}

		var divine = template.GetValue("divine") ?? string.Empty;
		var key = GetKey(id, divine.Length > 0);
		if (!this.rows.TryGetValue(key, out var row))
		{
			Debug.WriteLine($"{parser.Title}: ID {key} not found in sheet");
			return;
		}

		this.AddSection(parser, CreateRecipeText(row));
		this.rows.Remove(key); // Slow, but it'll do for this.
	}
	#endregion

	#region Private Static Methods
	private static void CreateFakeFooter(Site site, SectionCollection sections)
	{
		var lastSection = sections[^1];
		var index = lastSection.Content.FindIndex(node => node is ITemplateNode template && template.GetTitle(site) == "Template:Stub");
		if (index == -1)
		{
			index = lastSection.Content.FindLastIndex(node => node is IIgnoreNode ignore && string.Equals(ignore.Value.ToLowerInvariant(), "</noinclude>", StringComparison.Ordinal));
		}

		if (index != -1)
		{
			var footerContent = new WikiNodeCollection(lastSection.Content.Factory, lastSection.Content[index..]);
			var footerSection = new Section(null, footerContent);
			sections.Add(footerSection);
			lastSection.Content.RemoveRange(index, footerContent.Count);
		}
	}

	private static string CreateRecipeText(Dictionary<string, string> row)
	{
		var recipe = new StringBuilder();
		recipe.Append($"{{{{{TemplateName}\n");
		foreach (var kvp in row)
		{
			recipe
				.Append('|')
				.Append(kvp.Key)
				.Append('=')
				.Append(kvp.Value)
				.Append('\n');
		}

		recipe.Append("}}");
		return recipe.ToString();
	}

	private static string GetKey(string id, bool divine) => id + (divine ? "_divine" : string.Empty);

	private static void InsertIntoParser(SiteParser parser, Section newSection)
	{
		var insertText = newSection.ToRaw();
		insertText = "<noinclude>\n{{NewLeft}}\n" + insertText + "</noinclude>";
		parser.AddParsed(insertText);
	}
	#endregion

	#region
	private void AddSection(SiteParser parser, string recipe)
	{
		var header = parser.Factory.HeaderNodeFromParts(3, HeaderText);
		var content = new WikiNodeCollection(parser.Factory, parser.Parse('\n' + recipe));
		var newSection = new Section(header, content);

		var sections = parser.ToSections(3);
		CreateFakeFooter(parser.Site, sections);
		if (sections.Count > 1)
		{
			newSection.Content.AddText("\n\n");
			this.InsertIntoSections(sections, newSection);
			parser.FromSections(sections);
		}
		else
		{
			InsertIntoParser(parser, newSection);
		}
	}

	private void InsertIntoSections(SectionCollection sections, Section newSection)
	{
		var insertLoc = -1;
		for (var i = 0; i < sections.Count; i++)
		{
			var section = sections[i];
			if (section.GetTitle().OrdinalICEquals("Tempering Recipe"))
			{
				insertLoc = i + 1;
				break;
			}
		}

		if (insertLoc == -1)
		{
			var lastSectionIndex = sections.Count - 1;
			insertLoc = sections[lastSectionIndex].Header is null
				? lastSectionIndex
				: 1;

			// Note that this can happen via either branch
			if (insertLoc == 1 && sections[0].Content.FindTemplate(this.Site, "NewLeft") is null)
			{
				sections[0].Content.TrimEnd();
				sections[0].Content.AddText("\n{{NewLeft}}\n");
			}
		}

		sections.Insert(insertLoc, newSection);
	}
	#endregion
}