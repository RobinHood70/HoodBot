namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using WikiCommon;
	using static WikiCommon.Globals;

	/// <summary>A collection of Title objects.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Class coupling is a factor of using classes to handle complex inputs and is unavoidable.")]
	public class TitleCollection : TitleCollection<Title>, IEnumerable<Title>, IMessageSource
	{
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
		/// <param name="site">The site.</param>
		/// <param name="ns">The namespace the titles are in.</param>
		/// <param name="titles">The titles. Namespace text is optional and will be stripped if provided.</param>
		public TitleCollection(Site site, int ns, IEnumerable<string> titles)
			: base(site)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var item in titles)
			{
				this.Add(Title.ForcedNamespace(site, ns, item));
			}
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles in a given namespace.</summary>
		/// <param name="site">The site.</param>
		/// <param name="ns">The namespace the titles are in.</param>
		/// <param name="titles">The titles. Namespace text is optional and will be stripped if provided.</param>
		public TitleCollection(Site site, int ns, params string[] titles)
			: this(site, ns, titles as IEnumerable<string>)
		{
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleCollection" /> class from individual Title items.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection.</returns>
		public static TitleCollection CopyFrom(params Title[] titles) => CopyFrom(titles as IEnumerable<Title>);

		/// <summary>Initializes a new instance of the <see cref="TitleCollection" /> class from another Title collection.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection.</returns>
		public static TitleCollection CopyFrom(IEnumerable<Title> titles)
		{
			ThrowNull(titles, nameof(titles));
			Site site = null;
			foreach (var title in titles)
			{
				site = site ?? title.Site;
				break;
			}

			if (site == null)
			{
				throw new InvalidOperationException("Source collection is empty - TitleCollection could not be initialized.");
			}

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

		/// <summary>Adds a <em>copy</em> of the provided titles to the collection. The copies added will be standard <see cref="Title"/> objects regardless of the original type.</summary>
		/// <param name="titles">The titles to add.</param>
		public void AddCopy(IEnumerable<IWikiTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(new Title(title));
				}
			}
		}

		/// <summary>Converts all MediaWiki messages to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		public void AddMessages(Filter modifiedMessages) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages });

		/// <summary>Converts specific MediaWiki messages to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="messages">The messages to load.</param>
		public void AddMessages(Filter modifiedMessages, IEnumerable<string> messages) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages, Messages = messages });

		/// <summary>Converts MediaWiki messages beginning with the specified prefix to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="prefix">The prefix of the categories to load.</param>
		public void AddMessages(Filter modifiedMessages, string prefix) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages, Prefix = prefix });

		/// <summary>Converts MediaWiki messages within the given range to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="from">The message to start at (inclusive). The message specified does not have to exist.</param>
		/// <param name="to">The message to stop at (inclusive). The message specified does not have to exist.</param>
		public void AddMessages(Filter modifiedMessages, string from, string to) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages, MessageFrom = from, MessageTo = to });

		/// <summary>Adds all protected titles to the collection.</summary>
		public void AddProtectedTitles() => this.AddProtectedTitles(new ProtectedTitlesInput());

		/// <summary>Adds all protected titles in the given namespaces to the collection.</summary>
		/// <param name="namespaces">The namespaces to load from.</param>
		public void AddProtectedTitles(IEnumerable<int> namespaces) => this.AddProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces });

		/// <summary>Adds all protected titles of the specified levels to the collection.</summary>
		/// <param name="levels">The levels of titles to load (typically, one of: "autoconfirmed" or "sysop").</param>
		public void AddProtectedTitles(IEnumerable<string> levels) => this.AddProtectedTitles(new ProtectedTitlesInput() { Levels = levels });

		/// <summary>Adds all protected titles of the specified levels in the given namespaces to the collection.</summary>
		/// <param name="namespaces">The namespaces to load from.</param>
		/// <param name="levels">The levels of titles to load (typically, one of: "autoconfirmed" or "sysop").</param>
		public void AddProtectedTitles(IEnumerable<int> namespaces, IEnumerable<string> levels) => this.AddProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces, Levels = levels });

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
		public PageCollection Load(PageLoadOptions options)
		{
			var retval = new PageCollection(this.Site, options);
			retval.AddTitles(this);
			return retval;
		}

		/// <summary>Purges all pages in the collection.</summary>
		/// <returns>A page collection with the results of the purge.</returns>
		public PageCollection Purge() => this.Purge(PurgeMethod.Normal);

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="method">The method.</param>
		/// <returns>A page collection with the results of the purge.</returns>
		public PageCollection Purge(PurgeMethod method) => this.Purge(new PurgeInput(this.ToFullPageNames()) { Method = method });

		/// <summary>Watches all pages in the collection.</summary>
		/// <returns>A page collection with the watch results.</returns>
		public PageCollection Watch() => this.Watch(new WatchInput(this.ToFullPageNames()) { Unwatch = false });

		/// <summary>Unwatches all pages in the collection.</summary>
		/// <returns>A page collection with the unwatch results.</returns>
		public PageCollection Unwatch() => this.Watch(new WatchInput(this.ToFullPageNames()) { Unwatch = true });
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

		/// <summary>Adds pages to the collection from their revision IDs.</summary>
		/// <param name="revisionIds">The revision IDs.</param>
		public override void AddRevisionIds(IEnumerable<long> revisionIds) => this.LoadPages(DefaultPageSetInput.FromRevisionIds(revisionIds));
		#endregion

		#region Protected Override Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddBacklinks(BacklinksInput input)
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
		protected override void AddCategories(AllCategoriesInput input)
		{
			var result = this.Site.AbstractionLayer.AllCategories(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <c>true</c> load the entire category tree recursively.</param>
		protected override void AddCategoryMembers(CategoryMembersInput input, bool recurse)
		{
			ThrowNull(input, nameof(input));
			if (recurse)
			{
				this.FillFromTitleItems(this.Site.AbstractionLayer.CategoryMembers(input));
			}
			else
			{
				this.FillFromTitleItems(input, new HashSet<IWikiTitle>());
			}
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected override void AddDuplicateFiles(DuplicateFilesInput input, IEnumerable<IWikiTitle> titles) => this.LoadPages(new DefaultPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddFiles(AllImagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddFileUsage(AllFileUsagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void AddFileUsage(FileUsageInput input, IEnumerable<IWikiTitle> titles) => this.LoadPages(new DefaultPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddLinksToNamespace(AllLinksInput input)
		{
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddNamespace(AllPagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllPages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void AddPageCategories(CategoriesInput input, IEnumerable<IWikiTitle> titles) => this.LoadPages(new DefaultPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void AddPageLinks(LinksInput input, IEnumerable<IWikiTitle> titles) => this.LoadPages(new DefaultPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddPagesWithProperty(PagesWithPropertyInput input)
		{
			var result = this.Site.AbstractionLayer.PagesWithProperty(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void AddPageTransclusions(TemplatesInput input, IEnumerable<IWikiTitle> titles) => this.LoadPages(new DefaultPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddPrefixSearchResults(PrefixSearchInput input)
		{
			var result = this.Site.AbstractionLayer.PrefixSearch(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected override void AddQueryPage(QueryPageInput input)
		{
			var result = this.Site.AbstractionLayer.QueryPage(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddRecentChanges(RecentChangesInput input)
		{
			var result = this.Site.AbstractionLayer.RecentChanges(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddRedirectsToNamespace(AllRedirectsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddRevisions(AllRevisionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRevisions(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddSearchResults(SearchInput input)
		{
			var result = this.Site.AbstractionLayer.Search(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddTransclusions(AllTransclusionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddWatchlistChanged(WatchlistInput input)
		{
			var result = this.Site.AbstractionLayer.Watchlist(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddWatchlistRaw(WatchlistRawInput input)
		{
			var result = this.Site.AbstractionLayer.WatchlistRaw(input);
			this.FillFromTitleItems(result);
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Loads pages from the wiki based on a page set specifier.</summary>
		/// <param name="pageSetInput">The pageset inputs.</param>
		protected virtual void LoadPages(DefaultPageSetInput pageSetInput)
		{
			ThrowNull(pageSetInput, nameof(pageSetInput));
			var loadOptions = new PageLoadOptions(this.Site.DefaultLoadOptions, PageModules.Info);
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, PageCreator.Default.GetPropertyInputs(loadOptions), PageCreator.Default.CreatePageItem);
			foreach (var item in result)
			{
				this.Add(new Title(this.Site, item.Value.Title));
			}
		}

		/// <summary>Converts MediaWiki messages to titles and adds them to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void AddMessages(AllMessagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllMessages(input);
			foreach (var item in result)
			{
				var name = item.Name.Replace('_', ' ');
				name = this.Site.Namespaces[MediaWikiNamespaces.MediaWiki].CapitalizePageName(name);
				this.Add(new Title(this.Site, MediaWikiNamespaces.MediaWiki, name));
			}
		}

		/// <summary>Adds creation-protected titles (pages that are protected but don't exist) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void AddProtectedTitles(ProtectedTitlesInput input)
		{
			var result = this.Site.AbstractionLayer.ProtectedTitles(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="input">The input.</param>
		/// <returns>A page collection with the results of the purge.</returns>
		protected virtual PageCollection Purge(PurgeInput input)
		{
			var result = this.Site.AbstractionLayer.Purge(input);
			var retval = new PageCollection(this.Site, result);
			foreach (var item in result)
			{
				var purgePage = item.Value;
				var flags = purgePage.Flags;
				var page = this.Site.PageCreator.CreatePage(new TitleParts(this.Site, purgePage.Title));
				page.PopulateFlags(flags.HasFlag(PurgeFlags.Invalid), flags.HasFlag(PurgeFlags.Missing));

				retval.Add(page);
			}

			return retval;
		}

		/// <summary>Watches or unwatches all pages in the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A page collection with the watch/unwatch results.</returns>
		protected virtual PageCollection Watch(WatchInput input)
		{
			PageCollection retval;
			if (!this.Site.AllowEditing)
			{
				this.Site.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(input)] = input,
				});

				retval = new PageCollection(this.Site);
				foreach (var item in this)
				{
					retval.Add(PageCreator.Default.CreatePage(new TitleParts(this.Site, item.FullPageName)));
				}

				return retval;
			}

			var result = this.Site.AbstractionLayer.Watch(input);
			retval = new PageCollection(this.Site, result);
			foreach (var item in result)
			{
				var watchPage = item.Value;
				var flags = watchPage.Flags;
				var page = PageCreator.Default.CreatePage(new TitleParts(this.Site, watchPage.Title));
				page.PopulateFlags(false, flags.HasFlag(WatchFlags.Missing));

				retval.Add(page);
			}

			return retval;
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

		private void FillFromTitleItems(CategoryMembersInput input, HashSet<IWikiTitle> categoryTree)
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