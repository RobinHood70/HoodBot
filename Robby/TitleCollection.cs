namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>A collection of Title objects.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Class coupling is a factor of using classes to handle complex inputs and is unavoidable.")]
	public class TitleCollection : TitleCollection<Title>, IEnumerable<Title>, IMessageSource
	{
		#region Fields
		private IEnumerable<int> loadPageLimitations = null;
		private LimitationType loadPageLimitationType = LimitationType.None;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class.</summary>
		/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
		public TitleCollection(Site site)
			: base(site)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles.</summary>
		/// <param name="site">The site.</param>
		/// <param name="titles">The titles.</param>
		public TitleCollection(Site site, IEnumerable<string> titles)
			: base(site)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var item in titles)
			{
				var newTitle = new Title(site, item);
				this.Add(newTitle);
			}
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles.</summary>
		/// <param name="site">The site.</param>
		/// <param name="titles">The titles.</param>
		public TitleCollection(Site site, params string[] titles)
			: this(site, titles as IEnumerable<string>)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles in a given namespace.</summary>
		/// <param name="ns">The namespace the titles are in.</param>
		/// <param name="titles">The titles. Namespace text is optional and will be stripped if provided.</param>
		public TitleCollection(Namespace ns, IEnumerable<string> titles)
			: base(ns?.Site)
		{
			ThrowNull(ns, nameof(ns));
			ThrowNull(titles, nameof(titles));
			foreach (var item in titles)
			{
				this.Add(Title.ForcedNamespace(ns, item));
			}
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles in a given namespace.</summary>
		/// <param name="ns">The namespace the titles are in.</param>
		/// <param name="titles">The titles. Namespace text is optional and will be stripped if provided.</param>
		public TitleCollection(Namespace ns, params string[] titles)
			: this(ns, titles as IEnumerable<string>)
		{
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleCollection" /> class from individual Title items.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection. Note that this creates all new Titles based on the original objects' namespace, page name, and key.</returns>
		public static TitleCollection CopyFrom(params Title[] titles) => CopyFrom(titles as IEnumerable<Title>);

		/// <summary>Initializes a new instance of the <see cref="TitleCollection" /> class from another Title collection.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection. Note that this creates all new Titles based on the original objects' namespace, page name, and key.</returns>
		public static TitleCollection CopyFrom(IEnumerable<Title> titles)
		{
			ThrowNull(titles, nameof(titles));
			var first = titles.FirstOrDefault();
			if (first == null)
			{
				throw new InvalidOperationException("Source collection is empty - TitleCollection could not be initialized.");
			}

			var site = first.Site;
			var output = new TitleCollection(site);
			foreach (var title in titles)
			{
				var newTitle = new Title(title);
				output.Add(newTitle);
			}

			return output;
		}
		#endregion

		#region Public Methods

		/// <summary>Converts all MediaWiki messages to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		public void GetMessages(Filter modifiedMessages) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages });

		/// <summary>Converts specific MediaWiki messages to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="messages">The messages to load.</param>
		public void GetMessages(Filter modifiedMessages, IEnumerable<string> messages) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages, Messages = messages });

		/// <summary>Converts MediaWiki messages beginning with the specified prefix to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="prefix">The prefix of the categories to load.</param>
		public void GetMessages(Filter modifiedMessages, string prefix) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages, Prefix = prefix });

		/// <summary>Converts MediaWiki messages within the given range to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="from">The message to start at (inclusive). The message specified does not have to exist.</param>
		/// <param name="to">The message to stop at (inclusive). The message specified does not have to exist.</param>
		public void GetMessages(Filter modifiedMessages, string from, string to) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages, MessageFrom = from, MessageTo = to });

		/// <summary>Adds all protected titles to the collection.</summary>
		public void GetProtectedTitles() => this.GetProtectedTitles(new ProtectedTitlesInput());

		/// <summary>Adds all protected titles in the given namespaces to the collection.</summary>
		/// <param name="namespaces">The namespaces to load from.</param>
		public void GetProtectedTitles(IEnumerable<int> namespaces) => this.GetProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces });

		/// <summary>Adds all protected titles of the specified levels to the collection.</summary>
		/// <param name="levels">The levels of titles to load (typically, one of: "autoconfirmed" or "sysop").</param>
		public void GetProtectedTitles(IEnumerable<string> levels) => this.GetProtectedTitles(new ProtectedTitlesInput() { Levels = levels });

		/// <summary>Adds all protected titles of the specified levels in the given namespaces to the collection.</summary>
		/// <param name="namespaces">The namespaces to load from.</param>
		/// <param name="levels">The levels of titles to load (typically, one of: "autoconfirmed" or "sysop").</param>
		public void GetProtectedTitles(IEnumerable<int> namespaces, IEnumerable<string> levels) => this.GetProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces, Levels = levels });

		/// <summary>Loads all pages in the collection.</summary>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load() => this.Load(this.Site.DefaultLoadOptions);

		/// <summary>Loads the specified information for all pages in the collection.</summary>
		/// <param name="modules">The page modules to load, using their default options.</param>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load(PageModules modules) => this.Load(new PageLoadOptions(modules));

		/// <summary>Loads the specified information for all pages in the collection.</summary>
		/// <param name="options">The page load options.</param>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load(PageLoadOptions options) => this.Load(options, this.Site.PageCreator);

		/// <summary>Loads the specified information for all pages in the collection using the specified PageCreator.</summary>
		/// <param name="options">The page load options.</param>
		/// <param name="pageCreator">The page creator to use for this job only.</param>
		/// <returns>A <see cref="PageCollection" /> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load(PageLoadOptions options, PageCreator pageCreator)
		{
			var retval = new PageCollection(this.Site, options, pageCreator);
			if (this.loadPageLimitations != null)
			{
				retval.SetLimitations(this.loadPageLimitations, this.loadPageLimitationType);
			}

			retval.AddTitles(this);

			return retval;
		}

		/// <summary>Purges all pages in the collection.</summary>
		/// <returns>A value indicating the change status of the purge along with a page collection with the purge results.</returns>
		public ChangeValue<PageCollection> Purge() => this.Purge(PurgeMethod.Normal);

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="method">The method.</param>
		/// <returns>A value indicating the change status of the purge along with a page collection with the purge results.</returns>
		public ChangeValue<PageCollection> Purge(PurgeMethod method) => this.Site.PublishChange(
			this,
			new Dictionary<string, object> { [nameof(method)] = method },
			() =>
			{
				var pages = this.Purge(new PurgeInput(this.ToFullPageNames()) { Method = method });
				return new ChangeValue<PageCollection>((pages.Count < this.Count) ? ChangeStatus.Failure : ChangeStatus.Success, pages);
			},
			PageCollection.UnlimitedDefault(this.Site));

		/// <summary>Sets namespace limitations for the Load() methods.</summary>
		/// <param name="namespaceLimitations">The namespace limitations to apply to the PageCollection returned.</param>
		/// <param name="limitationType">Type of the limitation.</param>
		/// <remarks>Currently, this applies only to the <see cref="PageCollection"/> returned by the various <see cref="Load()"/> methods. The <see cref="Purge()"/>, <see cref="Watch()"/>, and <see cref="Unwatch()"/> methods always return unfiltered results.</remarks>
		public void SetNamespaceLimitations(IEnumerable<int> namespaceLimitations, LimitationType limitationType)
		{
			this.loadPageLimitations = namespaceLimitations;
			this.loadPageLimitationType = limitationType;
		}

		/// <summary>Watches all pages in the collection.</summary>
		/// <returns>A value indicating the change status of the watch along with a page collection with the watch results.</returns>
		public ChangeValue<PageCollection> Watch() => this.Site.PublishChange(
			this,
			new Dictionary<string, object>(),
			() =>
			{
				var pages = this.Watch(new WatchInput(this.ToFullPageNames()) { Unwatch = false });
				return new ChangeValue<PageCollection>((pages.Count < this.Count) ? ChangeStatus.Failure : ChangeStatus.Success, pages);
			},
			PageCollection.UnlimitedDefault(this.Site, this));

		/// <summary>Unwatches all pages in the collection.</summary>
		/// <returns>A value indicating the change status of the unwatch along with a page collection with the unwatch results.</returns>
		public ChangeValue<PageCollection> Unwatch() => this.Site.PublishChange(
			this,
			new Dictionary<string, object>(),
			() =>
			{
				var pages = this.Watch(new WatchInput(this.ToFullPageNames()) { Unwatch = true });
				return new ChangeValue<PageCollection>((pages.Count < this.Count) ? ChangeStatus.Failure : ChangeStatus.Success, pages);
			},
			PageCollection.UnlimitedDefault(this.Site, this));
		#endregion

		#region Public Override Methods

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles to add.</param>
		public override void Add(IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				this.Add(new Title(this.Site, title));
			}
		}

		/// <summary>Adds the specified titles to the collection, assuming that they are in the provided namespace if no other namespace is specified.</summary>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="titles">The titles to add, with or without the leading namespace text.</param>
		public override void Add(int defaultNamespace, IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				this.Add(new Title(this.Site.Namespaces[defaultNamespace], title));
			}
		}

		/// <summary>Adds new objects to the collection based on an existing <see cref="ISimpleTitle"/> collection.</summary>
		/// <param name="titles">The titles to be added.</param>
		/// <remarks>All items added are newly created, even if the type of the titles provided matches those in the collection.</remarks>
		public override void AddFrom(IEnumerable<ISimpleTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(new Title(title));
				}
			}
		}

		/// <summary>Adds pages to the collection from their revision IDs.</summary>
		/// <param name="revisionIds">The revision IDs.</param>
		public override void GetRevisionIds(IEnumerable<long> revisionIds) => this.LoadPages(QueryPageSetInput.FromRevisionIds(revisionIds));
		#endregion

		#region Protected Override Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetBacklinks(BacklinksInput input)
		{
			var result = this.Site.AbstractionLayer.Backlinks(input);
			foreach (var item in result)
			{
				var mainTitle = new Title(this.Site, item.Title);
				this.Add(mainTitle);
				if (item.Redirects != null)
				{
					foreach (var redirectedItem in item.Redirects)
					{
						this.Add(new BacklinkTitle(this.Site, redirectedItem.Title, mainTitle));
					}
				}
			}
		}

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetCategories(AllCategoriesInput input)
		{
			var result = this.Site.AbstractionLayer.AllCategories(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <c>true</c> load the entire category tree recursively.</param>
		protected override void GetCategoryMembers(CategoryMembersInput input, bool recurse)
		{
			ThrowNull(input, nameof(input));
			if (recurse)
			{
				this.FillFromTitleItems(this.Site.AbstractionLayer.CategoryMembers(input));
			}
			else
			{
				this.FillFromTitleItems(input, new HashSet<ISimpleTitle>());
			}
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected override void GetDuplicateFiles(DuplicateFilesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFiles(AllImagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFileUsage(AllFileUsagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void GetFileUsage(FileUsageInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetLinksToNamespace(AllLinksInput input)
		{
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetNamespace(AllPagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllPages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageCategories(CategoriesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageLinks(LinksInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that link to the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void GetPageLinksHere(LinksHereInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPagesWithProperty(PagesWithPropertyInput input)
		{
			var result = this.Site.AbstractionLayer.PagesWithProperty(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTranscludedIn(TranscludedInInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTransclusions(TemplatesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPrefixSearchResults(PrefixSearchInput input)
		{
			var result = this.Site.AbstractionLayer.PrefixSearch(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected override void GetQueryPage(QueryPageInput input)
		{
			var result = this.Site.AbstractionLayer.QueryPage(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRecentChanges(RecentChangesInput input)
		{
			var result = this.Site.AbstractionLayer.RecentChanges(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRedirectsToNamespace(AllRedirectsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRevisions(AllRevisionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRevisions(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetSearchResults(SearchInput input)
		{
			var result = this.Site.AbstractionLayer.Search(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetTransclusions(AllTransclusionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistChanged(WatchlistInput input)
		{
			var result = this.Site.AbstractionLayer.Watchlist(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistRaw(WatchlistRawInput input)
		{
			var result = this.Site.AbstractionLayer.WatchlistRaw(input);
			this.FillFromTitleItems(result);
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Converts MediaWiki messages to titles and adds them to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void GetMessages(AllMessagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllMessages(input);
			foreach (var item in result)
			{
				var name = item.Name.Replace('_', ' ');
				name = this.Site.Namespaces[MediaWikiNamespaces.MediaWiki].CapitalizePageName(name);
				this.Add(new Title(this.Site.Namespaces[MediaWikiNamespaces.MediaWiki], name));
			}
		}

		/// <summary>Adds creation-protected titles (pages that are protected but don't exist) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void GetProtectedTitles(ProtectedTitlesInput input)
		{
			var result = this.Site.AbstractionLayer.ProtectedTitles(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Loads pages from the wiki based on a page set specifier.</summary>
		/// <param name="pageSetInput">The pageset inputs.</param>
		protected virtual void LoadPages(QueryPageSetInput pageSetInput)
		{
			ThrowNull(pageSetInput, nameof(pageSetInput));
			var loadOptions = new PageLoadOptions(this.Site.DefaultLoadOptions, PageModules.Info);
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, PageCreator.Default.GetPropertyInputs(loadOptions), PageCreator.Default.CreatePageItem);
			foreach (var item in result)
			{
				this.Add(new Title(this.Site, item.Value.Title));
			}
		}

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="input">The input.</param>
		/// <returns>A <see cref="PageCollection"/> with the purge results.</returns>
		protected virtual PageCollection Purge(PurgeInput input)
		{
			var result = this.Site.AbstractionLayer.Purge(input);
			var retval = PageCollection.UnlimitedDefault(this.Site);
			retval.PopulateMapCollections(result);
			foreach (var item in result)
			{
				var purgePage = item.Value;
				var flags = purgePage.Flags;
				var page = retval.AddNewPage(purgePage.Title);
				page.PopulateFlags(flags.HasFlag(PurgeFlags.Invalid), flags.HasFlag(PurgeFlags.Missing));
			}

			return retval;
		}

		/// <summary>Watches or unwatches all pages in the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A <see cref="PageCollection"/> with the watch/unwatch results.</returns>
		protected virtual PageCollection Watch(WatchInput input)
		{
			var pages = PageCollection.UnlimitedDefault(this.Site);
			var result = this.Site.AbstractionLayer.Watch(input);
			pages.PopulateMapCollections(result);
			foreach (var item in result)
			{
				var watchPage = item.Value;
				var flags = watchPage.Flags;
				var page = pages.AddNewPage(watchPage.Title);
				page.PopulateFlags(false, flags.HasFlag(WatchFlags.Missing));
			}

			return pages;
		}
		#endregion

		#region Private Methods
		private void FillFromTitleItems(IEnumerable<ITitleOnly> result)
		{
			foreach (var item in result)
			{
				this.Add(new Title(this.Site, item.Title));
			}
		}

		private void FillFromTitleItems(CategoryMembersInput input, HashSet<ISimpleTitle> categoryTree)
		{
			if (!categoryTree.Add(new Title(this.Site, input.Title)))
			{
				return;
			}

			var newInput = new CategoryMembersInput(input);
			newInput.Properties |= CategoryMembersProperties.Title | CategoryMembersProperties.Type;
			newInput.Type = newInput.Type | CategoryMemberTypes.Subcat;
			var result = this.Site.AbstractionLayer.CategoryMembers(newInput);
			foreach (var item in result)
			{
				var title = new Title(this.Site, item.Title);
				if (input.Type.HasFlag(item.Type))
				{
					this.Add(title);
				}

				if (item.Type == CategoryMemberTypes.Subcat)
				{
					var recurseInput = new CategoryMembersInput(item.Title)
					{
						Properties = newInput.Properties,
						Type = newInput.Type,
					};
					this.FillFromTitleItems(recurseInput, categoryTree);
				}
			}
		}
		#endregion
	}
}