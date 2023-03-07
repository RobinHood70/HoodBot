namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	internal sealed class MassPurge : WikiJob
	{
		#region Fields
		private readonly TitleCollection titles;
		private readonly PurgeMethod purgeMethod;
		#endregion

		#region Constructors

		[JobInfo("Mass Purge")]
		public MassPurge(JobManager jobManager, string pages, bool linkUpdate, bool recursive)
			: base(jobManager, JobType.UnloggedWrite)
		{
			ArgumentNullException.ThrowIfNull(pages);
			pages = pages.Replace("\r\n", "\n", StringComparison.Ordinal);
			var values = pages.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			this.titles = new TitleCollection(this.Site, values);
			this.purgeMethod =
				recursive ? PurgeMethod.RecursiveLinkUpdate :
				linkUpdate ? PurgeMethod.LinkUpdate :
				PurgeMethod.Normal;
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.ProgressMaximum = 1;
			_ = PageCollection.Purge(this.Site, this.titles, this.purgeMethod, 5);
			this.Progress++;
		}
		#endregion
	}
}
