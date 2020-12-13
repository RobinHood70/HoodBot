namespace RobinHood70.HoodBot.Jobs
{
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
		}
		#endregion
	}
}