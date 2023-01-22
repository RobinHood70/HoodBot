namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;

	public abstract class EditJob : WikiJob
	{
		#region Constructors
		protected EditJob(JobManager jobManager)
			: base(jobManager, JobType.Write)
		{
			this.Pages = new PageCollection(this.Site);
		}
		#endregion

		#region Protected Properties
		protected Tristate CreateOnly { get; set; } = Tristate.Unknown;

		protected bool MinorEdit { get; set; } = true;

		protected bool RecreateIfDeleted { get; set; } = true;

		// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
		protected PageCollection Pages { get; set; }

		protected bool SaveOverDeleted { get; set; } = true;

		protected bool Shuffle { get; set; }
		#endregion

		#region Protected Abstract Properties

		/// <summary>Gets the edit summary to use by default for all edits.</summary>
		protected abstract string EditSummary { get; }
		#endregion

		#region Protected Virtual Properties

		/// <summary>Gets the edit conflict action.</summary>
		/// <value>The edit conflict action.</value>
		/// <remarks>During a SavePage, if an edit conflict occurs and this property is non-null, the page will automatically be re-loaded and the method specified here will be executed.</remarks>
		protected virtual Action<EditJob, Page>? EditConflictAction { get; }
		#endregion

		#region Protected Methods
		protected void SavePage(Page page) => this.SavePage(page.NotNull(), this.EditSummary, this.MinorEdit, this.EditConflictAction);

		protected void SavePage(Page page, string editSummary, bool isMinor, Action<EditJob, Page>? editConflictAction)
		{
			page.ThrowNull();
			var saved = false;
			while (!saved)
			{
				try
				{
					page.Save(editSummary, isMinor, this.CreateOnly, this.RecreateIfDeleted);
					saved = true;
				}
				catch (EditConflictException) when (editConflictAction != null)
				{
					page = new Title(page).Load();
					if (page.IsMissing || string.IsNullOrWhiteSpace(page.Text))
					{
						this.PageMissing(page);
					}

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
			this.Pages.PageMissing += this.Pages_PageMissing;
			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.LoadPages();
			this.Pages.PageLoaded -= this.Pages_PageLoaded;
			this.Pages.PageMissing -= this.Pages_PageMissing;
			this.AfterLoadPages();
		}

		protected override void Main() => this.SavePages();
		#endregion

		#region Protected Abstract Methods
		protected abstract void LoadPages();

		protected abstract void PageLoaded(Page page);
		#endregion

		#region Protected Virtual Methods
		protected virtual void AfterLoadPages()
		{
		}

		protected virtual void BeforeLoadPages()
		{
		}

		protected virtual void PageMissing(Page page)
		{
		}
		#endregion

		#region Private Methods
		private void Pages_PageLoaded(PageCollection sender, Page eventArgs) => this.PageLoaded(eventArgs);

		private void Pages_PageMissing(PageCollection sender, Page eventArgs) => this.PageMissing(eventArgs);
		#endregion
	}
}