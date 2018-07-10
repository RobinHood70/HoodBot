namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	public class OneJob : TaskJob
	{
		[JobInfo("You Had One Job!", "|UESP")]
		public OneJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			var numTasks = new Random().Next(2, 5);
			for (var i = 0; i < numTasks; i++)
			{
				this.Tasks.Add(new OneTask(this));
			}
		}

		[JobInfo("You Had Another Job!", "|Maintenance")]
		public OneJob(Site site, AsyncInfo asyncInfo, [JobParameter(null)] IEnumerable<string> inUSAToday)
			: base(site, asyncInfo)
		{
			Debug.WriteLine(string.Join(", ", inUSAToday));
		}
	}
}
