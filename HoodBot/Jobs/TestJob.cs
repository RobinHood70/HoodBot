namespace RobinHood70.HoodBot.Jobs
{
	using System.Threading;

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
			const int maxTimes = 60;
			this.ProgressMaximum = maxTimes;
			for (var i = 1; i <= maxTimes; i++)
			{
				this.StatusWriteLine($"Sleep: ( {i} / {maxTimes} )");
				Thread.Sleep(1000);
				this.Progress++;
			}
		}
		#endregion
	}
}