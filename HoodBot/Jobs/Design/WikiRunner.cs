namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public abstract class WikiRunner
	{
		#region Fields
		private int progress = 0;
		#endregion

		#region Constructors
		protected WikiRunner(Site site) => this.Site = site;
		#endregion

		#region Public Events
		public event StrongEventHandler<WikiRunner, EventArgs> Completed;

		public event StrongEventHandler<WikiRunner, EventArgs> Started;
		#endregion

		#region Public Properties
		public AsyncInfo AsyncInfo { get; protected set; }

		public int Progress
		{
			get => this.progress;
			set
			{
				this.progress = value;
				this.UpdateProgress();
			}
		}

		public int ProgressMaximum { get; set; }

		public Site Site { get; }
		#endregion

		#region Public Methods
		public static TitleCollection BuildRedirectList(IEnumerable<Title> titles)
		{
			var retval = TitleCollection.CopyFrom(titles);

			// Loop until nothing new is added.
			var pagesToCheck = new HashSet<Title>(retval, new SimpleTitleEqualityComparer());
			var alreadyChecked = new HashSet<Title>(new SimpleTitleEqualityComparer());
			do
			{
				foreach (var page in pagesToCheck)
				{
					retval.AddBacklinks(page.FullPageName, BacklinksTypes.Backlinks, true, Filter.Only);
				}

				alreadyChecked.UnionWith(pagesToCheck);
				pagesToCheck.Clear();
				pagesToCheck.UnionWith(retval);
				pagesToCheck.ExceptWith(alreadyChecked);
			}
			while (pagesToCheck.Count > 0);

			return retval;
		}

		public PageCollection FollowRedirects(IEnumerable<Title> titles)
		{
			var originalsFollowed = PageCollection.Unlimited(this.Site, new PageLoadOptions(PageModules.None) { FollowRedirects = true });
			originalsFollowed.AddTitles(titles);

			return originalsFollowed;
		}
		#endregion

		#region Public Virtual Methods
		public virtual void Execute()
		{
			this.OnStarted(EventArgs.Empty);
			this.Main();
			this.OnCompleted(EventArgs.Empty);
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void Main();
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

		protected virtual void OnCompleted(EventArgs e) => this.Completed?.Invoke(this, e);

		protected virtual void OnStarted(EventArgs e) => this.Started?.Invoke(this, e);

		protected virtual void StatusWrite(string status)
		{
			this.AsyncInfo.StatusMonitor.Report(status);
			this.FlowControlAsync();
		}

		protected virtual void StatusWriteLine(string status) => this.StatusWrite(status + Environment.NewLine);

		protected virtual void UpdateProgress()
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			this.FlowControlAsync();
		}

		// Same as UpdateJobProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual void UpdateProgressWrite(string status)
		{
			this.AsyncInfo.ProgressMonitor.Report((double)this.Progress / this.ProgressMaximum);
			this.AsyncInfo.StatusMonitor.Report(status);
			this.FlowControlAsync();
		}

		// Same as UpdateJobProgress/UpdateStatus but with only one pause/cancel check.
		protected virtual void UpdateProgressWriteLine(string status) => this.UpdateProgressWrite(status + Environment.NewLine);
		#endregion
	}
}
