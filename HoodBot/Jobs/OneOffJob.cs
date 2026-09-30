namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Fields
	private readonly Dictionary<string, string> idNameDict = new(StringComparer.Ordinal);
	private readonly SortedSet<string> output = new(StringComparer.Ordinal);
	private readonly HashSet<string> wikiIds = new(StringComparer.Ordinal);
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		this.GetCSV();
		var titles = this.GetTitles();
		var pages = new PageCollection(this.Site);
		pages.GetTitles(titles);
		this.CheckPages(pages);
		this.CheckIds();
		this.WriteLine(string.Join('\n', this.output));
	}
	#endregion

	#region Private Static Methods
	private static string FormatId(string id) => "'''" + id + "'''";

	private static string FormatTitle(Title title) => SiteLink.ToText(title, LinkFormat.LabelName);
	#endregion

	#region Private Methods
	private void CheckIds()
	{
		foreach (var (id, bookName) in this.idNameDict)
		{
			if (!this.wikiIds.Contains(id))
			{
				this.output.Add($"* Spreadsheet ID {FormatId(id)} for {this.FormatName(bookName)} not found on wiki");
			}
		}
	}

	private void CheckPages(PageCollection pages)
	{
		foreach (var page in pages)
		{
			if (!page.Exists)
			{
				this.output.Add($"* Page {FormatTitle(page.Title)} not found");
				continue;
			}

			var parser = new SiteParser(page);
			if ((parser.FindTemplate("Game Book") ?? parser.FindTemplate("Game Book Compilation")) is not TemplateNode template)
			{
				this.output.Add($"* Game Book not found on {FormatTitle(page.Title)}");
				continue;
			}

			for (var i = 1; i <= 5; i++)
			{
				var idValue = template.GetValue(i == 1 ? "id" : $"id{i}");
				this.CheckParameter(idValue, page.Title);
			}
		}
	}

	private void CheckParameter(string? idValue, Title title)
	{
		if (idValue is null)
		{
			return;
		}

		idValue = idValue.Split(TextArrays.Space)[0];
		this.wikiIds.Add(idValue);
		if (this.idNameDict.TryGetValue(idValue, out var idPage))
		{
			if (!idPage.OrdinalEquals(title.PageName))
			{
				this.output.Add($"* Wiki ID {FormatId(idValue)} on {FormatTitle(title)} does not match ID in the spreadsheet");
			}
		}
		else
		{
			this.output.Add($"* No match: Wiki ID {FormatId(idValue)} on {FormatTitle(title)} didn't match any ID in the spreadsheet");
		}
	}

	private string FormatName(string bookName)
	{
		var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.TamrielData], bookName);
		return FormatTitle(title);
	}

	private void GetCSV()
	{
		var csv = new CsvFile(LocalConfig.BotDataSubPath("OuranOS's Grand Literature Survey - Tamriel_Data.csv"))
		{
			HasHeader = true
		};

		foreach (var row in csv.ReadRows())
		{
			var id = row["ID"];
			var bookName = id switch
			{
				"T_Note_TheWarOfBetony1TR" => "The War of Betony (Fav'te)",
				"T_Note_TheWarOfBetony2TR" => "The War of Betony (Newgate)",
				_ => row["Book name"]
			};

			bookName = bookName switch
			{
				"The Legend of Lover's Lament" => "The Legend of Lovers Lament",
				"Wabbajack" => "Wabbajack (book)",
				_ => bookName
			};

			this.idNameDict.Add(id, bookName);
		}
	}

	private TitleCollection GetTitles()
	{
		var retval = new TitleCollection(this.Site);
		foreach (var bookName in this.idNameDict.Values)
		{
			var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.TamrielData], bookName);
			retval.TryAdd(title);
		}

		return retval;
	}
	#endregion
}