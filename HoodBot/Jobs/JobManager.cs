﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Loggers;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	public class JobManager
	{
		#region Constructors
		public JobManager(Site site) => this.Site = site ?? throw ArgumentNull(nameof(site));
		#endregion

		#region Public Events
		public event StrongEventHandler<JobManager, bool>? FinishedAllJobs;

		public event StrongEventHandler<JobManager, JobEventArgs>? FinishedJob;

		public event StrongEventHandler<JobManager, EventArgs>? StartingAllJobs;

		public event StrongEventHandler<JobManager, JobEventArgs>? StartingJob;
		#endregion

		#region Public Properties

		public CancellationToken? CancellationToken { get; set; }

		public JobLogger? Logger { get; set; }

		public PauseToken? PauseToken { get; set; }

		public IProgress<double>? ProgressMonitor { get; set; }

		public ResultHandler? ResultHandler { get; set; }

		public Site Site { get; }

		public IProgress<string>? StatusMonitor { get; set; }
		#endregion

		#region Public Methods
		public async Task Run(IEnumerable<JobInfo> jobList)
		{
			ThrowNull(jobList, nameof(jobList));
			this.OnStartingAllJobs();
			var allSuccessful = true;
			var editingEnabledMaster = this.Site.EditingEnabled;
			foreach (var jobInfo in jobList)
			{
				var abort = this.OnStartingJob(jobInfo);
				if (abort)
				{
					// Unclear why subscriber would abort a job that hasn't even been attempted yet, but since we're using the same semantics for both starting and finishing a job, if subscriber asks for it, do it.
					break;
				}

				var job = jobInfo.Instantiate(this);
				try
				{
					await Task.Run(job.Execute).ConfigureAwait(false);
					abort = this.OnFinishedJob(jobInfo, null);
					if (abort)
					{
						break;
					}
				}
				catch (Exception e)
				{
					allSuccessful = false;
					abort = this.OnFinishedJob(jobInfo, e);
					if (abort)
					{
						throw;
					}
				}
				finally
				{
					// Reset value in case job cheated and changed it.
					this.Site.EditingEnabled = editingEnabledMaster;
				}
			}

			this.OnFinishedAllJobs(allSuccessful);
		}
		#endregion

		#region Protected Methods
		protected void OnFinishedAllJobs(bool allSuccessful)
		{
			if (this.ResultHandler != null)
			{
				this.ResultHandler.Save();
				this.ResultHandler.Clear();
			}

			this.FinishedAllJobs?.Invoke(this, allSuccessful);
		}

		protected virtual bool OnFinishedJob(JobInfo job, Exception? e)
		{
			var eventArgs = new JobEventArgs(job, e);
			this.FinishedJob?.Invoke(this, eventArgs);
			return eventArgs.Abort;
		}

		protected virtual bool OnStartingJob(JobInfo job)
		{
			var eventArgs = new JobEventArgs(job, null);
			this.StartingJob?.Invoke(this, eventArgs);
			return eventArgs.Abort;
		}

		protected virtual void OnStartingAllJobs() => this.StartingAllJobs?.Invoke(this, EventArgs.Empty);
		#endregion
	}
}