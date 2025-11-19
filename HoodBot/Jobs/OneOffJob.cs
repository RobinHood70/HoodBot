namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Diagnostics;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

internal sealed class OneOffJob : WikiJob
{
	[JobInfo("One-Off Job")]
	public OneOffJob(JobManager jobManager)
		: base(jobManager, JobType.Write)
	{
	}

	#region Protected Override Methods
	protected override void Main()
	{
		var contributions = this.Site.User!.GetContributions(DateTime.Today.AddDays(-2), DateTime.MaxValue);
		var pages = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.FileInfo, false);
		var titles = contributions
			.Select(contribution => contribution.Title)
			.Where(title => title.Namespace == MediaWikiNamespaces.File);
		pages.GetTitles(titles);
		foreach (var page in pages)
		{
			if (page is FilePage filePage && filePage.FileRevisions.Count == 0)
			{
				page.Title.Delete("Page created in error");
			}
		}
	}
	#endregion
}