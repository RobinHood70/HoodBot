namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;

	public abstract class EditJob(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
	{
		#region Protected Properties
		protected Tristate CreateOnly { get; set; } = Tristate.Unknown;

		// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
		protected PageCollection Pages { get; init; } = new PageCollection(jobManager.Site);

		protected bool RecreateIfDeleted { get; set; } = true;

		protected bool SaveOverDeleted { get; set; } = true;

		protected bool Shuffle { get; set; }
		#endregion

		#region Protected Methods
		protected void SavePage(Page page)
		{
			ArgumentNullException.ThrowIfNull(page);
			this.SavePage(page, this.GetEditSummary(page), this.GetIsMinorEdit(page));
		}

		protected void SavePage(Page page, string editSummary, bool isMinor)
		{
			ArgumentNullException.ThrowIfNull(page);
			var saved = false;
			while (!saved)
			{
				try
				{
					page.Save(editSummary, isMinor, this.CreateOnly, this.RecreateIfDeleted);
					saved = true;
				}
				catch (EditConflictException)
				{
					page = page.Title.Load();
					if (page.IsMissing || string.IsNullOrWhiteSpace(page.Text))
					{
						this.PageMissing(page);
					}

					this.PageLoaded(page);
					if (!this.OnEditConflict(page))
					{
						throw;
					}
				}
				catch (WikiException we) when (!this.SaveOverDeleted && string.Equals(we.Code, "pagedeleted", StringComparison.Ordinal))
				{
					this.Warn("Page not saved because it was previously deleted.");
					saved = true;
				}
			}
		}

		protected void SavePages()
		{
			this.Pages.RemoveChanged(false);
			if (this.Pages.Count == 0)
			{
				this.StatusWriteLine("No pages to save!");
				return;
			}

			this.StatusWriteLine("Saving pages");
			if (this.Shuffle && !this.Site.EditingEnabled)
			{
				this.Pages.Shuffle();
			}
			else
			{
				this.Pages.Sort(NaturalTitleComparer.Instance);
			}

			this.Progress = 0;
			this.ProgressMaximum = this.Pages.Count;
			foreach (var page in this.Pages)
			{
				this.SavePage(page, this.GetEditSummary(page), this.GetIsMinorEdit(page));
				this.Progress++;
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.BeforeLoadPages();
			this.Pages.PageMissing += this.Pages_PageMissing;
			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.StatusWriteLine("Loading pages");
			this.LoadPages();
			this.StatusWriteLine("Finished loading");
			this.Pages.PageLoaded -= this.Pages_PageLoaded;
			this.Pages.PageMissing -= this.Pages_PageMissing;
			this.AfterLoadPages();
		}

		protected override void Main() => this.SavePages();
		#endregion

		#region Protected Abstract Methods

		/// <summary>Gets the edit summary to use for the given page.</summary>
		protected abstract string GetEditSummary(Page page);

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

		protected virtual bool GetIsMinorEdit(Page page)
		{
			ArgumentNullException.ThrowIfNull(page);
			return page.Exists;
		}

		/// <summary>The action to take when there's an edit conflict on a page.</summary>
		/// <returns><see langword="true"/> if the handler handled the conflict; otherwise, <see langword="false"/>.</returns>
		/// <remarks>During a SavePage, if an edit conflict occurs and this property is non-null, the page will automatically be re-loaded and the method specified here will be executed. If the method returns false, an error will be thrown.</remarks>
		protected virtual bool OnEditConflict(Page page) => true; // Assumes OnLoad/OnMissing have sufficiently handled required edits.

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