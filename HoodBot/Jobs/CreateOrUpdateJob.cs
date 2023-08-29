namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	public abstract class CreateOrUpdateJob<T> : EditJob
		where T : notnull
	{
		#region Constructors
		protected CreateOrUpdateJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Properties
		protected bool Clobber { get; set; }
		#endregion

		#region Protected Abstract Properties
		protected abstract string? Disambiguator { get; }
		#endregion

		#region Protected Abstract Methods
		protected abstract IDictionary<Title, T> LoadItems();

		protected abstract bool IsValid(ContextualParser parser, T item);

		protected abstract string NewPageText(Title title, T item);
		#endregion

		#region Protected Virtual Methods
		protected virtual void PageLoaded(ContextualParser parser, T item)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			var items = this.LoadItems();
			var parsedPages = new List<ContextualParser>();
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

			if (remaining.Count > 0)
			{
				this.CreateNewPages(items, parsedPages, remaining);
			}

			this.UpdatePages(items, parsedPages);
		}

		protected override void PageLoaded(Page page) => throw new NotSupportedException();
		#endregion

		#region Private Methods
		private void CreateNewPages(IDictionary<Title, T> items, List<ContextualParser> parsedPages, TitleCollection remaining)
		{
			foreach (var title in remaining)
			{
				var text = this.NewPageText(title, items[title]);
				var page = this.Site.CreatePage(title, text);
				var parser = new ContextualParser(page);
				if (this.IsValid(parser, items[title]))
				{
					parsedPages.Add(parser);
				}
				else
				{
					this.Warn($"New page [[{page.Title.FullPageName()}]] failed validity check.");
				}
			}
		}

		private void GetBasePages(IDictionary<Title, T> items, List<ContextualParser> parsedPages, TitleCollection remaining)
		{
			var found = new PageCollection(this.Site);
			found.GetTitles(remaining);
			foreach (var page in found)
			{
				var title = page.Title;
				if (page.Exists)
				{
					var parser = new ContextualParser(page);
					if (this.IsValid(parser, items[title]))
					{
						parsedPages.Add(parser);
						remaining.Remove(title);
						continue;
					}
				}

				if (this.Disambiguator is not null)
				{
					remaining.Remove(title);
					remaining.Add(title.FullPageName() + " (" + this.Disambiguator + ")");
				}
			}
		}

		private void GetDisambiguatedPages(IDictionary<Title, T> items, List<ContextualParser> parsedPages, TitleCollection remaining)
		{
			var found = new PageCollection(this.Site);
			found.GetTitles(remaining);
			foreach (var page in found)
			{
				if (page.Exists)
				{
					var parser = new ContextualParser(page);
					var title = page.Title;
					var trimmed = TitleFactory.FromValidated(title.Namespace, title.PageName[..^(this.Disambiguator!.Length - 3)]);
					if (this.IsValid(parser, items[trimmed]))
					{
						parsedPages.Add(parser);
						remaining.Remove(title);
					}
				}
			}
		}

		private void UpdatePages(IDictionary<Title, T> items, List<ContextualParser> parsedPages)
		{
			foreach (var parser in parsedPages)
			{
				this.PageLoaded(parser, items[parser.Page.Title]);
				parser.UpdatePage();
				this.Pages.Add(parser.Page);
			}
		}
		#endregion
	}
}