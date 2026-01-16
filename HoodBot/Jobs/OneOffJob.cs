namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Replace links per Discord request";

	protected override void LoadPages() => this.Pages.GetBacklinks("Daggerfall:Daggerfall Preview", BacklinksTypes.Backlinks, false);

	protected override void PageLoaded(Page page) => page.Text = page.Text.Replace(
		"[[Daggerfall:Daggerfall Preview|Daggerfall Preview]] - [[Daggerfall:Daggerfall Preview/REDGUARD.TXT|REDGUARD.TXT]]}}",
		"[[Daggerfall:Daggerfall Preview/REDGUARD.TXT|REDGUARD.TXT]] - [[Daggerfall:Daggerfall Preview|Daggerfall Preview]]}}",
		StringComparison.OrdinalIgnoreCase);
	#endregion
}