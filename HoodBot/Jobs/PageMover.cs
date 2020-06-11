namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.MoveOverExisting = false;
			DeleteFiles();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetCategoryMembers("Online-Crown Store Images");
			foreach (var title in titles)
			{
				if (title.PageName.Contains("-prerelease-", StringComparison.Ordinal))
				{
					var newTitle = new Title(title);
					newTitle.PageName = newTitle.PageName.Replace("-prerelease-", "-crown store-", StringComparison.Ordinal);
					this.Replacements.Add(new Replacement(title, newTitle));
				}
			}
		}
		#endregion
	}
}