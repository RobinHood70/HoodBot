﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Find On Pages")]
	internal sealed class FindOnPages(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			PageCollection pages = new(this.Site);
			pages.GetBacklinks("Template:Online Ingredient Summary", BacklinksTypes.EmbeddedIn);
			if (pages.Count == 0)
			{
				Debug.WriteLine("No pages returned!");
			}
			else
			{
				var found = false;
				foreach (var page in pages)
				{
					if (page.Text.Contains(":image", StringComparison.OrdinalIgnoreCase) ||
						page.Text.Contains(":width", StringComparison.OrdinalIgnoreCase))
					{
						Debug.WriteLine(page.Title.FullPageName());
						found = true;
					}
				}

				if (!found)
				{
					Debug.WriteLine("Nothing found!");
				}
			}
		}
		#endregion
	}
}