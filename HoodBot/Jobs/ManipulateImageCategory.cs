namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class ManipulateImageCategory : WikiJob
	{
		[JobInfo("Manipulate Image Category")]
		public ManipulateImageCategory([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => site.UserFunctions.InitializeResult(ResultDestination.ResultsPage, "User:Jeancey/Kah", null);

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
				if (result is FilePage image)
				{
					var fileInfo = image.LatestFileRevision;
					if (fileInfo.Height < 64 && fileInfo.Width < 64)
					{
						smallImages.Add(image);
					}
				}
			}

			Debug.WriteLine(smallImages.Count);
			smallImages.Sort(TitleComparer<Page>.Instance);

			foreach (var image in smallImages)
			{
				this.WriteLine($"* {SiteLink.LinkTextFromTitle(image)} ({image.LatestFileRevision.Width}x{image.LatestFileRevision.Height})");
			}
		}
	}
}
