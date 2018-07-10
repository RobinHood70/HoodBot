namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using System.Threading.Tasks;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public abstract class WikiRunner
	{
		#region Constructors
		protected WikiRunner(Site site) => this.Site = site;
		#endregion

		#region Public Events
		public event StrongEventHandler<WikiRunner, EventArgs> Completed;

		public event StrongEventHandler<WikiRunner, EventArgs> Started;
		#endregion

		#region Public Properties
		public AsyncInfo AsyncInfo { get; protected set; }

		public int Progress { get; set; }

		public int ProgressMaximum { get; set; }

		public Site Site { get; }
		#endregion

		#region Public Abstract Methods
		public abstract Task Execute();
		#endregion

		#region Protected Virtual Methods
		protected virtual void OnCompleted(EventArgs e) => this.Completed?.Invoke(this, e);

		protected virtual void OnStarted(EventArgs e) => this.Started?.Invoke(this, e);

		protected virtual async Task PauseOrCancel()
		{
			if (this.AsyncInfo.Pause.IsPaused)
			{
				await this.AsyncInfo.Pause.WaitWhilePausedAsync();
			}

			if (this.AsyncInfo.Cancel.IsCancellationRequested)
			{
				this.AsyncInfo.Cancel.ThrowIfCancellationRequested();
			}
		}

		protected virtual Task UpdateProgress()
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			return this.PauseOrCancel();
		}

		// Same as UpdateJobProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual Task UpdateProgressWrite(string status)
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			this.AsyncInfo.StatusMonitor.Report(status);
			return this.PauseOrCancel();
		}

		// Same as UpdateJobProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual Task UpdateProgressWriteLine(string status)
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			this.AsyncInfo.StatusMonitor.Report(status + Environment.NewLine);
			return this.PauseOrCancel();
		}

		protected virtual Task UpdateStatusWrite(string status)
		{
			this.AsyncInfo.StatusMonitor.Report(status);
			return this.PauseOrCancel();
		}

		protected virtual Task UpdateStatusWriteLine(string status)
		{
			this.AsyncInfo.StatusMonitor.Report(status + Environment.NewLine);
			return this.PauseOrCancel();
		}
		#endregion
	}
}
