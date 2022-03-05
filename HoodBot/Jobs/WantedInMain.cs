namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class WantedInMain : WikiJob
	{
		#region Constructors
		[JobInfo("Wanted Pages in Main Space", "Maintenance")]
		public WantedInMain(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			TitleCollection titles = new(this.Site);
			titles.GetQueryPage("Wantedpages");
			List<string> sorted = new();
			foreach (var title in titles)
			{
				if (title.Namespace == MediaWikiNamespaces.Main)
				{
					var uri = Uri.EscapeDataString(title.FullPageName() + '?');
					sorted.Add($"* [https://en.uesp.net/wiki/Special:WhatLinksHere/{uri} {title.FullPageName()}]");
				}
			}

			sorted.Sort(StringComparer.Ordinal);
			foreach (var item in sorted)
			{
				this.WriteLine(item);
			}
		}
		#endregion
	}
}