namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Threading;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;

	public class TestJob : WikiJob
	{
		#region Constructors
		[JobInfo("Test Job")]
		public TestJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			const int maxTimes = 30;
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
}