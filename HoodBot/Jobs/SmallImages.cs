namespace RobinHood70.HoodBot.Jobs
{
using System;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class SmallImages : WikiJob
	{
		[JobInfo("Find Small Images")]
		public SmallImages(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void Main()
		{
			var files = new PageCollection(this.Site, PageModules.FileInfo);
			files.GetCategoryMembers("Category:Legends-Art", true);
			foreach (var page in files)
			{
				if (page is FilePage file && file.LatestFileRevision is FileRevision rev && rev.Height <= 512 && rev.Width <= 512)
				{
					this.WriteLine(FormattableString.Invariant($"* {file.AsLink(true)}: {rev.Width} × {rev.Height}"));
				}
			}
		}
	}
}
