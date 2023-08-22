namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;

	public class LinkFinderEsoCollections : LinkFinderJob
	{
		#region Constructors
		[JobInfo("Collections Link Finder", "ESO")]
		public LinkFinderEsoCollections(JobManager jobManager)
			: base(jobManager, true)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			TitleCollection titles = new(this.Site, UespNamespaces.Online, "Assistants", "Body Markings", "Collectible Emotes", "Collectible Furnishings", "Costumes", "Facial Hair", "Fragments", "Hair Styles", "Hats(collectible)", "Head Markings", "Houseguests", "Major Adornments", "Mementos (collection)", "Minor Adornments", "Mounts", "Personalities", "Pets", "Polymorphs", "Skins");
			this.SetTitlesFromSubpages(titles.Titles());
			base.LoadPages();
		}
		#endregion
	}
}