namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			TitleCollection allTitles = new(this.Site);
			TitleCollection imageCats = new(this.Site);
			imageCats.GetNamespace(MediaWikiNamespaces.Category, Filter.Any, "Online-Furnishing Images-");
			foreach (var title in imageCats)
			{
				allTitles.Add(title);
				allTitles.Add(FurnishingTitle(title));
			}

			PageCollection pages = new(this.Site, PageModules.Default | PageModules.CategoryInfo);
			pages.GetTitles(allTitles);
			foreach (var page in pages)
			{
				var newTitle = FurnishingTitle(page);
				if (page is CategoryPage catPage && catPage.FullCount == 0)
				{
					if (catPage.Exists)
					{
						catPage.Text = "{{Proposeddeletion|bot=1|Unused category.}}";
						this.Pages.Add(catPage);
					}
				}
				else if (pages[newTitle] is CategoryPage catPageNew && catPageNew.FullCount == 0)
				{
					if (catPageNew.Exists)
					{
						catPageNew.Text = "{{Proposeddeletion|bot=1|Unused category.}}";
						this.Pages.Add(catPageNew);
					}
				}
				else if (page != newTitle && !pages[newTitle].Exists)
				{
					var newPage = this.Site.CreatePage(newTitle, page.Text
						.Replace("images of ", string.Empty, StringComparison.Ordinal)
						.Replace("Online-Furnishing Images", "Online-Furnishings", StringComparison.Ordinal));
					this.Pages.Add(newPage);
				}
			}

			static ISimpleTitle FurnishingTitle(ISimpleTitle title) => TitleFactory.Direct(title.Namespace, title.PageName.Replace("-Furnishing Images-", "-Furnishings-", StringComparison.Ordinal));
		}

		protected override void Main() => this.SavePages("Create furnishing redirects", false);
		#endregion
	}
}