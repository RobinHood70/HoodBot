namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;

	[method: JobInfo("Test Job")]
	internal sealed class TestJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			const int maxTimes = 0;
			this.ProgressMaximum = maxTimes;
			for (var i = 1; i <= maxTimes; i++)
			{
				this.Wait(TimeSpan.FromSeconds(2));
				this.Progress++;
				this.StatusWriteLine($"Sleep: ( {i} / {maxTimes} )");
			}

			var context = new Context(this.Site)
			{
				Title = TitleFactory.FromUnvalidated(this.Site, "Main Page")
			};

			this.StatusWriteLine(ParseToText.Build("{{{1|Hello World!}}} The current {{Sic|{{{test|page}}}|pg}} is {{FULLPAGENAME}}. This [[SomePage|link]] should be stripped.", context));

			foreach (var word in context.UnhandledMagicWords)
			{
				Debug.WriteLine("Unhandled: " + word);
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