namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;

[method: JobInfo("Collections Link Finder", "ESO")]
public class LinkFinderEsoCollections(JobManager jobManager) : LinkFinderJob(jobManager, true)
{
	#region Protected Override Methods
	protected override void LoadPages()
	{
		TitleCollection titles = new(this.Site, UespNamespaces.Online, "Assistants", "Body Markings", "Collectible Emotes", "Collectible Furnishings", "Costumes", "Facial Hair", "Fragments", "Hair Styles", "Hats(collectible)", "Head Markings", "Houseguests", "Major Adornments", "Mementos (collection)", "Minor Adornments", "Mounts", "Personalities", "Pets", "Polymorphs", "Skins");
		this.SetTitlesFromSubpages(titles.Titles());
		base.LoadPages();
	}
	#endregion
}