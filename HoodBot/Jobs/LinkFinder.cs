namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	public class LinkFinder : LinkFinderJob
	{
		#region Fields
		private readonly TitleCollection searches;
		#endregion

		#region Constructors

		[JobInfo("Link Finder")]
		public LinkFinder(JobManager jobManager, string searches, [JobParameter(DefaultValue = true)] bool sectionLinksOnly)
			: base(jobManager, sectionLinksOnly)
		{
			ThrowNullOrWhiteSpace(searches, nameof(searches));
			this.searches = new TitleCollection(this.Site);
			foreach (var search in searches.Split(TextArrays.Pipe))
			{
				this.searches.Add(Title.FromName(this.Site, search));
			}

			this.searches.Sort();
		}
		#endregion

		protected override void LoadPages()
		{
			this.SetTitlesFromSubpages(this.searches);
			base.LoadPages();
		}
	}
}
