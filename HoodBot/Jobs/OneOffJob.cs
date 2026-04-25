namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Add category";

	protected override void LoadPages()
	{
		this.Pages.GetRedirectsToNamespace(UespNamespaces.Lore);
	}

	protected override void PageLoaded(Page page)
	{
		if (
			this.Site.GetRedirectFromText(page.Text)?.Title is Title redirectTarget &&
			redirectTarget.Namespace == UespNamespaces.Lore &&
			redirectTarget.PageName.StartsWith("Fish", StringComparison.OrdinalIgnoreCase))
		{
			var parser = new SiteParser(page);
			parser.AddCategoryAfter(this.Site, "Lore-Creatures-Fish", "Lore-Creatures", false);
			parser.UpdatePage();
		}
	}
	#endregion
}