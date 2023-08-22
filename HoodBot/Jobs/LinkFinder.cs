namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;

	public class LinkFinder : LinkFinderJob
	{
		#region Constructors
		[JobInfo("Link Finder")]
		public LinkFinder(JobManager jobManager, string searches, [JobParameter(DefaultValue = true)] bool sectionLinksOnly)
			: base(jobManager, sectionLinksOnly)
		{
			if (!string.IsNullOrWhiteSpace(searches))
			{
				foreach (var search in searches.Split(TextArrays.Pipe))
				{
					this.Titles.Add(TitleFactory.FromUnvalidated(this.Site, search));
				}

				this.Titles.Sort();
			}
		}
		#endregion
	}
}