namespace RobinHood70.HoodBot.Jobs
{
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class VoidJob : WikiJob
	{
		[JobInfo("Do Nothing")]
		public VoidJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override void Main()
		{
			var progressMax = 5;
			this.ProgressMaximum = progressMax;
			for (var i = 1; i <= progressMax; i++)
			{
				this.StatusWrite($"Job {i}: Start...");
				Thread.Sleep(199);
				this.StatusWriteLine("End");
				this.IncrementProgress();
			}
		}
	}
}
