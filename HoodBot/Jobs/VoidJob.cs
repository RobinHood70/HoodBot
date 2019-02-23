namespace RobinHood70.HoodBot.Jobs
{
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class VoidJob : WikiJob
	{
		[JobInfo("Do Nothing")]
		public VoidJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.ProgressMaximum = 5;

		// public override string LogName => "Empty Job";

		protected override void Main()
		{
			for (var i = 1; i <= this.ProgressMaximum; i++)
			{
				this.StatusWrite($"Job {i}: Start...");
				Thread.Sleep(199);
				this.StatusWriteLine("End");
				this.Progress++;
			}
		}
	}
}
