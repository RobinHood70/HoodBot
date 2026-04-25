namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update license";

	protected override void LoadPages()
	{
		this.Pages.GetCategoryMembers("Starfield-Icons");
		this.Pages.GetCategoryMembers("Starfield-Icons-Planet Traits");
		this.Pages.GetCategoryMembers("Starfield-Logo Images");
		this.Pages.GetCategoryMembers("Starfield-Skill Images");
	}

	protected override void ParseText(SiteParser parser)
	{
		foreach (var template in parser.TemplateNodes)
		{
			var title = template.GetTitle(this.Site);
			if (title.Namespace != MediaWikiNamespaces.Template)
			{
				continue;
			}

			if (title.PageNameEquals("Sfwimage"))
			{
				template.SetTitle("sfimage");
			}
			else if (
				title.PageNameEquals("cc-by-sa-2.5") ||
				title.PageNameEquals("Cc-by-sa-3.0") ||
				title.PageNameEquals("Public Domain") ||
				title.PageNameEquals("sfimage"))
			{
			}
			else
			{
				Debug.WriteLine(parser.Title + ": No license found");
			}
		}
	}
	#endregion
}