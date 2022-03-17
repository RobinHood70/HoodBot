namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class DBMergeCheck : EditJob
	{
		#region Constants
		private const string MergeResultsPage = "Project:Dragonborn Merge Project/Merge Results";
		#endregion

		#region Fields
		private readonly TitleCollection filter;
		#endregion

		#region Constructors
		[JobInfo(/*"Final Check", "Dragonborn Merge"*/ "Dragonborn Final Check")]
		public DBMergeCheck(JobManager jobManager)
			: base(jobManager)
		{
			this.Logger = null;
			this.Results = new PageResultHandler(this.Site, MergeResultsPage);

			this.filter = new TitleCollection(this.Site)
			{
				MergeResultsPage,
				"User:HoodBot/Results",
				"User:Kiz/Sandbox1",
				"Project:Dragonborn Merge Project",
			};
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging() => this.filter.AddRange(this.LoadProposedDeletions());

		protected override void Main()
		{
			var pages = this.MainPages();
			this.RemainingNonRedirects(pages);
			this.RemainingRedirects(pages);
			this.Backlinks(pages);
			this.SeeAlso();

			this.Results?.Save();
		}
		#endregion

		#region Private Methods
		private void Backlinks(PageCollection ignore)
		{
			PageCollection pageCollection = new(this.Site, PageModules.CategoryInfo | PageModules.Backlinks);
			pageCollection.GetQueryPage("Wantedcategories");
			pageCollection.GetQueryPage("Wantedfiles");
			for (var i = pageCollection.Count - 1; i >= 0; i--)
			{
				var firstWord = pageCollection[i].PageName.Split(TextArrays.CategorySeparators, 2)[0];
				if (!string.Equals(firstWord, "DB", System.StringComparison.Ordinal) && !string.Equals(firstWord, "Dragonborn", System.StringComparison.Ordinal))
				{
					pageCollection.RemoveAt(i);
				}
			}

			pageCollection.GetNamespace(UespNamespaces.Dragonborn);
			pageCollection.GetNamespace(UespNamespaces.DragonbornTalk);
			pageCollection.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "DB-");
			pageCollection.GetCategories("Dragonborn");
			this.FilterPages(pageCollection);
			foreach (var page in ignore)
			{
				pageCollection.Remove(page);
			}

			pageCollection.Sort();

			this.WriteLine("\n== Pages Linked To ==");
			this.WriteLine("Pages here have incoming links that should be adjusted to point to the correct Skyrim page instead (excluding those reported in sections above).");
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
			List<string> list = new();
			Dictionary<Title, BacklinksTypes> backlinks = (Dictionary<Title, BacklinksTypes>)page.Backlinks;
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
				TitleCollection catMembers = new(this.Site);
				catMembers.GetCategoryMembers(page.FullPageName, CategoryMemberTypes.All, false);
				catMembers.Remove(page);
				//// catMembers.Remove("Skyrim:Dragonborn");

				if (catMembers.Count > 0)
				{
					var cats = "category member" + (catSize == 1 ? string.Empty : "s");
					list.Add($"{catSize.ToStringInvariant()} {cats}");
				}
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

		private PageCollection MainPages()
		{
			PageCollection pageCollection = new(this.Site, PageModules.Categories | PageModules.CategoryInfo | PageModules.Backlinks | PageModules.Info | PageModules.Properties);
			pageCollection.GetNamespace(UespNamespaces.Dragonborn);
			pageCollection.GetNamespace(UespNamespaces.DragonbornTalk);
			this.FilterPages(pageCollection);
			pageCollection.Sort();

			return pageCollection;
		}

		private void RemainingNonRedirects(PageCollection pageCollection)
		{
			this.WriteLine("== Remaining Non-Redirect Pages ==");
			this.WriteLine("Pages here were tagged as requiring human intervention, either due to complexity of the merge or because the target was a redirect. If desired, the bot can be used to do straight-up moves to a different name, but anything more complex than that should be handled by a human.");
			foreach (var page in pageCollection)
			{
				if (!page.IsRedirect)
				{
					this.WriteLine("* " + page.AsLink());
				}
			}
		}

		private void RemainingRedirects(PageCollection pageCollection)
		{
			Dictionary<Page, string> redirList = new();
			foreach (var page in pageCollection)
			{
				TitleCollection categories = new(this.Site, page.Categories);
				if (page.IsRedirect && !categories.Contains("Category:Redirects from Moves"))
				{
					var pageInfo = this.GetTextForPage(page);
					if (pageInfo.Length > 0)
					{
						redirList.Add(page, pageInfo);
					}
				}
			}

			if (redirList.Count > 0)
			{
				this.WriteLine("\n== Remaining Redirect Pages ==");
				this.WriteLine("Pages here were redirects ''before'' any page moves occurred. They should be checked against the Skyrim page of the same name to figure out what to do with them.");
				foreach (var (page, pageInfo) in redirList)
				{
					var newNs = page.Namespace == UespNamespaces.Dragonborn ? UespNamespaces.Skyrim : UespNamespaces.SkyrimTalk;
					var newTitle = CreateTitle.FromValidated(page.Site, newNs, page.PageName);
					this.WriteLine($"* {page.PageName}: {{{{Pl|{page.FullPageName}|{page.Namespace.Name}|3=redirect=no}}}}{pageInfo} / {{{{Pl|{newTitle.FullPageName}|{newTitle.Namespace.Name}|3=redirect=no}}}}");
				}
			}
		}

		private void SeeAlso()
		{
			PageCollection pageCollection = new(this.Site, PageModules.CategoryInfo);
			pageCollection.GetTitles("Category:DBMerge-Merged", "Category:DBMerge-Redirects");

			var doSeeAlso = false;
			foreach (var page in pageCollection)
			{
				CategoryPage catPage = (CategoryPage)page;
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
					CategoryPage catPage = (CategoryPage)page;
					if (catPage.FullCount > 0)
					{
						this.WriteLine($"* {page}{this.GetTextForPage(page)}");
					}
				}
			}
		}
		#endregion
	}
}