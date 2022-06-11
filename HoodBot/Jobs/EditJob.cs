namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;

	public abstract class EditJob : WikiJob
	{
		#region Constructors
		protected EditJob(JobManager jobManager)
			: base(jobManager)
		{
			this.Pages = new PageCollection(this.Site);
		}
		#endregion

		#region Public Override Properties
		public override JobTypes JobType => JobTypes.Read | JobTypes.Write;
		#endregion

		#region Protected Properties
		protected IDictionary<Title, string> CustomEditSummaries { get; } = new Dictionary<Title, string>(SimpleTitleComparer.Instance);

		protected IDictionary<Title, bool> CustomMinorEdits { get; } = new Dictionary<Title, bool>(SimpleTitleComparer.Instance);

		// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
		protected PageCollection Pages { get; }

		protected bool Shuffle { get; set; }
		#endregion

		#region Protected Virtual Properties
		protected virtual bool MinorEdit => true;

		protected virtual bool SaveOverDeleted => true;
		#endregion

		#region Protected Abstract Properties

		/// <summary>Gets the edit conflict action.</summary>
		/// <value>The edit conflict action.</value>
		/// <remarks>During a SavePage, if an edit conflict occurs and this property is non-null, the page will automatically be re-loaded and the method specified here will be executed.</remarks>
		protected abstract Action<EditJob, Page>? EditConflictAction { get; }

		/// <summary>Gets the edit summary to use by default for all edits.</summary>
		protected abstract string EditSummary { get; }
		#endregion

		#region Protected Methods
		protected void SavePage(Page page) => this.SavePage(page.NotNull(), this.EditSummary, this.MinorEdit, this.EditConflictAction);

		protected void SavePage(Page page, string defaultSummary, bool defaultIsMinor, Action<EditJob, Page>? editConflictAction)
		{
			page.ThrowNull();
			var saved = false;
			while (!saved)
			{
				try
				{
					var editSummary = this.CustomEditSummaries.TryGetValue(page, out var customSummary) ? customSummary : defaultSummary;
					var isMinor = this.CustomMinorEdits.TryGetValue(page, out var customIsMinor) ? customIsMinor : defaultIsMinor;
					page.Save(editSummary, isMinor);
					saved = true;
				}
				catch (EditConflictException) when (editConflictAction != null)
				{
					page = new Title(page).Load();
					editConflictAction(this, page);
				}
				catch (WikiException we) when (!this.SaveOverDeleted && string.Equals(we.Code, "pagedeleted", StringComparison.Ordinal))
				{
					this.Warn("Page not saved because it was previously deleted.");
					saved = true;
				}
			}
		}

		protected void SavePages() => this.SavePages(this.Pages, this.EditSummary, this.MinorEdit, this.EditConflictAction);

		protected void SavePages(PageCollection pages, string defaultSummary, bool defaultIsMinor, Action<EditJob, Page>? editConflictAction)
		{
			pages.NotNull().RemoveChanged(false);
			if (pages.Count == 0)
			{
				this.StatusWriteLine("No pages to save!");
				return;
			}

			if (this.Shuffle && !this.Site.EditingEnabled)
			{
				pages.Shuffle();
			}
			else
			{
				pages.Sort(NaturalTitleComparer.Instance);
			}

			this.Progress = 0;
			this.ProgressMaximum = pages.Count;
			foreach (var page in pages)
			{
				this.SavePage(page, defaultSummary, defaultIsMinor, editConflictAction);
				this.Progress++;
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.BeforeLoadPages();
			this.StatusWriteLine("Loading pages");
			this.LoadPages();

			foreach (var page in this.Pages)
			{
				this.PageLoaded(this, page);
			}

			this.AfterLoadPages();
			this.Results?.Clear();
		}

		protected override void Main() => this.SavePages();
		#endregion

		#region Protected Virtual Methods
		protected virtual void AfterLoadPages()
		{
		}

		protected virtual void BeforeLoadPages()
		{
		}

		protected virtual void NewPage(Page page)
		{
		}

		protected virtual void PageLoaded(object sender, Page page)
		{
			if (page.IsMissing || string.IsNullOrWhiteSpace(page.Text))
			{
				this.NewPage(page);
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void LoadPages();
		#endregion
	}
}