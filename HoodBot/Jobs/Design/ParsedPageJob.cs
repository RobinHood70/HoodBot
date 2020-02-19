﻿namespace RobinHood70.HoodBot.Jobs.Design
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	public abstract class ParsedPageJob : EditJob
	{
		#region Constructors
		protected ParsedPageJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Abstract Properties
		protected abstract string EditSummary { get; }
		#endregion

		#region Public Methods
		public TitleCollection GetAllTemplateTitles(params string[] templates)
		{
			var titles = new TitleCollection(this.Site, MediaWikiNamespaces.Template, templates);
			var pages = PageCollection.Unlimited(this.Site, new PageLoadOptions(PageModules.None, true));
			pages.GetTitles(titles);
			var pagesToCheck = new HashSet<Title>(pages, Title.SimpleEqualityComparer);
			var alreadyChecked = new HashSet<Title>(Title.SimpleEqualityComparer);
			do
			{
				foreach (var page in pagesToCheck)
				{
					pages.GetBacklinks(page.FullPageName, BacklinksTypes.Backlinks, true, Filter.Only);
				}

				alreadyChecked.UnionWith(pagesToCheck);
				pagesToCheck.Clear();
				pagesToCheck.UnionWith(pages);
				pagesToCheck.ExceptWith(alreadyChecked);
			}
			while (pagesToCheck.Count > 0);

			var retval = new TitleCollection(this.Site);
			foreach (var backlink in pages)
			{
				retval.GetBacklinks(backlink.FullPageName, BacklinksTypes.EmbeddedIn);
			}

			return retval;
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Loading Pages");
			this.Pages.PageLoaded += this.Results_PageLoaded;
			this.LoadPages();
			this.Pages.Sort();
			this.Pages.PageLoaded -= this.Results_PageLoaded;
		}

		protected override void Main() => this.SavePages(this.EditSummary, true, this.Results_PageLoaded);
		#endregion

		#region Protected Abstract Methods
		protected abstract void LoadPages();

		protected abstract void ParseText(object sender, Page page, ContextualParser parsedPage);
		#endregion

		#region Private Methods
		private void Results_PageLoaded(object sender, Page eventArgs)
		{
			var parsedPage = ContextualParser.FromPage(eventArgs);
			this.ParseText(sender, eventArgs, parsedPage);
			eventArgs.Text = WikiTextVisitor.Raw(parsedPage);
		}
		#endregion
	}
}