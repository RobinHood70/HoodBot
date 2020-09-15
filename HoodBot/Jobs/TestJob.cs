namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;

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