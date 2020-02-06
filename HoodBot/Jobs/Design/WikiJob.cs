﻿namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using System.Threading;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Extensions;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class WikiJob : IMessageSource, ISiteSpecific
	{
		#region Fields
		private int progress = 0;
		#endregion

		#region Constructors
		protected WikiJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(asyncInfo, nameof(asyncInfo));
			this.Site = site;
			this.AsyncInfo = asyncInfo;
			this.LogName = this.GetType().Name.UnCamelCase();
			this.Logger = (site as IJobLogger)?.JobLogger;
			this.Results = (site as IResultPageHandler)?.ResultPageHandler;
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

		public int ProgressMaximum { get; protected set; } = 1;

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
			this.AsyncInfo.ProgressMonitor?.Report((double)this.Progress / this.ProgressMaximum);
			this.FlowControlAsync();
		}

		// Same as UpdateProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual void UpdateProgressWrite(string status)
		{
			this.AsyncInfo.ProgressMonitor?.Report((double)this.Progress / this.ProgressMaximum);
			this.AsyncInfo.StatusMonitor?.Report(status);
			this.FlowControlAsync();
		}

		protected virtual void UpdateProgressWriteLine(string status) => this.UpdateProgressWrite(status + Environment.NewLine);
		#endregion
	}
}