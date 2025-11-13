namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;

public abstract class CreateOrUpdateJob<T> : EditJob
	where T : notnull
{
	#region Fields
	private readonly TitleCollection disambigTitles;
	private readonly TitleDictionary<Title> reverseTitleMap = [];
	private int phase;
	#endregion

	#region Constructors
	protected CreateOrUpdateJob(JobManager jobManager)
		: base(jobManager)
	{
		this.Pages = new PageCollection(this.Site, PageModules.Default, true);
		this.disambigTitles = new TitleCollection(this.Site);
	}
	#endregion

	#region Protected Properties
	protected bool Clobber { get; set; }

	protected TitleDictionary<T> Items { get; } = [];

	protected Func<Title, T, string>? NewPageText { get; set; }

	protected Action<SiteParser, T>? OnExists { get; set; }

	protected Action<SiteParser, T>? OnUpdate { get; set; }

	protected bool ThrowInvalid { get; set; }
	#endregion

	#region Protected Abstract Methods

	/// <summary>Gets the item data. The caller is responsible for storing it in whatever way makes sense, most likely a <c>Dictionary{key, T}</c>.</summary>
	protected virtual void GetData() => throw new NotImplementedException();

	protected abstract string? GetDisambiguator(T item);

	/// <summary>Loads any known-good pages into this.Pages, such as via a .GetBacklinks query.</summary>
	protected virtual void GetKnownPages() => throw new NotImplementedException();

	protected abstract bool IsValidPage(SiteParser parser, T item);

	protected abstract void LoadItems();
	#endregion

	#region Protected Virtual Methods
	protected virtual void DisambiguateWantedTitles(TitleCollection wantedTitles)
	{
#if DEBUG
		wantedTitles.Sort();
#endif
		var exists = new PageCollection(this.Site, PageModules.None, false)
			.GetTitles(wantedTitles);
		var originalTitles = new TitleDictionary<Title>();
		foreach (var page in exists)
		{
			if (page.Exists && this.Items.Remove(page.Title, out var item))
			{
				var disambigTitle = TitleFactory.FromUnvalidated(this.Site, $"{page.Title} ({this.GetDisambiguator(item)})");
				originalTitles.Add(disambigTitle, page.Title);
				this.Items.Add(disambigTitle, item); // Conflicts should be impossible here, I think. If there are, let it throw, then investigate.
				wantedTitles.Remove(page.Title);
				wantedTitles.Add(disambigTitle);
			}
		}
	}

	/// <summary>Figures out what new pages need to be created.</summary>
	/// <returns>A TitleCollection containing the list of pages to create.</returns>
	protected virtual TitleCollection GetWantedTitles()
	{
		var wantedTitles = new TitleCollection(this.Site);
		foreach (var title in this.Items.Keys)
		{
			if (!this.Pages.Contains(title))
			{
				wantedTitles.Add(title);
			}
		}

		return wantedTitles;
	}

	protected virtual void ValidPageLoaded(SiteParser parser, T item)
	{
		if (parser.Page.Exists && this.OnExists is not null)
		{
			this.OnExists(parser, item);
		}

		if (this.OnUpdate is not null)
		{
			this.OnUpdate(parser, item);
		}
	}
	#endregion

	#region Protected Override Methods
	protected override void BeforeLoadPages() => this.LoadItems();

	protected override void LoadPages()
	{
		if (this.Items.Count == 0)
		{
			return;
		}

		// Even if we're clobbering, we still check for valid pages that might exist, so we're clobbering the existing disambiguation in the rare instance where that might happen.
		this.GetKnownPages();
		var wantedTitles = this.GetWantedTitles();
		if (!this.Clobber)
		{
			this.DisambiguateWantedTitles(wantedTitles);
		}

		this.Pages.GetTitles(wantedTitles);
	}

	protected override void PageLoaded(Page page)
	{
		if (!this.reverseTitleMap.TryGetValue(page.Title, out var title))
		{
			title = page.Title;
		}

		if (!this.Items.TryGetValue(title, out var item))
		{
			// Item was removed due to a redirect conflict or by an external process - ignore it.
			Debug.WriteLine($"Item not found for {title}");
			this.Pages.Remove(page.Title); // THis is safe because we're looping over the API collection, not this.Pages.
			return;
		}

		var parser = new SiteParser(page);
		if (this.IsValidPage(parser, item))
		{
			this.ValidPageLoaded(parser, item);
		}
		else
		{
			if (page.Exists && this.ThrowInvalid)
			{
				throw new InvalidOperationException("Page found but failed validity check.");
			}

			if (this.phase == 1 && this.GetDisambiguator(item) is string disambigText)
			{
				var disambigTitle = TitleFactory.FromValidated(title.Namespace, $"{title.PageName} ({disambigText})");
				this.disambigTitles.Add(disambigTitle);
				this.Items.Remove(title);
				this.Items.Add(disambigTitle, item);
			}
		}
	}

	protected override void TitleMapLoaded()
	{
		foreach (var (from, to) in this.Pages.TitleMap)
		{
			var fromTitle = TitleFactory.FromUnvalidated(this.Site, from);
			this.reverseTitleMap[to.Title] = fromTitle;
			if (this.Items.ContainsKey(to.Title))
			{
				// We found a belated dupe caused by a redirect, so remove it from everything.
				Debug.WriteLine($"Item conflict: {fromTitle} => {to.Title}");
				this.Items.Remove(to.Title);
			}
		}
	}
	#endregion

	#region Private Methods
	private void LoadDisambiguatedPages()
	{
		this.ThrowInvalid = true;
		this.Pages.GetTitles(this.disambigTitles);
		this.ThrowInvalid = false;
	}

	private void GetNewPages()
	{
		if (this.NewPageText is null)
		{
			return;
		}

		foreach (var (title, item) in this.Items)
		{
			if (!this.Pages.TryGetValue(title, out var page))
			{
				page = this.Site.CreatePage(title);
			}

			if (page.IsMissing || this.Clobber)
			{
				page.Text = this.NewPageText(title, item);
				var parser = new SiteParser(page);
				if (this.IsValidPage(parser, item))
				{
					this.Pages.TryAdd(page);
					this.ValidPageLoaded(parser, item);
				}
				else
				{
					this.Warn($"New page [[{page.Title}]] fails validity check.");
				}
			}
		}
	}
	#endregion
}