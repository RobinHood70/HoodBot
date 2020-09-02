namespace RobinHood70.HoodBot.Design
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	public class JobManager
	{
		#region Fields
		private readonly IEnumerable<JobInfo> jobList;
		private readonly Site site;
		private readonly AsyncInfo asyncInfo;
		#endregion

		#region Constructors
		public JobManager(IEnumerable<JobInfo> jobList, Site site, AsyncInfo asyncInfo)
		{
			this.jobList = jobList ?? throw ArgumentNull(nameof(jobList));
			this.site = site ?? throw ArgumentNull(nameof(site));
			this.asyncInfo = asyncInfo ?? throw ArgumentNull(nameof(asyncInfo));
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<JobManager, bool>? FinishedAllJobs;

		public event StrongEventHandler<JobManager, JobEventArgs>? FinishedJob;

		public event StrongEventHandler<JobManager, EventArgs>? StartingAllJobs;

		public event StrongEventHandler<JobManager, JobEventArgs>? StartingJob;
		#endregion

		#region Public Methods
		public async Task Run()
		{
			this.OnStartingAllJobs();
			var allSuccessful = true;
			var editingEnabledMaster = this.site.EditingEnabled;
			foreach (var jobInfo in this.jobList)
			{
				var abort = this.OnStartingJob(jobInfo);
				if (abort)
				{
					// Unclear why subscriber would abort a job that hasn't even been attempted yet, but since we're using the same semantics for both starting and finishing a job, if subscriber asks for it, do it.
					break;
				}

				var job = jobInfo.Instantiate(this.site, this.asyncInfo);
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
					this.site.EditingEnabled = editingEnabledMaster;
				}
			}

			this.OnFinishedAllJobs(allSuccessful);
		}
		#endregion

		#region Protected Methods
		protected void OnFinishedAllJobs(bool allSuccessful) => this.FinishedAllJobs?.Invoke(this, allSuccessful);

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