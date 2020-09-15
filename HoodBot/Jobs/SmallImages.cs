namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

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
					this.WriteLine(Invariant($"* {file.AsLink(true)}: {rev.Width} × {rev.Height}"));
				}
			}
		}
	}
}
