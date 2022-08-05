namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class OneOffJob : WikiJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void Main()
		{
			var pages = new PageCollection(this.Site, PageModules.Backlinks);
			pages.GetQueryPage("Unusedimages");
			pages.Sort();
			foreach (var page in pages)
			{
				if (page.Backlinks.Count > 0)
				{
					this.WriteLine("* " + page.AsLink());
				}
			}
		}
		#endregion
	}
}