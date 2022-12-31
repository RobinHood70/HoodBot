namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>Represents a collection of pages, with methods to request additional pages from the site.</summary>
	/// <remarks>Generally speaking, a PageCollection represents data that's returned from the site, although there's nothing preventing you from creating a PageCollection to store your own newly created pages, either. In most such cases, however, it's better to create and save one page at a time than to store the entire set in memory.</remarks>
	/// <seealso cref="TitleCollection{TTitle}" />
	public class PageCollection : TitleCollection<Page>
	{
		#region Fields
		private readonly PageCreator pageCreator;
		private readonly List<string> recurseCategories = new();
		private readonly Dictionary<string, FullTitle> titleMap = new(StringComparer.Ordinal);
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		public PageCollection(Site site)
			: this(site, site.NotNull().DefaultLoadOptions)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="modules">The module types indicating which data to retrieve from the site. Using this constructor, all modules will be loaded using default parameters.</param>
		public PageCollection(Site site, PageModules modules)
			: this(site, new PageLoadOptions(modules))
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="modules">The module types indicating which data to retrieve from the site. Using this constructor, all modules will be loaded using default parameters.</param>
		/// <param name="followRedirects">Indicates whether redirects should be followed when loading.</param>
		public PageCollection(Site site, PageModules modules, bool followRedirects)
			: this(site, new PageLoadOptions(modules, followRedirects))
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="options">A <see cref="PageLoadOptions"/> object initialized with a set of modules. Using this constructor allows you to customize some options.</param>
		public PageCollection(Site site, PageLoadOptions options)
			: base(site)
		{
			this.LoadOptions = options ?? site.NotNull().DefaultLoadOptions;
			this.pageCreator = this.LoadOptions.PageCreator;
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class from a WallE result set and populates the title map from it.</summary>
		/// <param name="site">The site the result is from.</param>
		/// <param name="result">The result set.</param>
		/// <remarks>Note that this does <em>not</em> populate the collection itself, leaving that to the caller, since IPageSetResult does not provide enough information to do so.</remarks>
		public PageCollection(Site site, IPageSetResult result)
			: this(site)
		{
			this.PopulateMapCollections(result.NotNull());
		}
		#endregion

		#region Public Events

		/// <summary>This event occurs for each page when it is loaded.</summary>
		/// <remarks>This event does not fire if a page is merely added to the collection, or a new blank page is created with the <see cref="New"/> method.</remarks>
		public event StrongEventHandler<PageCollection, Page>? PageLoaded;

		/// <summary>This event occurs when a loaded page is missing or empty.</summary>
		/// <remarks>PageMissing always precedes <see cref="PageLoaded"/>, allowing the subscriber to initialize the page with standard text for PageLoaded to act on.</remarks>
		public event StrongEventHandler<PageCollection, Page>? PageMissing;

		#endregion

		#region Public Properties

		/// <summary>Gets the options used by <see cref="LoadPages(QueryPageSetInput)"/>.</summary>
		/// <value>The load options.</value>
		public PageLoadOptions LoadOptions { get; }

		/// <summary>Gets the title map.</summary>
		/// <value>The title map.</value>
		/// <remarks>
		/// <para>The title map allows mapping from the original name you provided for a page to the actual title that was returned. If, for example, you requested "Main Page" and got redirected to "Main page", there would be an entry in the title map indicating that. Not all titles in the title map will necessarily appear in the other set. For example, if you provided an interwiki title, the other set most likely won't include that, but the title map will still include an InterwikiTitle result for it.</para>
		/// <para>The title map is largely for informational purposes. When accessing items in the collection, it will automatically check the title map and attempt to return the correct result.</para>
		/// </remarks>
		public IReadOnlyDictionary<string, FullTitle> TitleMap => this.titleMap;
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="Page"/> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The requested <see cref="Page"/>.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the page cannot be found either by the name requested or via the substitute name in the <see cref="TitleMap"/>.</exception>
		/// <remarks>Like a <see cref="Dictionary{TKey, TValue}"/>, this indexer will add a new entry if the requested entry isn't found.</remarks>
		public override Page this[string key]
		{
			get => this.TryGetValue(key.NotNull(), out var retval) ||
				(this.titleMap.TryGetValue(key, out var altKey) && this.TryGetValue(altKey, out retval))
					? retval!
					: throw new KeyNotFoundException();

			set => base[key] = value;
		}

		/// <summary>Gets or sets the <see cref="Title"/> with the specified key.</summary>
		/// <param name="title">The title.</param>
		/// <returns>The <see cref="Title">Title</see>.</returns>
		/// <remarks>Like a <see cref="Dictionary{TKey, TValue}"/>, this indexer will add a new entry on set if the requested entry isn't found.</remarks>
		/// <exception cref="KeyNotFoundException">Thrown when the title could not be found.</exception>
		public override Page this[Title title]
		{
			get => this.TryGetValue(title.NotNull(), out var page)
				? page
				: throw new KeyNotFoundException();

			set => base[title] = value;
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new PageCollection intended to store results of other operations like Purge, Watch, or Unwatch.</summary>
		/// <param name="site">The site.</param>
		/// <param name="other">The collection to initialize this instance from.</param>
		/// <returns>A new PageCollection with no namespace limitations, load options set to none, and creating only default pages rather than user-specified.</returns>
		public static PageCollection CreateEmptyPages(Site site, IEnumerable<Title> other)
		{
			// Currently only used for Purge, Watch, and Unwatch when returning fake results.
			var retval = UnlimitedDefault(site);
			foreach (var title in other.NotNull())
			{
				var page = retval.pageCreator.CreateEmptyPage(title);
				retval[page] = page;
			}

			return retval;
		}

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="site">The site to work on.</param>
		/// <param name="input">The input.</param>
		/// <returns>A <see cref="PageCollection"/> with the purge results.</returns>
		public static PageCollection Purge(Site site, PurgeInput input)
		{
			var retval = UnlimitedDefault(site);
			if (site.NotNull().EditingEnabled)
			{
				var result = site.NotNull().AbstractionLayer.Purge(input);
				retval.PopulateMapCollections(result);
				foreach (var item in result)
				{
					var page = retval.New(item);
					retval[page] = page;
				}
			}

			return retval;
		}

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="site">The site to work on.</param>
		/// <param name="titles">The titles to purge.</param>
		/// <param name="method">The type of purge to perform.</param>
		/// <param name="batchSize">The number of purges to send with each request. Lower this value if purge returns errors.</param>
		/// <returns>A <see cref="PageCollection"/> with the purge results.</returns>
		public static PageCollection Purge(Site site, IEnumerable<Title> titles, PurgeMethod method, int batchSize)
		{
			titles.ThrowNull();
			var retval = UnlimitedDefault(site);
			var subTitles = new List<string>();
			foreach (var title in titles)
			{
				subTitles.Add(title.FullPageName);
				if (subTitles.Count >= batchSize)
				{
					var input = new PurgeInput(subTitles, method);
					retval.MergeWith(Purge(site, input));
					subTitles.Clear();
				}
			}

			return retval;
		}

		/// <summary>Initializes a new instance of the PageCollection class with no namespace limitations.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <returns>A new PageCollection with all namespace limitations disabled.</returns>
		/// <remarks>This is a simple shortcut method to create PageCollections where limitations can safely be ignored.</remarks>
		public static PageCollection Unlimited(Site site) => new(site) { LimitationType = LimitationType.None };

		/// <summary>Initializes a new instance of the PageCollection class with no namespace limitations.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="options">A <see cref="PageLoadOptions"/> object initialized with a set of modules. Using this constructor allows you to customize some options.</param>
		/// <returns>A new PageCollection with all namespace limitations disabled.</returns>
		/// <remarks>This is a simple shortcut method to create PageCollections where limitations can safely be ignored.</remarks>
		public static PageCollection Unlimited(Site site, PageLoadOptions options) => new(site, options) { LimitationType = LimitationType.None };

		/// <summary>Initializes a new instance of the PageCollection class with no namespace limitations.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="modules">The module types indicating which data to retrieve from the site. Using this constructor, all modules will be loaded using default parameters.</param>
		/// <param name="followRedirects">Whether to follow redirects.</param>
		/// <returns>A new PageCollection with all namespace limitations disabled.</returns>
		/// <remarks>This is a simple shortcut method to create PageCollections where limitations can safely be ignored.</remarks>
		public static PageCollection Unlimited(Site site, PageModules modules, bool followRedirects) => Unlimited(site, new PageLoadOptions(modules, followRedirects));

		/// <summary>Initializes a new PageCollection intended to store results of other operations like Purge, Watch, or Unwatch.</summary>
		/// <param name="site">The site.</param>
		/// <returns>A new PageCollection with no namespace limitations, load options set to none, and creating only default pages rather than user-specified.</returns>
		public static PageCollection UnlimitedDefault(Site site) => new(site, PageLoadOptions.None) { LimitationType = LimitationType.None };

		/// <summary>Watches or unwatches all pages in the collection.</summary>
		/// <param name="site">The site to work on.</param>
		/// <param name="input">The input parameters.</param>
		/// <returns>A <see cref="PageCollection"/> with the watch/unwatch results.</returns>
		public static PageCollection Watch(Site site, WatchInput input)
		{
			var result = site.NotNull().AbstractionLayer.Watch(input);
			PageCollection retval = new(site, result);
			foreach (var item in result)
			{
				var page = retval.New(item);
				retval[page] = page;
			}

			return retval;
		}
		#endregion

		#region Public Methods

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(params string[] titles) => this.GetTitles(new TitleCollection(this.Site, titles));

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(IEnumerable<string> titles) => this.GetTitles(new TitleCollection(this.Site, titles));

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(params Title[] titles) => this.GetTitles(new TitleCollection(this.Site, titles));

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(titles.ToFullPageNames()));

		/// <summary>Merges the current PageCollection with another, including all <see cref="TitleMap"/> entries.</summary>
		/// <param name="other">The PageCollection to merge with.</param>
		public void MergeWith(PageCollection other)
		{
			other.ThrowNull();
			this.AddRange(other);
			foreach (var entry in other.TitleMap)
			{
				this.titleMap.Add(entry.Key, entry.Value);
			}
		}

		/// <summary>Removes all pages from the collection where the page's <see cref="Page.Exists"/> property is false.</summary>
		/// <param name="changed"><see langword="true"/>to remove changed pages; <see langword="false"/> to remove unchanged pages.</param>
		public void RemoveChanged(bool changed)
		{
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (this[i].TextModified == changed)
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Removes all pages from the collection where the page's <see cref="Page.Exists"/> property equals the value provided.</summary>
		/// <param name="exists">If <see langword="true"/>, pages that exist will be removed from the collection; if <see langword="false"/>, non-existent pages will be removed fromt he collection.</param>
		public void RemoveExists(bool exists)
		{
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (this[i].Exists == exists)
				{
					this.RemoveAt(i);
				}
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Removes all items from the <see cref="TitleCollection">collection</see>, as well as those in the <see cref="TitleMap"/>.</summary>
		public override void Clear()
		{
			base.Clear();
			this.titleMap.Clear();
		}

		/// <inheritdoc/>
		public override void GetCustomGenerator(IGeneratorInput generatorInput) => this.LoadPages(new QueryPageSetInput(generatorInput));

		/// <summary>Adds pages with the specified revision IDs to the collection.</summary>
		/// <param name="revisionIds">The IDs.</param>
		/// <remarks>General information about the pages for the revision IDs specified will always be loaded, regardless of the LoadOptions setting, though the revisions themselves may not be if the collection's load options would filter them out.</remarks>
		// Note that while RevisionsInput() can be used as a generator, I have not implemented it because I can think of no situation in which it would be useful to populate a PageCollection given the existing revisions methods.
		public override void GetRevisionIds(IEnumerable<long> revisionIds) => this.LoadPages(QueryPageSetInput.FromRevisionIds(revisionIds));

		/// <inheritdoc/>
		public override bool TryGetValue(Page key, [MaybeNullWhen(false)] out Page value)
		{
			if (base.TryGetValue(key, out var retval) || (this.titleMap.TryGetValue(key.NotNull().FullPageName, out var altKey) && base.TryGetValue(altKey, out retval)))
			{
				value = retval;
				return true;
			}

			value = default;
			return false;
		}

		/// <inheritdoc/>
		public override bool TryGetValue(Title key, [MaybeNullWhen(false)] out Page value)
		{
			if (base.TryGetValue(key, out var retval) || (this.titleMap.TryGetValue(key.NotNull().FullPageName, out var altKey) && base.TryGetValue(altKey, out retval)))
			{
				value = retval;
				return true;
			}

			value = default;
			return false;
		}

		/// <inheritdoc/>
		public override bool TryGetValue(string key, [MaybeNullWhen(false)] out Page value)
		{
			if (base.TryGetValue(key.NotNull(), out var retval) || (this.titleMap.TryGetValue(key, out var altKey) && base.TryGetValue(altKey, out retval)))
			{
				value = retval;
				return true;
			}

			value = default;
			return false;
		}
		#endregion

		#region Internal Methods
		internal void PopulateMapCollections(IPageSetResult result)
		{
			result.ThrowNull();
			foreach (var item in result.Interwiki)
			{
				FullTitle title = TitleFactory.FromUnvalidated(this.Site, item.Value.Title);
				Debug.Assert(string.Equals(title.Interwiki?.Prefix, item.Value.Prefix, StringComparison.Ordinal), "Interwiki prefixes didn't match.", title.Interwiki?.Prefix + " != " + item.Value.Prefix);
				this.titleMap[item.Key] = title;
			}

			foreach (var item in result.Converted)
			{
				this.titleMap[item.Key] = TitleFactory.FromUnvalidated(this.Site, item.Value);
			}

			foreach (var item in result.Normalized)
			{
				this.titleMap[item.Key] = TitleFactory.FromUnvalidated(this.Site, item.Value);
			}

			foreach (var item in result.Redirects)
			{
				this.titleMap[item.Key] = TitleFactory.FromUnvalidated(this.Site, item.Value.ToString()!);
			}
		}
		#endregion

		#region Protected Override Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetBacklinks(BacklinksInput input)
		{
			input.ThrowNull();
			input.Title.PropertyThrowNull(nameof(input), nameof(input.Title));
			var inputTitle = TitleFactory.FromUnvalidated(this.Site, input.Title);
			if (inputTitle.Namespace != MediaWikiNamespaces.File && (input.LinkTypes & BacklinksTypes.ImageUsage) != 0)
			{
				input = new BacklinksInput(input, input.LinkTypes & ~BacklinksTypes.ImageUsage);
				input.Title.PropertyThrowNull(nameof(input), nameof(input.Title)); // Input changed, so re-check before proceeding.
			}

			foreach (var type in input.LinkTypes.GetUniqueFlags())
			{
				this.GetCustomGenerator(new BacklinksInput(input.Title, type)
				{
					FilterRedirects = input.FilterRedirects,
					Namespace = input.Namespace,
					Redirect = input.Redirect,
				});
			}
		}

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetCategories(AllCategoriesInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <see langword="true"/> load the entire category tree recursively.</param>
		protected override void GetCategoryMembers(CategoryMembersInput input, bool recurse)
		{
			input.ThrowNull();
			if (recurse)
			{
				this.RecurseCategoryPages(input, new HashSet<string>(StringComparer.Ordinal));
			}
			else
			{
				this.GetCustomGenerator(input);
			}
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected override void GetDuplicateFiles(DuplicateFilesInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFiles(AllImagesInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFileUsage(AllFileUsagesInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void GetFileUsage(FileUsageInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetLinksToNamespace(AllLinksInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageCategories(CategoriesInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageLinks(LinksInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose backlinks should be loaded.</param>
		protected override void GetPageLinksHere(LinksHereInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages with the specified filters to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPages(AllPagesInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTransclusions(TemplatesInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that transclude the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTranscludedIn(TranscludedInInput input, IEnumerable<Title> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPagesWithProperty(PagesWithPropertyInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPrefixSearchResults(PrefixSearchInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected override void GetQueryPage(QueryPageInput input) => this.GetCustomGenerator(input);

		/// <summary>Gets a random set of pages from the wiki.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRandomPages(RandomInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRecentChanges(RecentChangesInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRedirectsToNamespace(AllRedirectsInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRevisions(AllRevisionsInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetSearchResults(SearchInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetTransclusions(AllTransclusionsInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistChanged(WatchlistInput input) => this.GetCustomGenerator(input);

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistRaw(WatchlistRawInput input) => this.GetCustomGenerator(input);
		#endregion

		#region Protected Virtual Methods

		/// <summary>Loads pages from the wiki based on a page set specifier.</summary>
		/// <param name="pageSetInput">The page set input.</param>
		/// <param name="pageValidator">A function which validates whether a page can be added to the collection.</param>
		protected virtual void LoadPages(QueryPageSetInput pageSetInput, Func<Page, bool> pageValidator)
		{
			if (pageSetInput.NotNull().IsEmpty)
			{
				return;
			}

			pageValidator.ThrowNull();
			var options = this.LoadOptions;
			pageSetInput.ConvertTitles = options.ConvertTitles;
			pageSetInput.Redirects = options.FollowRedirects;
			if (pageSetInput.GeneratorInput is ILimitableInput limited)
			{
				if (options.PageLimit >= 50)
				{
					limited.Limit = options.PageLimit;
				}
				else if ((options.Modules & PageModules.Revisions) != 0)
				{
					// API-specific. Because of the way revisions output is handled in a pageset, setting the page limit to be the same as the revisions limit results in a much more optimal result, returning less data in more evenly sized batches. This might apply to other modules as well, but revisions is likely the biggest concern, so we always set 500 here unless a higher limit was specifically requested above.
					limited.Limit = 500;
				}
			}

			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, this.pageCreator.GetPropertyInputs(options), this.pageCreator.CreatePageItem);
			this.PopulateMapCollections(result);
			foreach (var item in result)
			{
				var page = this.New(item);
				if (pageValidator(page))
				{
					this[page] = page;
					if (page.IsMissing || string.IsNullOrWhiteSpace(page.Text))
					{
						this.PageMissing?.Invoke(this, page);
					}

					this.PageLoaded?.Invoke(this, page);
				}
			}
		}
		#endregion

		#region Private Methods
		private void LoadPages(IGeneratorInput generator, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(generator, titles.ToFullPageNames()));

		private void LoadPages(QueryPageSetInput pageSetInput) => this.LoadPages(pageSetInput, this.IsTitleInLimits);

		/// <summary>Creates a new page using the collection's <see cref="pageCreator"/> and adds it to the collection.</summary>
		/// <param name="item">The <see cref="IApiTitle"/> with all the information for the page.</param>
		/// <returns>The page that was created.</returns>
		/// <remarks>If the page title specified represents a page already in the collection, that page will be overwritten.</remarks>
		private Page New(IApiTitle item)
		{
			var pageTitle = TitleFactory.FromUnvalidated(this.Site, item.FullPageName);
			return this.pageCreator.CreatePage(pageTitle, this.LoadOptions, item);
		}

		private bool RecurseCategoryHandler(Page page)
		{
			if (page.Namespace == MediaWikiNamespaces.Category)
			{
				this.recurseCategories.Add(page.FullPageName);
			}

			return this.IsTitleInLimits(page);
		}

		/// <summary>Loads category pages recursively.</summary>
		/// <param name="input">The input.</param>
		/// <param name="categoryTree">A hashet used to track which categories have already been loaded. This avoids loading the same category if it appears in the tree more than once, and breaks possible recursion loops.</param>
		private void RecurseCategoryPages(CategoryMembersInput input, HashSet<string> categoryTree)
		{
			input.Title.PropertyThrowNull(nameof(input), nameof(input.Title));
			if (!categoryTree.Add(input.Title))
			{
				return;
			}

			this.recurseCategories.Clear();
			this.LoadPages(new QueryPageSetInput(input), this.RecurseCategoryHandler);
			if (this.recurseCategories.Count > 0)
			{
				List<string> copy = new(this.recurseCategories);
				this.recurseCategories.Clear();
				var originalTitle = input.Title;
				foreach (var category in copy)
				{
					input.ChangeTitle(category);
					this.RecurseCategoryPages(input, categoryTree);
				}

				input.ChangeTitle(originalTitle);
			}
		}
		#endregion
	}
}