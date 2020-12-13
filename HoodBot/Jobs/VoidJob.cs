namespace RobinHood70.HoodBot.Jobs
{
	using System.Threading;
	using RobinHood70.CommonCode;

	public class VoidJob : WikiJob
	{
		#region Constructors
		// We want to keep this job around for testing, but don't need it to clutter up the main list, so comment out the JobInfo.
		// [JobInfo("Do Nothing")]
		public VoidJob(JobManager jobManager)
			: base(jobManager) => this.ProgressMaximum = 5;
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			for (var i = 1; i <= this.ProgressMaximum; i++)
			{
				this.StatusWrite($"Job {i.ToStringInvariant()}: Start...");
				Thread.Sleep(199);
				this.StatusWriteLine("End");
				this.Progress++;
			}
		}
		#endregion
	}
}
