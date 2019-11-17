namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;

	public class ProgressEventArgs : EventArgs
	{
		public ProgressEventArgs(int progress, int progressMax)
			: this(progress, progressMax, null)
		{
		}

		public ProgressEventArgs(int progress, int progressMax, WikiTask? task)
		{
			this.Progress = progress;
			this.ProgressMaximum = progressMax;
			this.Task = task;
		}

		public int Progress { get; }

		public int ProgressMaximum { get; }

		public WikiTask? Task { get; }
	}
}