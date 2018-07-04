namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class WikiJob : IWikiTask
	{
		protected WikiJob(Site site)
			: this(site, new WikiTask[0])
		{
		}

		protected WikiJob(Site site, params WikiTask[] tasks)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
			this.Tasks = new List<WikiTask>(tasks);
		}

		// TODO: All event arguments temporarily set to EventArgs until jobs and tasks are more fully developed.
		public event StrongEventHandler<WikiJob, EventArgs> Completed;

		public event StrongEventHandler<WikiJob, ProgressEventArgs> ProgressChanged;

		public event StrongEventHandler<WikiJob, EventArgs> Started;

		public event StrongEventHandler<WikiJob, TaskEventArgs> TaskCompleted;

		public event StrongEventHandler<WikiJob, TaskEventArgs> TaskStarted;

		public Site Site { get; }

		public IReadOnlyList<WikiTask> Tasks { get; protected set; }

		public abstract void Execute();

		public void FetchParameterData() => throw new NotImplementedException();

		protected virtual void OnCompleted(EventArgs e) => this.Completed?.Invoke(this, e);

		protected virtual void OnProgressChanged(ProgressEventArgs e) => this.ProgressChanged?.Invoke(this, e);

		protected virtual void OnStarted(EventArgs e) => this.Started?.Invoke(this, e);

		protected virtual void OnTaskCompleted(TaskEventArgs e) => this.TaskCompleted?.Invoke(this, e);

		protected virtual void OnTaskStarted(TaskEventArgs e) => this.TaskStarted?.Invoke(this, e);
	}
}