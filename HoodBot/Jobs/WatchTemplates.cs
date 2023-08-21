namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.WikiCommon;

	internal sealed class WatchTemplates : WikiJob
	{
		[JobInfo("Watch Template Space")]
		public WatchTemplates(JobManager jobManager)
			: base(jobManager, JobType.UnloggedWrite)
		{
		}

		protected override void Main()
		{
			var pages = this.Site.Watch(MediaWikiNamespaces.Template);
			foreach (var page in pages.Value)
			{
				Debug.WriteLine(page.Title.PageName);
			}
		}
	}
}
