﻿namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class DBMergeCheck : EditJob
	{
		#region Fields
		private TitleCollection filter;
		#endregion

		#region Constructors
		[JobInfo("Final Check", "Dragonborn Merge")]
		public DBMergeCheck([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			var checkPage = new Title(this.Site, "Project:Dragonborn Merge Project/Merge Results");

			this.Logger = null;
			this.Results = new PageResultHandler(checkPage);

			this.filter = new TitleCollection(site)
			{
				checkPage,
				"User:HoodBot/Results",
				"Project:Dragonborn Merge Project",
			};
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging() => this.filter.AddRange(this.GetProposedDeletions());

		protected override void Main()
		{
			this.MainPages();
			this.Backlinks();
			this.SeeAlso();

			this.Results?.Save();
		}
		#endregion

		#region Private Methods
		private void Backlinks()
		{
			var pageCollection = new PageCollection(this.Site, PageModules.CategoryInfo | PageModules.Backlinks);
			pageCollection.GetQueryPage("Wantedcategories");
			pageCollection.GetQueryPage("Wantedfiles");
			for (var i = pageCollection.Count - 1; i >= 0; i--)
			{
				var firstWord = pageCollection[i].PageName.Split(TextArrays.CategorySeparators, 2)[0];
				if (firstWord != "DB" && firstWord != "Dragonborn")
				{
					pageCollection.RemoveAt(i);
				}
			}

			pageCollection.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);
			pageCollection.GetNamespace(UespNamespaces.DragonbornTalk, Filter.Any);
			pageCollection.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "DB-");
			pageCollection.GetNamespace(MediaWikiNamespaces.Category, Filter.Any, "Dragonborn");
			this.FilterPages(pageCollection);
			pageCollection.Sort();

			this.WriteLine("\n== Pages Linked To ==");
			this.WriteLine("Pages here have incoming links that should be adjusted to point to the correct Skyrim page instead.");
			foreach (var page in pageCollection)
			{
				var text = this.GetTextForPage(page);
				if (text.Length > 0)
				{
					this.WriteLine($"* {page.AsLink()}{text}");
				}
			}
		}

		private string GetTextForPage(Page page)
		{
			var catSize = page is CategoryPage catPage ? catPage.FullCount : 0;
			var list = new List<string>();
			var backlinks = (Dictionary<Title, BacklinksTypes>)page.Backlinks;
			foreach (var title in this.filter)
			{
				backlinks.Remove(title);
			}

			if (page.Backlinks.Count > 0)
			{
				var links = "link" + (page.Backlinks.Count == 1 ? string.Empty : "s");
				list.Add($"{page.Backlinks.Count} {links}");
			}

			if (catSize > 0)
			{
				var cats = "category member" + (catSize == 1 ? string.Empty : "s");
				list.Add($"{catSize} {cats}");
			}

			var text = string.Join(", ", list);
			return text.Length > 0 ? " (" + text + ")" : text;
		}

		private void FilterPages(PageCollection pageCollection)
		{
			foreach (var item in this.filter)
			{
				pageCollection.Remove(item);
			}
		}

		private void MainPages()
		{
			var pageCollection = new PageCollection(this.Site, PageModules.Categories | PageModules.CategoryInfo | PageModules.Backlinks | PageModules.Info | PageModules.Properties);
			pageCollection.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);
			pageCollection.GetNamespace(UespNamespaces.DragonbornTalk, Filter.Any);
			this.FilterPages(pageCollection);
			pageCollection.Sort();

			this.WriteLine("== Remaining Non-Redirect Pages ==");
			this.WriteLine("Pages here were tagged as requiring human intervention, either due to complexity of the merge or because the target was a redirect. If desired, the bot can be used to do straight-up moves to a different name, but anything more complex than that should be handled by a human.");
			foreach (var page in pageCollection)
			{
				if (!page.IsRedirect)
				{
					this.WriteLine("* " + page.AsLink());
				}
			}

			this.WriteLine("\n== Remaining Redirect Pages ==");
			this.WriteLine("Pages here were redirects ''before'' any page moves occurred. They should be checked against the Skyrim page of the same name to figure out what to do with them.");
			foreach (var page in pageCollection)
			{
				var categories = new TitleCollection(this.Site);
				categories.AddRange(page.Categories);
				if (page.IsRedirect && !categories.Contains("Category:Redirects from Moves"))
				{
					var newTitle = new TitleParts(page)
					{
						NamespaceId = page.NamespaceId == UespNamespaces.Dragonborn ? UespNamespaces.Skyrim : UespNamespaces.SkyrimTalk
					};
					var pageInfo = this.GetTextForPage(page);
					if (pageInfo.Length > 0)
					{
						this.WriteLine($"* {page.PageName}: {{{{Pl|{page.FullPageName}|{page.Namespace.Name}|3=redirect=no}}}}{pageInfo} / {{{{Pl|{newTitle.FullPageName}|{newTitle.Namespace.Name}|3=redirect=no}}}}");
					}
				}
			}
		}

		private void SeeAlso()
		{
			var pageCollection = new PageCollection(this.Site, PageModules.CategoryInfo);
			pageCollection.GetTitles("Category:DBMerge-Merged", "Category:DBMerge-Redirects");

			var doSeeAlso = false;
			foreach (var page in pageCollection)
			{
				var catPage = (CategoryPage)page;
				if (catPage.FullCount > 0)
				{
					doSeeAlso = true;
					break;
				}
			}

			if (doSeeAlso)
			{
				this.WriteLine("\n== See Also ==");
				foreach (var page in pageCollection)
				{
					var catPage = (CategoryPage)page;
					if (catPage.FullCount > 0)
					{
						this.WriteLine($"* {page.AsLink()}{this.GetTextForPage(page)}");
					}
				}
			}
		}
		#endregion
	}
}