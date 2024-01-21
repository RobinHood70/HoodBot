namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;

	[method: JobInfo("One-Off Job")]
	internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
	{
		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create redirect";

		protected override void LoadPages()
		{
			var fileName = LocalConfig.BotDataSubPath("RedirectList.txt");
			var pageNames = File.ReadAllLines(fileName);
			var unique = new HashSet<string>(StringComparer.Ordinal);
			foreach (var pageName in pageNames)
			{
				unique.Add("Starfield:" + pageName);
			}

			this.Pages.GetTitles(unique);
		}

		protected override void PageLoaded(Page page)
		{
			if (page.Exists)
			{
				if (!page.Text.Contains("Starfield-Items-Keys", StringComparison.Ordinal))
				{
					this.Warn($"Page {page.Title.FullPageName()} exists!");
				}
			}
			else
			{
				page.Text = $"#REDIRECT [[Starfield:Keys#{page.Title.PageName}]] [[Category:Redirects to Broader Subjects]] [[Category:Starfield-Items-Keys]]";
			}

		}
		#endregion
	}
}