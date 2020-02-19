namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.EditSummaryMove = "Same move as live server";
			this.MoveOptions = MoveOptions.MoveSubPages | MoveOptions.MoveTalkPage;
			DeleteFiles();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "SR-wallpaper-Dragonborn-");

			foreach (var title in titles)
			{
				this.Replacements.Add(new Replacement(this.Site, title.FullPageName, title.FullPageName.Replace("Dragonborn", "Dovahkiin", System.StringComparison.Ordinal)));
			}
		}
		#endregion
	}
}