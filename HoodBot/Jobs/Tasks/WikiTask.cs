namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public abstract class WikiTask : IWikiTask
	{
		#region Fields
		private int progress;
		private int progressMax;
		#endregion

		protected WikiTask(IWikiTask parent)
		{
			this.Parent = parent;
			this.Job = (parent as WikiJob) ?? (parent as WikiTask).Job;
			this.Site = parent.Site;
		}

		public event StrongEventHandler<WikiTask, EventArgs> Completed;

		public event StrongEventHandler<WikiTask, ProgressEventArgs> ProgressChanged;

		public event StrongEventHandler<WikiTask, EventArgs> Started;

		public WikiJob Job { get; }

		public IWikiTask Parent { get; }

		public Site Site { get; }

		public int Progress
		{
			get => this.progress;
			protected set
			{
				this.progress = value;
				this.OnProgressChanged(new ProgressEventArgs(value, this.progressMax));
			}
		}

		public int ProgressMaximum
		{
			get => this.progressMax;
			protected set
			{
				this.progressMax = value;
				this.OnProgressChanged(new ProgressEventArgs(this.progress, value));
			}
		}

		Site IWikiTask.Site { get; }

		public abstract void Execute();

		protected virtual void OnFinished(EventArgs e) => this.Completed?.Invoke(this, e);

		protected virtual void OnProgressChanged(ProgressEventArgs e) => this.ProgressChanged?.Invoke(this, e);

		protected virtual void OnStarted(EventArgs e) => this.Started?.Invoke(this, e);
	}
}
