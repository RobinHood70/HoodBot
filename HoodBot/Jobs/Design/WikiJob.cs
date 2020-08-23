namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Threading;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	public abstract class WikiJob : IMessageSource, ISiteSpecific
	{
		#region Fields
		private int progress;
		private int progressMaximum = 1;
		#endregion

		#region Constructors
		protected WikiJob([NotNull, ValidatedNotNull]Site site, [NotNull, ValidatedNotNull]AsyncInfo asyncInfo)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(asyncInfo, nameof(asyncInfo));
			this.Site = site;
			this.AsyncInfo = asyncInfo;
			this.LogName = this.GetType().Name.UnCamelCase();
			this.Logger = (site as IJobLogger)?.JobLogger;
			this.Results = (site as IResultHandler)?.ResultHandler;
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<WikiJob, EventArgs>? Completed;

		public event StrongEventHandler<WikiJob, EventArgs>? Started;
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

		public int ProgressMaximum
		{
			get => this.progressMaximum;
			protected set
			{
				this.progressMaximum = value <= 0 ? 1 : value;
				this.UpdateProgress();
			}
		}

		public double ProgressPercent => (double)this.Progress / this.ProgressMaximum;

		public Site Site { get; }
		#endregion

		#region Public Abstract Properties
		public virtual string LogName { get; }
		#endregion

		#region Public Virtual Properties
		public JobTypes JobType { get; protected set; } = JobTypes.Read;

		public virtual string? LogDetails { get; protected set; }

		public JobLogger? Logger { get; protected set; }
		#endregion

		#region Protected Properties
		protected ResultHandler? Results { get; set; }
		#endregion

		#region Public Methods
		public void Execute()
		{
			this.BeforeMain();
			this.Main();
			this.JobCompleted();
		}

		public void ResetProgress(int progressMax)
		{
			this.Progress = 0;
			this.ProgressMaximum = progressMax;
		}

		public void StatusWrite(string status)
		{
			this.AsyncInfo.StatusMonitor?.Report(status);
			this.FlowControlAsync();
		}

		public void StatusWriteLine(string status) => this.StatusWrite(status + Environment.NewLine);

		public void Warn(string warning) => this.Site.PublishWarning(this, warning);

		public void Write(string text) => this.Results?.Write(text);

		public void WriteLine() => this.WriteLine(string.Empty);

		public void WriteLine(string text) => this.Write(text + '\n');
		#endregion

		#region Protected Methods
		protected void SetResultDescription(string title)
		{
			if (this.Results != null)
			{
				this.Results.Description = this.Results.Description == null ? title : this.Results.Description + "; " + title;
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void Main();
		#endregion

		#region Protected Virtual Methods
		protected virtual void BeforeLogging()
		{
		}

		protected virtual void BeforeMain()
		{
			this.Started?.Invoke(this, EventArgs.Empty);
			this.BeforeLogging();
			if (this.Logger != null && this.Logger.ShouldLog(this.JobType) == true)
			{
				this.StatusWriteLine("Adding Log Entry");
				var logInfo = new LogInfo(this.LogName ?? "Unknown Job Type", this.LogDetails, this.JobType);
				this.Logger.AddLogEntry(logInfo);
			}
		}

		protected virtual void FlowControlAsync()
		{
			if (this.AsyncInfo.PauseToken is PauseToken pause && pause.IsPaused)
			{
				pause.WaitWhilePausedAsync().Wait();
			}

			if (this.AsyncInfo.CancellationToken is CancellationToken cancel && cancel != CancellationToken.None && cancel.IsCancellationRequested)
			{
				cancel.ThrowIfCancellationRequested();
			}
		}

		protected virtual void JobCompleted()
		{
			this.StatusWriteLine("Ending Log Entry");
			if (this.Logger != null && this.Logger.ShouldLog(this.JobType))
			{
				this.Logger.EndLogEntry();
			}

			this.Completed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void UpdateProgress()
		{
			this.AsyncInfo.ProgressMonitor?.Report(this.ProgressPercent);
			this.FlowControlAsync();
		}

		// Same as UpdateProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual void UpdateProgressWrite(string status)
		{
			this.AsyncInfo.ProgressMonitor?.Report(this.ProgressPercent);
			this.AsyncInfo.StatusMonitor?.Report(status);
			this.FlowControlAsync();
		}

		protected virtual void UpdateProgressWriteLine(string status) => this.UpdateProgressWrite(status + Environment.NewLine);
		#endregion
	}
}