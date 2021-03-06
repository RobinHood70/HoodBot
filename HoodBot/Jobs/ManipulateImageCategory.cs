﻿namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	public class ManipulateImageCategory : WikiJob
	{
		[JobInfo("Manipulate Image Category")]
		public ManipulateImageCategory(JobManager jobManager)
			: base(jobManager) => this.Results = new PageResultHandler(this.Site, "User:Jeancey/Kah");

		protected override void Main()
		{
			// TODO: Switch to loading by TitleCollection, then save TitleCollection so reloads can be much faster.
			// Investigate why original attempt at this produced a recurring load that never completed.
			var list = new PageCollection(this.Site)
			{
				LoadOptions = new PageLoadOptions(PageModules.FileInfo)
			};

			list.GetCategoryMembers("Online-Icons", true);

			var smallImages = new List<FilePage>();
			foreach (var result in list)
			{
				if (result is FilePage image && image.LatestFileRevision is FileRevision imageInfo && imageInfo.Height < 64 && imageInfo.Width < 64)
				{
					smallImages.Add(image);
				}
			}

			smallImages.Sort(SimpleTitleComparer.Instance);
			foreach (var image in smallImages)
			{
				if (image.LatestFileRevision is FileRevision imageInfo)
				{
					this.WriteLine(Invariant($"* {image.AsLink(true)} ({imageInfo.Width}x{imageInfo.Height})"));
				}
			}
		}
	}
}