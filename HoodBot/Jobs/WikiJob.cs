namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Threading;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Loggers;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	#region Public Enumerations
	[Flags]
	public enum JobTypes
	{
		/*
		Currently only supporting simple Read/Write flags, but could be expanded to distinguish between different types of jobs, for example:
			* PageEdit (anything that edits pages as opposed to moving them or whatever)
			* Report (for single-page reports)
			* User (anything that works only on users or in user space)
			... etc.
		*/
		None = 0,
		Read = 1,
		Write = 1 << 1,
	}
	#endregion

	public abstract class WikiJob : IMessageSource, ISiteSpecific
	{
		#region Fields
		private readonly string logName;
		private int progress;
		private int progressMaximum = 1;
		#endregion

		#region Constructors
		protected WikiJob(JobManager jobManager)
		{
			this.JobManager = jobManager.NotNull(nameof(jobManager));
			this.Site = jobManager.Site; // We make a copy of this due to the high access rate in most jobs.
			this.logName = this.GetType().Name.UnCamelCase();
			this.Logger = jobManager.Logger; // We make a copy of this so that it can be overridden on a job-specific basis, if needed.
			this.Results = jobManager.ResultHandler; // We make a copy of this so that it can be overridden on a job-specific basis, if needed.
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<WikiJob, EventArgs>? Completed;

		public event StrongEventHandler<WikiJob, EventArgs>? Started;
		#endregion

		#region Public Properties
		public JobManager JobManager { get; }

		public JobTypes JobType { get; protected set; } = JobTypes.Read;

		public JobLogger? Logger { get; protected set; }

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

		#region Public Virtual Properties
		public virtual string? LogDetails { get; protected set; }

		public virtual string LogName => this.logName;
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
			this.JobManager.StatusMonitor?.Report(status);
			this.FlowControl();
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
			if (this.Logger?.ShouldLog(this.JobType) == true)
			{
				this.StatusWriteLine("Adding Log Entry");
				var logInfo = new LogInfo(this.LogName ?? "Unknown Job Type", this.LogDetails);
				this.Logger.AddLogEntry(logInfo);
			}
		}

		protected virtual void FlowControl()
		{
			if (this.JobManager.PauseToken is PauseToken pause && pause.IsPaused)
			{
				pause.WaitWhilePausedAsync().Wait();
			}

			if (this.JobManager.CancellationToken is CancellationToken cancel && cancel != CancellationToken.None && cancel.IsCancellationRequested)
			{
				cancel.ThrowIfCancellationRequested();
			}
		}

		protected virtual void JobCompleted()
		{
			if (this.Logger?.ShouldLog(this.JobType) == true)
			{
				this.StatusWriteLine("Ending Log Entry");
				this.Logger.EndLogEntry();
			}

			this.Completed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void UpdateProgress()
		{
			this.JobManager.ProgressMonitor?.Report(this.ProgressPercent);
			this.FlowControl();
		}

		// Same as UpdateProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual void UpdateProgressWrite(string status)
		{
			this.JobManager.ProgressMonitor?.Report(this.ProgressPercent);
			this.JobManager.StatusMonitor?.Report(status);
			this.FlowControl();
		}

		protected virtual void UpdateProgressWriteLine(string status) => this.UpdateProgressWrite(status + Environment.NewLine);
		#endregion
	}
}