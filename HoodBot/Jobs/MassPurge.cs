namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
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
			: base(jobManager)
		{
			pages = pages.NotNull().Replace("\r\n", "\n", StringComparison.Ordinal);
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
			const int chunkSize = 10;
			this.ProgressMaximum = this.titles.Count;
			int i;
			for (i = 0; i < this.titles.Count; i += chunkSize)
			{
				int j;
				var titlesChunk = new List<Title>(chunkSize);
				for (j = 0; j < chunkSize && ((i + j) < this.titles.Count); j++)
				{
					titlesChunk.Add(this.titles[i + j]);
				}

				PageCollection.Purge(this.Site, titlesChunk, this.purgeMethod);
				this.Progress = i + j;
			}
		}
		#endregion
	}
}
