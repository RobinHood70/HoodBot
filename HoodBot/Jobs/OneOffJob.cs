namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using System.IO;
	using RobinHood70.WallE.Base;

	[method: JobInfo("One-Off Job")]
	internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			var lines = File.ReadAllLines(@"D:\CreationCheck.txt");
			foreach (var fullPageName in lines)
			{
				var input = new LogEventsInput(fullPageName);
				var logs = this.Site.AbstractionLayer.LogEvents(input);
				foreach (var logEvent in logs)
				{
					if (!string.Equals(logEvent.LogAction, "autopatrol", System.StringComparison.OrdinalIgnoreCase))
					{
						Debug.WriteLine($"{logEvent.LogAction}: {logEvent.Timestamp} - {fullPageName}");
					}
				}
			}
		}
		#endregion
	}
}