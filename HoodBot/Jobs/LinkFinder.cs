namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby.Design;

public class LinkFinder : LinkFinderJob
{
	#region Constructors
	[JobInfo("Link Finder")]
	public LinkFinder(JobManager jobManager, string searches, [JobParameter(DefaultValue = false)] bool sectionLinksOnly)
		: base(jobManager, sectionLinksOnly)
	{
		if (!string.IsNullOrWhiteSpace(searches))
		{
			foreach (var search in searches.Split(Environment.NewLine))
			{
				this.Titles.Add(TitleFactory.FromUnvalidated(this.Site, search));
			}

			this.Titles.Sort();
		}
	}
	#endregion
}