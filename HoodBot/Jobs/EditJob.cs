namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;

public abstract class EditJob(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
{
	#region Protected Properties
	protected Tristate CreateOnly { get; set; } = Tristate.Unknown;

	protected bool IgnorePageCount { get; set; }

	// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
	protected PageCollection Pages { get; init; } = new PageCollection(jobManager.Site);

	protected bool RecreateIfDeleted { get; set; } = true;

	protected TitleCollection SafeToRecreate { get; } = new TitleCollection(jobManager.Site);

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
				// recreateIfJustDeleted is set to true here because it should have been handled by the SavePages method. If set to this.RecreateIfDeleted, it would disallow saving a deleted page, even if the job tagged the page as safe to recreate.
				page.Save(editSummary, isMinor, this.CreateOnly, true);
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
			catch (WikiException we) when (!this.RecreateIfDeleted && we.Code.OrdinalEquals("pagedeleted"))
			{
				this.Warn($"Page {page.Title} not saved because it was recently deleted.");
				saved = true; // Mark as saved to prevent further attempts to save it.
			}
		}
	}

	protected void SavePages()
	{
		this.Pages.RemoveChanged(false);
		this.RemoveDeletedPages();
		if (this.Pages.Count == 0)
		{
			this.StatusWriteLine("No pages to save!");
			return;
		}

		var plural = this.Pages.Count == 1 ? string.Empty : "s";
		this.StatusWriteLine($"Saving {this.Pages.Count} page{plural}");
		if (this.Shuffle && !this.Site.EditingEnabled)
		{
			this.Pages.Shuffle();
		}
		else
		{
			this.Pages.Sort(NaturalTitleComparer.Instance);
		}

		this.ResetProgress(this.Pages.Count);
		foreach (var page in this.Pages)
		{
			this.SavePage(page, this.GetEditSummary(page), this.GetIsMinorEdit(page));
			this.Progress++;
		}
	}
	#endregion

	#region Protected Override Methods
	protected override bool BeforeLogging()
	{
		this.BeforeLoadPages();
		this.Pages.TitleMapLoaded += this.Pages_TitleMapLoaded;
		this.Pages.PageMissing += this.Pages_PageMissing;
		this.Pages.PageLoaded += this.Pages_PageLoaded;
		this.StatusWriteLine("Loading pages");
		this.LoadPages();
		this.StatusWriteLine("Finished loading");
		this.Pages.PageLoaded -= this.Pages_PageLoaded;
		this.Pages.PageMissing -= this.Pages_PageMissing;
		this.Pages.TitleMapLoaded -= this.Pages_TitleMapLoaded;
		if (this.Pages.Count == 0 && !this.IgnorePageCount)
		{
			return false;
		}

		this.AfterLoadPages();
		return true;
	}

	protected override void Main() => this.SavePages();
	#endregion

	#region Protected Abstract Methods

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
	/// <param name="page">The page affected.</param>
	/// <returns><see langword="true"/> if the handler handled the conflict; otherwise, <see langword="false"/>.</returns>
	/// <remarks>During a SavePage, if an edit conflict occurs and this property is non-null, the page will automatically be re-loaded and the method specified here will be executed. If the method returns false, an error will be thrown.</remarks>
	protected virtual bool OnEditConflict(Page page) => true; // Assumes OnLoad/OnMissing have sufficiently handled required edits.

	protected virtual void PageMissing(Page page)
	{
	}

	protected virtual void TitleMapLoaded()
	{
	}
	#endregion

	#region Private Methods
	private IEnumerable<LogEventsItem> GetDeletionLog(IEnumerable<int> namespaces)
	{
		foreach (var ns in namespaces)
		{
			var input = LogEventsInput.FromNamespace(ns);
			input.Properties = LogEventsProperties.Title | LogEventsProperties.User | LogEventsProperties.Comment | LogEventsProperties.Timestamp;
			input.Type = "delete";
			foreach (var deletion in this.Site.AbstractionLayer.LogEvents(input))
			{
				yield return deletion;
			}
		}
	}

	private void Pages_PageLoaded(PageCollection sender, Page eventArgs) => this.PageLoaded(eventArgs);

	private void Pages_PageMissing(PageCollection sender, Page eventArgs) => this.PageMissing(eventArgs);

	private void Pages_TitleMapLoaded(object? sender, EventArgs e) => this.TitleMapLoaded();

	private void RemoveDeletedPages()
	{
		if (this.RecreateIfDeleted)
		{
			return;
		}

		var namespaces = this.Pages
			.Select(p => p.Title.Namespace.Id)
			.Distinct()
			.ToArray(); // Has to be a static collection because Pages might be modified while enumerating the log.
		foreach (var deletion in this.GetDeletionLog(namespaces))
		{
			if (deletion.Title is not null &&
				this.Pages.TryGetValue(deletion.Title, out var page) &&
				page.IsMissing &&
				!this.SafeToRecreate.Contains(deletion.Title))
			{
				this.Pages.Remove(deletion.Title);
				this.StatusWriteLine($"Deleted page [[{deletion.Title}]] ignored. Deleted on {deletion.Timestamp} by {deletion.User} with comment: {deletion.Comment}");
			}
		}
	}
	#endregion
}