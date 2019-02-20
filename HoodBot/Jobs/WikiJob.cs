namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using static RobinHood70.WikiCommon.Extensions;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class WikiJob : WikiTask
	{
		#region Fields
		private int progress = 0;
		#endregion

		#region Constructors
		protected WikiJob(Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site)
		{
			ThrowNull(asyncInfo, nameof(asyncInfo));
			this.AsyncInfo = asyncInfo;
			this.Tasks.AddRange(tasks);
		}
		#endregion

		#region Public Properties
		public AsyncInfo AsyncInfo { get; }

		public int Progress
		{
			get => this.progress;
			protected set
			{
				this.progress = value;
				this.UpdateProgress();
			}
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void FlowControlAsync()
		{
			var pause = this.AsyncInfo.PauseToken;
			if (pause.IsPaused)
			{
				pause.WaitWhilePausedAsync().Wait();
			}

			var cancel = this.AsyncInfo.CancellationToken;
			if (cancel != CancellationToken.None && cancel.IsCancellationRequested)
			{
				cancel.ThrowIfCancellationRequested();
			}
		}

		protected virtual void StatusWrite(string status)
		{
			this.AsyncInfo.StatusMonitor.Report(status);
			this.FlowControlAsync();
		}

		protected virtual void StatusWriteLine(string status) => this.StatusWrite(status + Environment.NewLine);

		protected virtual void UpdateProgress()
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			this.FlowControlAsync();
		}

		// Same as UpdateProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual void UpdateProgressWrite(string status)
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			this.AsyncInfo.StatusMonitor.Report(status);
			this.FlowControlAsync();
		}

		protected virtual void UpdateProgressWriteLine(string status) => this.UpdateProgressWrite(status + Environment.NewLine);
		#endregion
	}
}