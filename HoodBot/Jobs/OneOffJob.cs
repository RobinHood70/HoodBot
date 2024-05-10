namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	[method: JobInfo("One-Off Job")]
	internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			var titles = new TitleCollection(this.Site, "User:Alpha Kenny Buddy/Sandbox1");
			var pages = titles.Load(PageModules.Links);
			var links = pages[0].Links;
			this.ProgressMaximum = links.Count;
			foreach (var link in links)
			{
				link.Delete("User-created name from mod");
				this.Progress++;
			}
		}
		#endregion
	}
}