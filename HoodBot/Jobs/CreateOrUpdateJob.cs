namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;

public abstract class CreateOrUpdateJob<T>(JobManager jobManager) : EditJob(jobManager)
	where T : notnull
{
	#region Protected Properties
	protected bool Clobber { get; set; }

	protected Func<Title, T, string>? NewPageText { get; set; }

	protected Action<SiteParser, T>? OnExists { get; set; }

	protected Action<SiteParser, T>? OnUpdate { get; set; }

	protected PageLoadOptions? PageLoadOptions { get; set; }
	#endregion

	#region Protected Abstract Properties
	protected abstract string? Disambiguator { get; }
	#endregion

	#region Protected Abstract Methods
	protected abstract bool IsValid(SiteParser parser, T item);

	protected abstract IDictionary<Title, T> LoadItems();
	#endregion

	#region Protected Override Methods
	protected override void LoadPages()
	{
		var items = this.LoadItems();
		if (items.Count == 0)
		{
			return;
		}

		var parsedPages = new List<SiteParser>();
		var remaining = new TitleCollection(this.Site);
		remaining.AddRange(items.Keys);

		if (!this.Clobber)
		{
			this.GetBasePages(items, parsedPages, remaining);
			if (this.Disambiguator is not null && remaining.Count > 0)
			{
				this.GetDisambiguatedPages(items, parsedPages, remaining);
			}
		}

		parsedPages.AddRange(this.GetNewPages(items, remaining));
		this.DoOnExists(items, parsedPages);
		this.DoOnUpdate(items, parsedPages);

		foreach (var parser in parsedPages)
		{
			parser.UpdatePage();
			if (parser.Page.TextModified)
			{
				this.Pages.Add(parser.Page);
			}
		}
	}

	protected override void PageLoaded(Page page) => throw new NotSupportedException();
	#endregion

	#region Private Methods
	private void DoOnExists(IDictionary<Title, T> items, List<SiteParser> parsedPages)
	{
		if (this.OnExists is null)
		{
			return;
		}

		foreach (var parser in parsedPages)
		{
			if (parser.Page.Exists)
			{
				this.OnExists(parser, items[parser.Title]);
			}
		}
	}

	private void DoOnUpdate(IDictionary<Title, T> items, List<SiteParser> parsedPages)
	{
		if (this.OnUpdate is null)
		{
			return;
		}

		foreach (var parser in parsedPages)
		{
			this.OnUpdate(parser, items[parser.Page.Title]);
		}
	}

	private void GetBasePages(IDictionary<Title, T> items, List<SiteParser> parsedPages, TitleCollection remaining)
	{
		var found = new PageCollection(this.Site, this.PageLoadOptions);
		found.GetTitles(remaining);
		foreach (var page in found)
		{
			var title = page.Title;
			if (page.Exists)
			{
				var parser = new SiteParser(page);
				if (this.IsValid(parser, items[title]))
				{
					parsedPages.Add(parser);
					remaining.Remove(title);
					if (this.OnUpdate is null)
					{
						this.Warn(title.ToString() + " - valid page already exists, possible conflict.");
					}
				}
				else if (this.Disambiguator is not null)
				{
					var disambig = TitleFactory.FromValidated(title.Namespace, title.PageName + " (" + this.Disambiguator + ")");
					remaining.Remove(title);
					remaining.Add(disambig);
					var item = items[title];
					items.Remove(title);
					items.Add(disambig, item);
				}
			}
		}
	}

	private void GetDisambiguatedPages(IDictionary<Title, T> items, List<SiteParser> parsedPages, TitleCollection remaining)
	{
		var found = new PageCollection(this.Site);
		found.GetTitles(remaining);
		foreach (var page in found)
		{
			var title = page.Title;
			if (page.Exists)
			{
				var parser = new SiteParser(page);
				if (this.IsValid(parser, items[title]))
				{
					parsedPages.Add(parser);
					remaining.Remove(title);
				}
				else
				{
					throw new InvalidOperationException("Disambiguated page found but it fails validity check.");
				}
			}
		}
	}

	private IEnumerable<SiteParser> GetNewPages(IDictionary<Title, T> items, TitleCollection remaining)
	{
		if (this.NewPageText is null)
		{
			yield break;
		}

		foreach (var title in remaining)
		{
			var item = items[title];
			var text = this.NewPageText(title, item);
			var page = this.Site.CreatePage(title, text);
			var parser = new SiteParser(page);
			if (this.IsValid(parser, item))
			{
				yield return parser;
			}
			else
			{
				this.Warn($"New page [[{page.Title.FullPageName()}]] failed validity check.");
			}
		}
	}
	#endregion
}