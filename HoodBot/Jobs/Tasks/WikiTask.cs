namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
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

	public abstract class WikiTask
	{
		#region Fields
		private readonly Regex alreadyProposed;
		private readonly Regex neverPropose;
		#endregion

		#region Constructors
		protected WikiTask(WikiTask parent)
		{
			ThrowNull(parent, nameof(parent));
			this.Site = parent.Site;
			this.Parent = parent;
			this.Job = parent.Job ?? (parent as WikiJob);
			this.alreadyProposed = Template.Find(this.Site.UserFunctions.DeleteTemplates);
			this.neverPropose = Template.Find(this.Site.UserFunctions.DoNotDeleteTemplates);
		}

		protected WikiTask([ValidatedNotNull] Site site)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<WikiTask, EventArgs> Completed;

		public event StrongEventHandler<WikiTask, EventArgs> RunningTasks;

		public event StrongEventHandler<WikiTask, EventArgs> Started;
		#endregion

		#region Public Properties
		public WikiJob Job { get; } // Top-level Job object.

		public WikiTask Parent { get; } // Immediate parent, in the event of task nesting.

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
			var originalsFollowed = PageCollection.Unlimited(this.Site, new PageLoadOptions(PageModules.None) { FollowRedirects = true });
			originalsFollowed.GetTitles(titles);

			return originalsFollowed;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Method performs a time-consuming operation (at least relative to a property).")]
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

		public ProposedDeletionResult CanDelete(Page page)
		{
			ThrowNull(page, nameof(page));
			if (!page.IsLoaded)
			{
				page.Load();
			}

			if (!page.Exists)
			{
				return ProposedDeletionResult.NonExistent;
			}
			else if (this.neverPropose.IsMatch(page.Text))
			{
				return ProposedDeletionResult.FoundNoDeleteRequest;
			}
			else if (this.alreadyProposed.IsMatch(page.Text))
			{
				return ProposedDeletionResult.AlreadyProposed;
			}
			else
			{
				return ProposedDeletionResult.Add;
			}
		}

		public ProposedDeletionResult ProposeForDeletion(Page page, Template deletionTemplate)
		{
			ThrowNull(page, nameof(page));
			var retval = this.CanDelete(page);
			if (retval == ProposedDeletionResult.Add)
			{
				var deletionText = deletionTemplate.ToString();
				var status = ChangeStatus.Unknown;
				while (status != ChangeStatus.Success && status != ChangeStatus.EditingDisabled)
				{
					page.Text =
						page.Namespace == MediaWikiNamespaces.Template ? "<noinclude>" + deletionText + "</noinclude>" :
						page.IsRedirect ? page.Text + '\n' + deletionText :
						deletionText + '\n' + page.Text;
					status = page.Save("Propose for deletion", false);
				}
			}

			return retval;
		}
		#endregion

		#region Public Methods
		public virtual void Execute()
		{
			this.OnStarted();
			this.ProgressMaximum = this.GetProgressEstimate();
			this.Main();
			this.OnRunningTasks();
			this.RunTasks();
			this.OnCompleted();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void Main();
		#endregion

		#region Protected Virtual Methods
		protected virtual void OnCompleted() => this.Completed?.Invoke(this, EventArgs.Empty);

		protected virtual void OnRunningTasks() => this.RunningTasks?.Invoke(this, EventArgs.Empty);

		protected virtual void OnStarted() => this.Started?.Invoke(this, EventArgs.Empty);

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