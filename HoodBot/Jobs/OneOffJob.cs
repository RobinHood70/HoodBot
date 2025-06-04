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

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Constants
	private const string FileName = "SalvagingRecipeList - Sheet1.csv";
	private const string HeaderText = "Salvaging Recipe";
	private const string ItemSummary = "Blades Item Summary";
	private const string TemplateName = "Blades " + HeaderText;
	#endregion

	#region Fields
	private readonly Dictionary<string, Dictionary<string, string>> rows = new(StringComparer.Ordinal);
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Add Blades recipes";

	public override string LogName => "One-Off Job";
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
			var id = row["ID"];
			if (id.Length == 0)
			{
				continue;
			}

			var key = GetKey(id, row["Divine"]);
			var newRow = new Dictionary<string, string>(StringComparer.Ordinal);
			for (var cellNum = 2; cellNum < row.Count; cellNum++)
			{
				newRow.Add(header[cellNum], row[cellNum]);
			}

			if (!this.rows.TryAdd(key, newRow))
			{
				Debug.WriteLine("There are multiple rows with the key: " + key);
			}
		}
	}

	protected override string GetEditSummary(Page page) => "Add recipes";

	protected override void LoadPages() => this.Pages.GetBacklinks("Template:" + ItemSummary, BacklinksTypes.EmbeddedIn);

	protected override void PageLoaded(Page page)
	{
		page.Text = page.Text.Replace("</includeonly>\n<noinclude>", "</includeonly><noinclude>", StringComparison.OrdinalIgnoreCase);
		var parser = new SiteParser(page, InclusionType.CurrentPage, false);
		if (parser.FindTemplate(ItemSummary) is not ITemplateNode template)
		{
			Debug.WriteLine($"{ItemSummary} not found on page {parser.Title}.");
			return;
		}

		var id = template.GetValue("id");
		if (id is null)
		{
			Debug.WriteLine($"No ID on {parser.Title}");
			return;
		}

		var divine = template.GetValue("divine") ?? string.Empty;
		var key = GetKey(id, divine);
		if (!this.rows.TryGetValue(key, out var row))
		{
			Debug.WriteLine($"{parser.Title}: ID {key} not found in sheet");
			return;
		}

		this.AddSection(parser, CreateRecipeText(row));
		this.rows.Remove(key); // Slow, but it'll do for this.
		parser.UpdatePage();
	}
	#endregion

	#region Private Static Methods
	private static string GetKey(string id, string divine) => id + (divine.Trim().Length == 0 ? string.Empty : "_divine");
	#endregion

	#region Private Static Methods

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
	#endregion

	#region Private Methods
	private void AddSection(SiteParser parser, string recipe)
	{
		var sections = parser.ToSections(3);
		if (sections.Count > 1)
		{
			var header = parser.Factory.HeaderNodeFromParts(3, HeaderText);
			var content = new WikiNodeCollection(parser.Factory, parser.Parse('\n' + recipe + "\n\n"));
			var newSection = new Section(header, content);
			sections.Insert(1, newSection);
			sections[0].Content.TrimEnd();
			sections[0].Content.AddText("\n\n");
			parser.FromSections(sections);
		}
		else
		{
			var method = "Noinclude";
			var index = parser.FindLastIndex(node => node is IIgnoreNode ignore && string.Equals(ignore.Value.ToLowerInvariant(), "<noinclude>", StringComparison.Ordinal)) + 1;
			if (index == 0)
			{
				method = "Stub";
				index = parser.FindIndex(node => node is ITemplateNode template && template.GetTitle(this.Site) == "Template:Stub");
				if (index == -1)
				{
					method = "Append";
					index = parser.Count;
				}
			}

			var insertText = $"==={HeaderText}===\n" + recipe + "\n\n";
			switch (method)
			{
				case "Append":
					insertText = "<noinclude>{{NewLeft}}\n" + insertText + "</noinclude>";
					break;
				case "Noinclude":
				case "Stub":
					if (parser.FindTemplate("NewLeft") is null)
					{
						insertText = "{{NewLeft}}\n" + insertText;
					}

					break;
			}

			parser.InsertText(index, insertText);
		}
	}
	#endregion
}