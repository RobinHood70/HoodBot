namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => throw new NotImplementedException();

	protected override void LoadPages() => throw new NotImplementedException();

	protected override void ParseText(SiteParser parser) => throw new NotImplementedException();
	#endregion
}