namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;

internal sealed class OneOffJob : EditJob
{
	#region Fields
	private readonly TitleCollection searchTitles;
	#endregion

	#region Constructors
	[JobInfo("One-Off Job")]
	public OneOffJob(JobManager jobManager)
		: base(jobManager)
	{
		this.searchTitles = new TitleCollection(this.Site, "Tamriel Rebuilt:Firewatch Library", "Tamriel Rebuilt:Helnim Hall", "Tamriel Rebuilt:Telvanni Library");
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Remove references to deprecated locations";

	protected override void LoadPages()
	{
		this.Pages.GetBacklinks("Tamriel Rebuilt:Firewatch Library", BacklinksTypes.Backlinks);
		this.Pages.GetBacklinks("Tamriel Rebuilt:Helnim Hall", BacklinksTypes.Backlinks);
		this.Pages.GetBacklinks("Tamriel Rebuilt:Telvanni Library", BacklinksTypes.Backlinks);
	}

	protected override void PageLoaded(Page page)
	{
		var lines = page.Text.Split(TextArrays.LineFeed);
		var newLines = new List<string>(lines.Length);
		foreach (var line in lines)
		{
			var parser = new SiteParser(page, line);
			var found = false;
			foreach (var title in this.searchTitles)
			{
				if (line.Length > 0 && line[0] == '*')
				{
					found |= parser.FindLink(title) is not null;
				}
			}

			if (!found)
			{
				newLines.Add(line);
			}
		}

		page.Text = string.Join('\n', newLines);
	}
	#endregion
}