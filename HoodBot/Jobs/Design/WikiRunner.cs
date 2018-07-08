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

		public Site Site { get; }

		public int Progress { get; set; }

		public int ProgressMaximum { get; set; }
		#endregion

		#region Public Abstract Methods
		public abstract Task Execute();
		#endregion

		#region Protected Virtual Methods
		protected virtual async Task UpdateJobProgress()
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			if (this.AsyncInfo.Pause.IsPaused)
			{
				await this.AsyncInfo.Pause.WaitWhilePausedAsync();
			}

			if (this.AsyncInfo.Cancel.IsCancellationRequested)
			{
				this.AsyncInfo.Cancel.ThrowIfCancellationRequested();
			}
		}

		protected virtual void OnCompleted(EventArgs e) => this.Completed?.Invoke(this, e);

		protected virtual void OnStarted(EventArgs e) => this.Started?.Invoke(this, e);
		#endregion
	}
}
