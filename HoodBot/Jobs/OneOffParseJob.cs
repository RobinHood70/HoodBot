namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update ID to xx";

	protected override void LoadPages()
	{
		var title = TitleFactory.FromUnvalidated(this.Site, "Oblivion Mod:Order of the Dragon/Items");
		this.Pages.GetPageLinks([title]);
		for (var i = this.Pages.Count - 1; i >= 0; i--)
		{
			if (!this.Pages[i].Title.PageName.StartsWith("Order of the Dragon/", StringComparison.CurrentCulture))
			{
				this.Pages.RemoveAt(i);
			}
		}
	}

	protected override void ParseText(SiteParser parser)
	{
		foreach (var textNode in parser.TextNodes)
		{
			textNode.Text = Regex.Replace(textNode.Text, @"([|(\n]\s*)01([0-9A-Fa-f]{6})", "$1xx$2", RegexOptions.None, Globals.DefaultRegexTimeout);
		}
	}
	#endregion
}