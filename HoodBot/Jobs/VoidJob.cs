namespace RobinHood70.HoodBot.Jobs
{
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class VoidJob : EditJob
	{
		#region Constructors
		[JobInfo("Do Nothing")]
		public VoidJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.ProgressMaximum = 5;
			site.EditingDisabled = true;
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Do Nothing";
		#endregion

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
