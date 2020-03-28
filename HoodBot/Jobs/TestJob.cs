namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class TestJob : WikiJob
	{
		#region Constructors
		[JobInfo("Test Job")]
		public TestJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
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
