namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	[JobInfo("You Had One Job!", "|UESP | Generic | Generic")]
	public class OneJob : WikiJob
	{
		public OneJob(Site site)
			: base(site)
		{
			var random = new Random();
			var numTasks = random.Next(1, 5);
			var tasks = new List<WikiTask>();
			for (var i = 0; i < numTasks; i++)
			{
				tasks.Add(new OneTask(this));
			}

			this.Tasks = tasks;
		}

		public override void Execute()
		{
			this.OnStarted(EventArgs.Empty);
			foreach (var task in this.Tasks)
			{
				task.Started += this.Task_Started;
				task.ProgressChanged += this.Task_ProgressChanged;
				task.Completed += this.Task_Completed;
				task.Execute();
				task.Completed -= this.Task_Completed;
				task.ProgressChanged -= this.Task_ProgressChanged;
				task.Started -= this.Task_Started;
			}

			this.OnCompleted(EventArgs.Empty);
		}

		private void Task_Completed(WikiTask sender, EventArgs eventArgs) => this.OnTaskCompleted(new TaskEventArgs(this, sender));

		private void Task_ProgressChanged(WikiTask sender, ProgressEventArgs eventArgs) => this.OnProgressChanged(new ProgressEventArgs(eventArgs.Progress, eventArgs.ProgressMaximum, sender));

		private void Task_Started(WikiTask sender, EventArgs eventArgs) => this.OnTaskStarted(new TaskEventArgs(this, sender));
	}
}
