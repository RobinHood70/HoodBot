namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;

	public class TaskEventArgs : EventArgs
	{
		public TaskEventArgs(WikiJob job, WikiTask task)
		{
			this.Job = job;
			this.Task = task;
		}

		public WikiJob Job { get; }

		public WikiTask Task { get; }
	}
}