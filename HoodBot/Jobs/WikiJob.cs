namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Threading;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Jobs.Loggers;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

#region Public Enumerations
public enum JobType
{
	ReadOnly,
	UnloggedWrite,
	Write
}
#endregion

public abstract class WikiJob : IMessageSource
{
	#region Fields
	private readonly string logName;
	private int progress;
	private int progressMaximum = 1;
	#endregion

	#region Constructors
	protected WikiJob(JobManager jobManager, JobType jobType)
	{
		ArgumentNullException.ThrowIfNull(jobManager);
		this.JobManager = jobManager;
		this.Site = jobManager.Site; // We make a copy of this due to the high access rate in most jobs.
		this.logName = this.GetType().Name.UnCamelCase();
		this.Logger = jobManager.Logger; // We make a copy of this so that it can be overridden on a job-specific basis, if needed.
		this.Results = jobManager.ResultHandler; // We make a copy of this so that it can be overridden on a job-specific basis, if needed.
		this.JobType = jobType;
	}
	#endregion

	#region Public Events
	public event StrongEventHandler<WikiJob, EventArgs>? Completed;

	public event StrongEventHandler<WikiJob, EventArgs>? Started;
	#endregion

	#region Public Properties
	public JobType JobType { get; }

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
		private set
		{
			this.progressMaximum = value <= 0 ? 1 : value;
			this.UpdateProgress();
		}
	}

	public double ProgressPercent => (double)this.progress / this.progressMaximum;

	public Site Site { get; }
	#endregion

	#region Public Virtual Properties
	public virtual string LogDetails => string.Empty;

	public virtual string LogName => this.logName;
	#endregion

	#region Protected Properties
	protected JobManager JobManager { get; }

	protected ResultHandler? Results { get; set; }
	#endregion

	#region Public Methods
	public void ClearStatus() => this.StatusWrite(null);

	public void Execute()
	{
		if (this.BeforeMain())
		{
			this.FlowControl();
			this.Main();
			this.FlowControl();
			this.JobCompleted();
		}
	}

	public void ResetProgress(int progressMax)
	{
		this.Progress = 0;
		this.ProgressMaximum = progressMax;
		this.JobManager.ResetProgress();
	}

	public void StatusWrite(string? status)
	{
		this.JobManager.UpdateStatus(status);
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

	/// <summary>Executes any code that should happen before a log entry is created.</summary>
	/// <returns><see langword="true"/> if the job should continue; otherwise, <see langword="false"/>.</returns>
	protected virtual bool BeforeLogging() => true;

	/// <summary>Executes any code that should happen before the main code body.</summary>
	/// <returns><see langword="true"/> if the job should continue; otherwise, <see langword="false"/>.</returns>
	/// <remarks>If this returns <see langword="false"/>, no logging or modifications should occur.</remarks>
	protected virtual bool BeforeMain()
	{
		this.Started?.Invoke(this, EventArgs.Empty);
		if (!this.BeforeLogging())
		{
			return false;
		}

		if (this.Logger is not null && this.JobType == JobType.Write)
		{
			LogInfo logInfo = new(this.LogName ?? "Unknown Job Type", this.LogDetails);
			this.Logger.AddLogEntry(logInfo);
		}

		return true;
	}

	protected virtual void FlowControl()
	{
		if (this.JobManager.PauseToken is PauseToken pause && pause.IsPaused)
		{
			pause.WaitWhilePausedAsync().Wait(this.JobManager.CancelToken);
		}

		if (this.JobManager.CancelToken is CancellationToken token &&
			token != CancellationToken.None)
		{
			token.ThrowIfCancellationRequested();
		}
	}

	protected virtual void JobCompleted()
	{
		if (this.Logger is not null && this.JobType == JobType.Write)
		{
			this.Logger.EndLogEntry();
		}

		this.Results?.Save();
		this.Completed?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void UpdateProgress()
	{
		this.JobManager.UpdateProgress(this.ProgressPercent);
		this.FlowControl();
	}
	#endregion
}