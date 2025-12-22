namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;

public abstract class CreateOrUpdateJob<T>(JobManager jobManager) : EditJob(jobManager)
	where T : notnull
{
	#region Fields
	private readonly TitleDictionary<T> dictionary = [];
	#endregion

	#region Protected Properties
	protected bool Clobber { get; set; }

	// TODO: Should be made into a read-only dictionary so processes can only add to Items via the designated methods.
	protected IDictionary<Title, T> Items => this.dictionary;

	protected Func<Title, T, string>? NewPageText { get; set; }

	protected Action<SiteParser, T>? OnUpdate { get; set; }

	protected bool ThrowInvalid { get; set; }
	#endregion

	#region Protected Abstract Methods

	protected abstract string? GetDisambiguator(T item);

	protected abstract bool IsValidPage(SiteParser parser, T item);

	protected virtual void LoadItems()
	{
		this.GetExternalData();
		var existing = this.GetExistingItems();
		this.dictionary.AddRange(existing);

		var newItems = this.GetNewItems();
		if (!this.Clobber)
		{
			this.DisambiguateNewItems(newItems);
		}

		this.dictionary.AddRange(newItems);
	}
	#endregion

	#region Protected Virtual Methods
	protected virtual void DisambiguateNewItems(TitleDictionary<T> newItems)
	{
		var pages = new PageCollection(this.Site, PageModules.None, false)
			.GetTitles(newItems.ToTitleCollection(this.Site));
		foreach (var page in pages)
		{
			if (page.Exists && newItems.Remove(page.Title, out var item))
			{
				var disambigTitle = TitleFactory.FromUnvalidated(this.Site, $"{page.Title} ({this.GetDisambiguator(item)})");
				newItems.Remove(page.Title);
				newItems.Add(disambigTitle, item);
			}
		}
	}

	/// <summary>Gets the list of existing titles. Do *NOT* load these via <see cref="Pages"/> or add them to <see cref="Items"/> - that will be done after pages are disambiguated, if appropriate.</summary>
	/// <remarks>This is called even when <see cref="Clobber"/> is true so that if valid pages exist, they're the ones that get clobbered. If the job is create-only, there's no need to override it.</remarks>
	protected abstract TitleDictionary<T> GetExistingItems();

	protected abstract void GetExternalData();

	/// <summary>Figures out what new pages need to be created.</summary>
	/// <param name="existing">Existing items on the wiki.</param>
	/// <returns>A <see cref="TitleDictionary{T}"/> containing the list of pages to create.</returns>
	protected abstract TitleDictionary<T> GetNewItems();

	protected virtual void ValidPageLoaded(SiteParser parser, T item)
	{
	}
	#endregion

	#region Protected Override Methods
	protected override void LoadPages()
	{
		this.LoadItems();
		var titles = new TitleCollection(this.Site, this.dictionary.Keys);
		this.Pages.GetTitles(titles);
	}

	protected override void PageLoaded(Page page)
	{
		if (!this.Items.TryGetValue(page.Title, out var item))
		{
			// Item was removed due to a redirect conflict or by an external process - ignore it.
			Debug.WriteLine($"Item not found for {page.Title}");
			this.Pages.Remove(page.Title); // This is safe because we're looping over the API collection, not this.Pages.
			return;
		}

		if (this.NewPageText is not null && (page.IsMissing || page.Text.Length == 0))
		{
			page.Text = this.NewPageText(page.Title, item);
		}

		var parser = new SiteParser(page);
		if (this.IsValidPage(parser, item))
		{
			if (this.OnUpdate is not null)
			{
				this.OnUpdate(parser, item);
			}

			parser.UpdatePage();
		}
		else if (page.Exists && this.ThrowInvalid)
		{
			throw new InvalidOperationException($"Page failed validity check: {page.Title}");
		}
	}

	protected override void TitleMapLoaded()
	{
		foreach (var (from, to) in this.Pages.TitleMap)
		{
			var fromTitle = TitleFactory.FromUnvalidated(this.Site, from);
			if (this.Items.ContainsKey(to.Title))
			{
				// We found a belated dupe caused by a redirect, so remove it from everything.
				Debug.WriteLine($"Item conflict: {fromTitle} => {to.Title}");
				this.dictionary.Remove(to.Title);
			}
		}
	}
	#endregion
}