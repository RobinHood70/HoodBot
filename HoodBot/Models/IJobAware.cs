namespace RobinHood70.HoodBot.Models
{
	public interface IJobAware
	{
		void OnJobsCompleted(bool success);

		void OnJobsStarted();
	}
}
