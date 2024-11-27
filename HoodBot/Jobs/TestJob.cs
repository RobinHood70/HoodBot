namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Threading;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Clients;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;

[method: JobInfo("Test Job")]
internal sealed class TestJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var parsed = new WikiNodeFactory()
			.Parse("=Test=\n</onlyinclude>==Hello==\nWorld<onlyinclude>Hello</onlyinclude> Screw You! <onlyinclude> World</onlyinclude>", InclusionType.Transcluded, true);
		this.StatusWriteLine(parsed.ToRaw());

		const int maxTimes = 0;
		this.ProgressMaximum = maxTimes;
		for (var i = 1; i <= maxTimes; i++)
		{
			this.Wait(TimeSpan.FromSeconds(2));
			this.Progress++;
			this.StatusWriteLine($"Sleep: ( {i} / {maxTimes} )");
		}
	}
	#endregion

	#region Private Methods
	private void Wait(TimeSpan delay)
	{
		var site = this.Site;
		if (site.AbstractionLayer is IInternetEntryPoint net && net.Client is IMediaWikiClient client)
		{
			client.RequestDelay(delay, DelayReason.ClientThrottled, "Just cuz.");
		}
		else
		{
			Thread.Sleep(delay);
		}
	}
	#endregion
}