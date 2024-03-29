﻿namespace RobinHood70.HoodBot.Jobs
{
	using System.Globalization;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	internal sealed class SmallImages : WikiJob
	{
		[JobInfo("Find Small Images")]
		public SmallImages(JobManager jobManager)
			: base(jobManager, JobType.ReadOnly)
		{
		}

		protected override void Main()
		{
			PageCollection files = new(this.Site, PageModules.FileInfo);
			files.GetCategoryMembers("Category:Legends-Art", true);
			foreach (var page in files)
			{
				if (page is FilePage file && file.LatestFileRevision is FileRevision rev && rev.Height <= 512 && rev.Width <= 512)
				{
					this.WriteLine(string.Create(
						CultureInfo.InvariantCulture,
						$"* {SiteLink.ToText(file, LinkFormat.LabelName)}: {rev.Width} × {rev.Height}"));
				}
			}
		}
	}
}