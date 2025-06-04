namespace RobinHood70.HoodBot.Jobs;

using System.Threading;
using RobinHood70.CommonCode;

internal sealed class VoidJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		this.ResetProgress(5);
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