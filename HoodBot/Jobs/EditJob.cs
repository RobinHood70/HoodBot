namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using static RobinHood70.CommonCode.Globals;

	public abstract class EditJob : WikiJob
	{
		#region Constructors
		protected EditJob(JobManager jobManager)
			: base(jobManager)
		{
			this.JobType = JobTypes.Read | JobTypes.Write;
			this.Pages = new PageCollection(this.Site);
		}
		#endregion

		#region Public Properties

		// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
		public PageCollection Pages { get; }
		#endregion

		#region Protected Properties

		/// <summary>Gets or sets the edit conflict action.</summary>
		/// <value>The edit conflict action.</value>
		/// <remarks>During a SavePage, if an edit conflict occurs, the page will automatically be re-loaded and the method specified here will be executed.</remarks>
		protected Action<EditJob, Page>? EditConflictAction { get; set; }
		#endregion

		#region Protected Methods
		protected TitleCollection LoadProposedDeletions()
		{
			var deleted = new TitleCollection(this.Site);
			foreach (var title in this.Site.DeletionCategories)
			{
				deleted.GetCategoryMembers(title.PageName);
			}

			return deleted;
		}

		protected void SavePage(Page page, string editSummary, bool isMinor)
		{
			ThrowNull(page, nameof(page));
			var saved = false;
			while (!saved)
			{
				try
				{
					page.Save(editSummary, isMinor);
					saved = true;
				}
				catch (EditConflictException) when (this.EditConflictAction != null)
				{
					page.Load();
					this.EditConflictAction(this, page);
				}
				catch (WikiException we) when (string.Equals(we.Code, "pagedeleted", StringComparison.Ordinal))
				{
					this.Warn("Page not saved because it was previously deleted.");
					saved = true;
				}
			}
		}

		protected void SavePages(string editSummary) => this.SavePages(editSummary, true, null);

		protected void SavePages(string editSummary, bool isMinor) => this.SavePages(editSummary, isMinor, null);

		protected void SavePages(string editSummary, bool isMinor, Action<EditJob, Page>? editConflictAction) => this.SavePages(this.Pages, "Saving pages", editSummary, isMinor, editConflictAction);

		protected void SavePages(PageCollection pages, string status, string editSummary, bool isMinor, Action<EditJob, Page>? editConflictAction)
		{
			ThrowNull(pages, nameof(pages));
			this.StatusWriteLine(status);
			pages.RemoveUnchanged();
			if (pages.Count == 0)
			{
				this.StatusWriteLine("No pages to save!");
			}
			else
			{
				this.EditConflictAction = editConflictAction;
				pages.Sort(NaturalTitleComparer.Instance);
				this.Progress = 0;
				this.ProgressMaximum = pages.Count;
				foreach (var page in pages)
				{
					this.SavePage(page, editSummary, isMinor);
					this.Progress++;
				}
			}
		}
		#endregion

		#region Protected Abstract Overrie Methods
		protected abstract override void BeforeLogging();
		#endregion
	}
}