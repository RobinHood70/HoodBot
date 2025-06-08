namespace RobinHood70.HoodBot.Jobs;
using RobinHood70.Robby;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	protected override string GetEditSummary(Page page) => "Replace incorrect Paradise link (bot assisted)";

	protected override void LoadPages() => this.Pages.GetBacklinks("Oblivion:Paradise (quest)", BacklinksTypes.All);

	protected override void PageLoaded(Page page) => page.Text = page.Text.Replace("Paradise (quest)", "Paradise (place)", System.StringComparison.OrdinalIgnoreCase);
}