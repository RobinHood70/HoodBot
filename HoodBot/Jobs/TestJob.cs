namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Threading;
using RobinHood70.Robby;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Clients;

[method: JobInfo("Test Job")]
internal sealed class TestJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var result = this.Site.Upload(@"D:\Data\HoodBot\esoui\art\icons\ability_1handed_001.png", "Test Image.png", "Test upload", "This is a test upload.");
		var warnings = string.Join(" / ", result.Value.Warnings);
		var duplicates = string.Join(" / ", result.Value.Duplicates);
		this.StatusWriteLine($"Upload aborted.\r\nWarnings: {warnings}\r\nDuplicate versions: {duplicates}");

		const int maxTimes = 5;
		this.ResetProgress(maxTimes);
		for (var i = 1; i <= maxTimes; i++)
		{
			this.Wait(TimeSpan.FromSeconds(2));
			this.StatusWriteLine($"Sleep: ( {i} / {maxTimes} )");
			this.Progress++;
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