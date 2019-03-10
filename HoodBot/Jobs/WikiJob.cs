namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Extensions;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class WikiJob : WikiTask, IMessageSource
	{
		#region Fields
		private int progress = 0;
		#endregion

		#region Constructors
		protected WikiJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
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

		#region Public Virtual Methods
		public virtual bool ReadOnly => true;
		#endregion

		#region Public Methods
		public void StatusWrite(string status)
		{
			this.AsyncInfo.StatusMonitor.Report(status);
			this.FlowControlAsync();
		}

		public void StatusWriteLine(string status) => this.StatusWrite(status + Environment.NewLine);

		public void Warn(string warning) => this.Site.PublishWarning(this, warning);

		public void Write(string text) => this.Site.UserFunctions.AddResult(text);

		public void Write(ResultDestination destination, string text) => this.Site.UserFunctions.AddResult(destination, text);

		public void WriteLine() => this.WriteLine(string.Empty);

		public void WriteLine(string text) => this.Site.UserFunctions.AddResult(text + '\n');

		public void WriteLine(ResultDestination destination) => this.WriteLine(destination, string.Empty);

		public void WriteLine(ResultDestination destination, string text) => this.Site.UserFunctions.AddResult(destination, text + '\n');
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