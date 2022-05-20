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
		#region Fields
		private readonly IDictionary<Title, SaveInfo> saveInfo = new Dictionary<Title, SaveInfo>(SimpleTitleComparer.Instance);
		#endregion

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

		protected bool Shuffle { get; set; }
		#endregion

		#region Protected Methods
		protected TitleCollection LoadProposedDeletions()
		{
			TitleCollection deleted = new(this.Site);
			foreach (var title in this.Site.DeletionCategories)
			{
				deleted.GetCategoryMembers(title.PageName);
			}

			return deleted;
		}

		protected void SavePage(Page page, string editSummary, bool isMinor)
		{
			page.ThrowNull();
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
					page = new Title(page).Load();
					this.EditConflictAction(this, page);
				}
				catch (WikiException we) when (string.Equals(we.Code, "pagedeleted", StringComparison.Ordinal))
				{
					this.Warn("Page not saved because it was previously deleted.");
					saved = true;
				}
			}
		}

		protected void SavePages(string defaultSummary) => this.SavePages(defaultSummary, true, null);

		protected void SavePages(string defaultSummary, bool defaultIsMinor) => this.SavePages(defaultSummary, defaultIsMinor, null);

		protected void SavePages(string defaultSummary, bool defaultIsMinor, Action<EditJob, Page>? editConflictAction)
		{
			this.StatusWriteLine("Saving pages");
			this.SavePages(this.Pages, defaultSummary, defaultIsMinor, editConflictAction);
		}

		protected void SavePages(PageCollection pages, string defaultSummary, bool defaultIsMinor, Action<EditJob, Page>? editConflictAction)
		{
			pages.NotNull().RemoveChanged(false);
			if (pages.Count == 0)
			{
				this.StatusWriteLine("No pages to save!");
				return;
			}

			this.EditConflictAction = editConflictAction;
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
				if (this.saveInfo.TryGetValue(page, out var retval))
				{
					defaultSummary = retval.EditSummary;
					defaultIsMinor = retval.IsMinor;
				}

				this.SavePage(page, defaultSummary, defaultIsMinor);
				this.Progress++;
			}
		}

		protected void SetSaveInfoForPage(Title page, string editSummary, bool isMinor) => this.saveInfo[page.NotNull()] = new SaveInfo(editSummary.NotNull(), isMinor);
		#endregion

		#region Protected Abstract Overrie Methods
		protected abstract override void BeforeLogging();
		#endregion

		#region Private Structures
		public struct SaveInfo
		{
			#region Constructors
			public SaveInfo(string editSummary, bool isMinor)
			{
				this.EditSummary = editSummary;
				this.IsMinor = isMinor;
			}
			#endregion

			#region Public Properties
			public string EditSummary { get; }

			public bool IsMinor { get; }
			#endregion
		}
		#endregion
	}
}