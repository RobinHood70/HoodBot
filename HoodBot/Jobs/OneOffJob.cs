namespace RobinHood70.HoodBot.Jobs;

using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Public Override Properties
	public override string LogName => "One-Off Job";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Remove deprecated section protection";

	protected override void LoadPages() => this.Pages.GetCategoryMembers("Category:Section Protection");

	protected override void PageLoaded(Page page)
	{
		page.Text = Regex.Replace(page.Text, @"(\[\[Category:Section Protection\]\]|{{Protection\|section}}|<protect>|<!-- preemptively added protection so the message isn't accidentally deleted -->)\s*", string.Empty, RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		page.Text = Regex.Replace(page.Text, @"\s*</protect>", string.Empty, RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout).Trim();
	}
	#endregion

}