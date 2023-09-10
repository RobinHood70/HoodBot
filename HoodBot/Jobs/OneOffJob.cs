namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	internal sealed class OneOffJob : WikiJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager, JobType.Write)
		{
		}

		protected override void Main()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(MediaWikiNamespaces.Template);
			titles.Sort();
			foreach (var title in titles)
			{
				Debug.WriteLine(title);
			}
		}
		#endregion
	}
}