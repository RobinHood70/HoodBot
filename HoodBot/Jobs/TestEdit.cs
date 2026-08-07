namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;

[method: JobInfo("Test Edit")]
internal sealed class TestEdit(JobManager jobManager) : EditJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Test";

	protected override void LoadPages()
	{
		if (this.Site.User is User user)
		{
			this.RecreateIfDeleted = false;
			this.Pages.SetLimitations(Robby.Design.LimitationType.None);
			this.Pages.GetTitles(user.Title + "/TestPage");
		}
	}

	protected override void PageLoaded(Page page) => page.Text = $"This is a test page. Last updated at {DateTime.UtcNow:O}";
	#endregion
}