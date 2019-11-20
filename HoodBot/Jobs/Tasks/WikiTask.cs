namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Internal Enumerations
	public enum ProposedDeletionResult
	{
		Add,
		AlreadyProposed,
		FoundNoDeleteRequest,
		NonExistent,
	}
	#endregion

	public abstract class WikiTask : ISiteSpecific
	{
		#region Constructors
		protected WikiTask(WikiTask parent)
		{
			ThrowNull(parent, nameof(parent));
			this.Site = parent.Site;
			this.Parent = parent;
			this.Job = parent.Job ?? (WikiJob)parent;
		}

		protected WikiTask([ValidatedNotNull] Site site)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<WikiTask, EventArgs>? Completed;

		public event StrongEventHandler<WikiTask, EventArgs>? RunningTasks;

		public event StrongEventHandler<WikiTask, EventArgs>? Started;
		#endregion

		#region Public Properties
		public WikiJob? Job { get; } // Top-level Job object.

		public WikiTask? Parent { get; } // Immediate parent, in the event of task nesting.

		public int ProgressMaximum { get; protected set; } = 1;

		public Site Site { get; }
		#endregion

		#region Protected Properties
		protected IList<WikiTask> Tasks { get; } = new List<WikiTask>(); // Might want this to be public so edit tasks can be added by caller.
		#endregion

		#region Public Methods
		public static TitleCollection BuildRedirectList(IEnumerable<Title> titles)
		{
			var retval = TitleCollection.CopyFrom(titles);

			// Loop until nothing new is added.
			var pagesToCheck = new HashSet<Title>(retval, Title.SimpleEqualityComparer);
			var alreadyChecked = new HashSet<Title>(Title.SimpleEqualityComparer);
			do
			{
				foreach (var page in pagesToCheck)
				{
					retval.GetBacklinks(page.FullPageName, BacklinksTypes.Backlinks, true, Filter.Only);
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
			var originalsFollowed = PageCollection.Unlimited(this.Site, new PageLoadOptions(PageModules.None, true));
			originalsFollowed.GetTitles(titles);

			return originalsFollowed;
		}

		public int GetProgressEstimate()
		{
			if (this.Tasks == null)
			{
				return this.ProgressMaximum;
			}

			var total = this.ProgressMaximum;
			foreach (var task in this.Tasks)
			{
				total += task.GetProgressEstimate();
			}

			return total;
		}
		#endregion

		#region Public Methods
		public virtual void Execute()
		{
			this.BeforeMain();
			this.ProgressMaximum = this.GetProgressEstimate();
			this.Main();
			this.OnRunningTasks();
			this.RunTasks();
			this.JobCompleted();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void Main();
		#endregion

		#region Protected Virtual Methods
		protected virtual void JobCompleted() => this.Completed?.Invoke(this, EventArgs.Empty);

		protected virtual void OnRunningTasks() => this.RunningTasks?.Invoke(this, EventArgs.Empty);

		protected virtual void BeforeMain() => this.Started?.Invoke(this, EventArgs.Empty);

		protected virtual void RunTasks()
		{
			foreach (var task in this.Tasks)
			{
				var sw = new Stopwatch();
				sw.Start();
				task.Execute();
				sw.Stop();
				Debug.WriteLine($"{task.GetType().Name}: {sw.ElapsedMilliseconds}");
			}
		}
		#endregion
	}
}