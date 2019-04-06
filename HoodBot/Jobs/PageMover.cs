namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo, TaskActions.FixLinks | TaskActions.CheckLinksRemaining | TaskActions.SaveResults)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override ICollection<Replacement> GetReplacements()
		{
			var retval = new List<Replacement>();
			var titles = new TitleCollection(this.Site);
			titles.GetCategoryMembers("Category:Online-Facial Accessory Images", false);
			titles.GetCategoryMembers("Category:Online-Icons-Facial Accessories", false);
			foreach (var title in titles)
			{
				retval.Add(new Replacement(this.Site, title.FullPageName.Replace("-major adornment-", "-facial accessory-"), title.FullPageName.Replace("-facial accessory-", "-major adornment-")));
			}

			titles.Clear();
			titles.GetCategoryMembers("Category:Online-Jewelry Images", false);
			titles.GetCategoryMembers("Category:Online-Icons-Jewelry", false);
			foreach (var title in titles)
			{
				retval.Add(new Replacement(this.Site, title.FullPageName.Replace("-minor adornment-", "-jewelry-"), title.FullPageName.Replace("-jewelry-", "-minor adornment-")));
			}

			retval.Sort();

			retval.Add(new Replacement(this.Site, "Category:Online-Facial Accessory Images", "Category:Online-Major Adornment Images"));
			retval.Add(new Replacement(this.Site, "Category:Online-Icons-Facial Accessories", "Category:Online-Icons-Major Adornments"));
			retval.Add(new Replacement(this.Site, "Category:Online-Jewelry Images", "Category:Online-Minor Adornment Images"));
			retval.Add(new Replacement(this.Site, "Category:Online-Icons-Jewelry", "Category:Online-Icons-Minor Adornments"));

			return retval;
		}
		#endregion
	}
}