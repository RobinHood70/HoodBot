namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;

	using RobinHood70.WikiCommon;

	public class WatchTemplates : WikiJob
	{
		[JobInfo("Watch Template Space")]
		public WatchTemplates(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void Main()
		{
			var pages = this.Site.Watch(MediaWikiNamespaces.Template);
			foreach (var page in pages.Value)
			{
				Debug.WriteLine(page.PageName);
			}
		}
	}
}
