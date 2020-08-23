namespace RobinHood70.HoodBot
{
	public interface IJobAware
	{
		void OnJobsCompleted(bool success);

		void OnJobsStarted();
	}
}
