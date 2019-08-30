namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations

	/// <summary>Specifies how to limit any pages added to the collection.</summary>
	public enum LimitationType
	{
		/// <summary>Ignore all limitations.</summary>
		None,

		/// <summary>Automatically remove pages with namespaces specified in <see cref="PageCollection.NamespaceLimitations"/> from the collection.</summary>
		Remove,

		/// <summary>Automatically limit pages in the collection to those with namespaces specified in <see cref="PageCollection.NamespaceLimitations"/>.</summary>
		FilterTo,
	}
	#endregion

	/// <summary>Represents a collection of pages, with methods to request additional pages from the site.</summary>
	/// <remarks>Generally speaking, a PageCollection represents data that's returned from the site, although there's nothing preventing you from creating a PageCollection to store your own newly created pages, either. In most such cases, however, it's better to create and save one page at a time than to store the entire set in memory.</remarks>
	/// <seealso cref="TitleCollection{TTitle}" />
	public class PageCollection : TitleCollection<Page>
	{
		#region Fields
		private readonly Dictionary<string, TitleParts> titleMap = new Dictionary<string, TitleParts>();
		private readonly List<string> recurseCategories = new List<string>();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		public PageCollection(Site site)
			: this(site, null, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="modules">The module types indicating which data to retrieve from the site. Using this constructor, all modules will be loaded using default parameters.</param>
		public PageCollection(Site site, PageModules modules)
			: this(site, new PageLoadOptions(modules), null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="options">A <see cref="PageLoadOptions"/> object initialized with a set of modules. Using this constructor allows you to customize some options.</param>
		public PageCollection(Site site, PageLoadOptions options)
			: this(site, options, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="options">A <see cref="PageLoadOptions"/> object initialized with a set of modules. Using this constructor allows you to customize some options.</param>
		/// <param name="creator">A custom page creator. Use this to create Page items that include page data from custom property modules. On Wikipedia, for example, this might be used to create pages that include geographic coordinates from the GeoData exstension.</param>
		public PageCollection(Site site, PageLoadOptions options, PageCreator creator)
			: base(site)
		{
			this.LoadOptions = options ?? site.DefaultLoadOptions;
			this.PageCreator = creator ?? site.PageCreator;
		}

		/// <summary>Initializes a new instance of the <see cref="PageCollection"/> class from a WallE result set and populates the title map from it.</summary>
		/// <param name="site">The site the result is from.</param>
		/// <param name="result">The result set.</param>
		/// <remarks>Note that this does <em>not</em> populate the collection itself, leaving that to the caller, since IPageSetResult does not provide enough information to do so.</remarks>
		public PageCollection(Site site, IPageSetResult result)
			: this(site)
		{
			ThrowNull(result, nameof(result));
			this.PopulateMapCollections(result);
		}
		#endregion

		#region Public Events

		/// <summary>Occurs for each page when any method in the class causes pages to be loaded.</summary>
		/// <remarks>This event does not fire if a page is merely added to the collection, or a new blank page is created with the <see cref="AddNewPage"/> method.</remarks>
		public event StrongEventHandler<PageCollection, Page> PageLoaded;
		#endregion

		#region Public Static Properties

		/// <summary>Gets the default namespace limitations.</summary>
		/// <value>A set of namespace IDs that will, by default, be filtered out or filtered down to automatically as pages are added.</value>
		/// <remarks>This collection can be changed in order to affect all new PageCollections which don't override the default. Be careful not to unintentionally set a property or variable directly to this property. Instead, it should normally be used to seed a new collection.</remarks>
		public static IList<int> DefaultNamespaceLimitations { get; } = new List<int>
		{
			// TODO: Figure out a better way to handle this.
			MediaWikiNamespaces.Media,
			MediaWikiNamespaces.MediaWiki,
			MediaWikiNamespaces.Special,
			MediaWikiNamespaces.Template,
			MediaWikiNamespaces.User,
		};

		/// <summary>Gets or sets a value indicating whether <see cref="NamespaceLimitations"/> specifies namespaces to be removed from the collection or only allowing those namepaces.</summary>
		/// <value>The type of the namespace limitation.</value>
		/// <remarks>This value can be changed in order to affect all new PageCollections which don't override the default.</remarks>
		public static LimitationType DefaultLimitationType { get; set; } = LimitationType.Remove;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the load options.</summary>
		/// <value>The load options.</value>
		public PageLoadOptions LoadOptions { get; set; }

		/// <summary>Gets the namespace limitations.</summary>
		/// <value>A set of namespace IDs that will be filtered out or filtered down to automatically as pages are added.</value>
		/// <remarks>Changing the contents of this collection only affects newly added pages and does not affect any existing items in the collection. Use <see cref="ReapplyLimitations"/> to do so, if needed.</remarks>
		public ICollection<int> NamespaceLimitations { get; } = new HashSet<int>(DefaultNamespaceLimitations);

		/// <summary>Gets or sets a value indicated whether <see cref="NamespaceLimitations"/> specifies namespaces to be removed from the collection or only allowing those namepaces.</summary>
		/// <value>The type of the namespace limitation.</value>
		/// <remarks>Changing this property only affects newly added pages and does not affect any existing items in the collection. Use <see cref="ReapplyLimitations"/> to do so, if needed.</remarks>
		public LimitationType LimitationType { get; set; } = DefaultLimitationType;

		/// <summary>Gets or sets the page creator.</summary>
		/// <value>The page creator.</value>
		public PageCreator PageCreator { get; set; }

		/// <summary>Gets the title map.</summary>
		/// <value>The title map.</value>
		/// <remarks>
		/// <para>The title map allows mapping from the original name you provided for a page to the actual title that was returned. If, for example, you requested "Main Page" and got redirected to "Main page", there would be an entry in the title map indicating that. Not all titles in the title map will necessarily appear in the result set. For example, if you provided an interwiki title, the result set most likely won't include that, but the title map will still include an InterwikiTitle result for it.</para>
		/// <para>The title map is largely for informational purposes. When accessing items in the collection, it will automatically check the title map and attempt to return the correct result.</para>
		/// </remarks>
		public IReadOnlyDictionary<string, TitleParts> TitleMap => this.titleMap;
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="Page"/> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The requested <see cref="Page"/>.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the page cannot be found either by the name requested or via the substitute name in the <see cref="TitleMap"/>.</exception>
		/// <remarks>Like a <see cref="Dictionary{TKey, TValue}"/>, this indexer will add a new entry if the requested entry isn't found.</remarks>
		public override Page this[string key]
		{
			get
			{
				if (this.TryGetValue(key, out var retval))
				{
					return retval;
				}

				if (this.titleMap.TryGetValue(key, out var altKey) && this.TryGetValue(altKey.FullPageName, out retval))
				{
					return retval;
				}

				throw new KeyNotFoundException();
			}

			set => base[key] = value;
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the PageCollection class with no namespace limitations.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <returns>A new PageCollection with all namespace limitations disabled.</returns>
		/// <remarks>This is a simple shortcut method to create PageCollections where limitations can safely be ignored.</remarks>
		public static PageCollection Unlimited(Site site) => new PageCollection(site) { LimitationType = LimitationType.None };

		/// <summary>Initializes a new instance of the PageCollection class with no namespace limitations.</summary>
		/// <param name="site">The site the pages are from. All pages in a collection must belong to the same site.</param>
		/// <param name="options">A <see cref="PageLoadOptions"/> object initialized with a set of modules. Using this constructor allows you to customize some options.</param>
		/// <returns>A new PageCollection with all namespace limitations disabled.</returns>
		/// <remarks>This is a simple shortcut method to create PageCollections where limitations can safely be ignored.</remarks>
		public static PageCollection Unlimited(Site site, PageLoadOptions options) => new PageCollection(site, options) { LimitationType = LimitationType.None };
		#endregion

		#region Public Methods

		/// <summary>Creates a new page using the collection's <see cref="PageCreator"/> and adds it to the collection.</summary>
		/// <param name="title">The title of the page to create.</param>
		/// <returns>The page that was created.</returns>
		/// <remarks>If the page title specified represents a page already in the collection, that page will be overwritten.</remarks>
		public Page AddNewPage(string title)
		{
			var page = this.CreatePage(title);
			this[page.Key] = page;
			return page;
		}

		/// <summary>Creates a new page using the collection's <see cref="PageCreator"/>.</summary>
		/// <param name="title">The title of the page to create.</param>
		/// <returns>The page that was created.</returns>
		public Page CreatePage(string title) => this.PageCreator.CreatePage(new TitleParts(this.Site, title));

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(params string[] titles) => this.GetTitles(new TitleCollection(this.Site, titles));

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(IEnumerable<string> titles) => this.GetTitles(new TitleCollection(this.Site, titles));

		/// <summary>Loads pages into the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void GetTitles(IEnumerable<ISimpleTitle> titles) => this.GetTitles(this.LoadOptions, titles);

		/// <summary>Reapplies the namespace limitations in <see cref="NamespaceLimitations"/> to the existing collection.</summary>
		public void ReapplyLimitations()
		{
			if (this.LimitationType == LimitationType.Remove)
			{
				this.RemoveNamespaces(this.NamespaceLimitations);
			}
			else if (this.LimitationType == LimitationType.FilterTo)
			{
				this.FilterToNamespaces(this.NamespaceLimitations);
			}
		}

		/// <summary>Removes all pages from the collection where the page's <see cref="Page.Exists"/> property is false.</summary>
		public void RemoveNonExistent()
		{
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (!this[i].Exists)
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Sets the namespace limitations to new values, clearing out any previous limitations.</summary>
		/// <param name="namespaceLimitations">The namespace limitations. If null, only the limitation type is applied; the namespace set will remain unchanged.</param>
		/// <param name="limitationType">The type of namespace limitations to apply.</param>
		/// <remarks>If the <paramref name="namespaceLimitations"/> parameter is null, no changes will be made to either of the limitation properties. This allows current/default limitations to remain in place if needed.</remarks>
		public void SetLimitations(IEnumerable<int> namespaceLimitations, LimitationType limitationType)
		{
			this.LimitationType = limitationType;
			if (limitationType != LimitationType.None && namespaceLimitations != null)
			{
				this.NamespaceLimitations.Clear();
				this.NamespaceLimitations.AddRange(namespaceLimitations);
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles to add.</param>
		/// <remarks>Unlike <see cref="GetTitles(IEnumerable{string})"/> and related methods, which all load data from the wiki, this will simply add blank pages to the result set.</remarks>
		public override void Add(IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				this.AddNewPage(title);
			}
		}

		/// <summary>Adds the specified titles to the collection, assuming that they are in the provided namespace if no other namespace is specified.</summary>
		/// <param name="defaultNamespace">The namespace to coerce.</param>
		/// <param name="titles">The titles to add, with or without the leading namespace text.</param>
		public override void Add(int defaultNamespace, IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				var titleParts = new TitleParts(this.Site, title);
				if (titleParts.Namespace.Id == MediaWikiNamespaces.Main)
				{
					titleParts.Namespace = this.Site.Namespaces[defaultNamespace];
				}
				else if (titleParts.Namespace.Id != defaultNamespace)
				{
					titleParts.Namespace = this.Site.Namespaces[defaultNamespace];
					titleParts.PageName = title;
				}

				this.Add(this.PageCreator.CreatePage(titleParts));
			}
		}

		/// <summary>Adds new pages based on an existing <see cref="ISimpleTitle"/> collection.</summary>
		/// <param name="titles">The titles to be added.</param>
		public override void AddFrom(IEnumerable<ISimpleTitle> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				var page = this.PageCreator.CreatePage(title);
				this[page.Key] = page;
			}
		}

		/// <summary>Removes all items from the <see cref="TitleCollection">collection</see>, as well as those in the <see cref="TitleMap"/>.</summary>
		public override void Clear()
		{
			base.Clear();
			this.titleMap.Clear();
		}

		/// <summary>Adds pages with the specified revision IDs to the collection.</summary>
		/// <param name="revisionIds">The IDs.</param>
		/// <remarks>General information about the pages for the revision IDs specified will always be loaded, regardless of the LoadOptions setting, though the revisions themselves may not be if the collection's load options would filter them out.</remarks>
		// Note that while RevisionsInput() can be used as a generator, I have not implemented it because I can think of no situation in which it would be useful to populate a PageCollection given the existing revisions methods.
		public override void GetRevisionIds(IEnumerable<long> revisionIds) => this.LoadPages(this.LoadOptions, QueryPageSetInput.FromRevisionIds(revisionIds));
		#endregion

		#region Internal Static Methods

		/// <summary>Initializes a new PageCollection intended to store results of other operations like Purge, Watch, or Unwatch.</summary>
		/// <param name="site">The site.</param>
		/// <returns>A new PageCollection with no namespace limitations, load options set to none, and creating only default pages rather than user-specified.</returns>
		internal static PageCollection UnlimitedDefault(Site site) => new PageCollection(site, PageLoadOptions.None, PageCreator.Default) { LimitationType = LimitationType.None };

		/// <summary>Initializes a new PageCollection intended to store results of other operations like Purge, Watch, or Unwatch.</summary>
		/// <param name="site">The site.</param>
		/// <param name="other">The collection to initialize this instance from.</param>
		/// <returns>A new PageCollection with no namespace limitations, load options set to none, and creating only default pages rather than user-specified.</returns>
		internal static PageCollection UnlimitedDefault(Site site, IEnumerable<ISimpleTitle> other)
		{
			var retval = UnlimitedDefault(site);
			retval.AddFrom(other);
			return retval;
		}
		#endregion

		#region Internal Methods
		internal void PopulateMapCollections(IPageSetResult result)
		{
			foreach (var item in result.Interwiki)
			{
				var titleParts = new TitleParts(this.Site, item.Value.Title);
				Debug.Assert(titleParts.Interwiki.Prefix != item.Value.InterwikiPrefix, "Interwiki prefixes didn't match.", titleParts.Interwiki.Prefix + " != " + item.Value.InterwikiPrefix);
				this.titleMap[item.Key] = titleParts;
			}

			foreach (var item in result.Converted)
			{
				this.titleMap[item.Key] = new TitleParts(this.Site, item.Value);
			}

			foreach (var item in result.Normalized)
			{
				this.titleMap[item.Key] = new TitleParts(this.Site, item.Value);
			}

			foreach (var item in result.Redirects)
			{
				// Move interwiki redirects to InterwikiTitles collection, since lookups would try to redirect to a local page with the same name.
				var value = item.Value;
				this.titleMap[item.Key] = new TitleParts(this.Site, value.Interwiki, value.Title, value.Fragment);
			}
		}
		#endregion

		#region Protected Methods

		/// <summary>Gets a value indicating whether the page title is within the collection's limitations.</summary>
		/// <param name="page">The page.</param>
		/// <returns><see langword="true"/> if the page is within the collection's limitations and can be added to it; otherwise, <see langword="false"/>.</returns>
		protected bool IsTitleInLimits(ISimpleTitle page) =>
			page != null &&
			(this.LimitationType == LimitationType.None ||
			(this.LimitationType == LimitationType.Remove && !this.NamespaceLimitations.Contains(page.Namespace.Id)) ||
			(this.LimitationType == LimitationType.FilterTo && this.NamespaceLimitations.Contains(page.Namespace.Id)));
		#endregion

		#region Protected Override Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetBacklinks(BacklinksInput input)
		{
			ThrowNull(input, nameof(input));
			var inputTitle = new TitleParts(this.Site, input.Title);
			if (inputTitle.Namespace != MediaWikiNamespaces.File && input.LinkTypes.HasFlag(BacklinksTypes.ImageUsage))
			{
				input = new BacklinksInput(input, input.LinkTypes & ~BacklinksTypes.ImageUsage);
			}

			foreach (var type in input.LinkTypes.GetUniqueFlags())
			{
				this.LoadPages(new BacklinksInput(input.Title, type)
				{
					FilterRedirects = input.FilterRedirects,
					Namespace = input.Namespace,
					Redirect = input.Redirect,
				});
			}
		}

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetCategories(AllCategoriesInput input) => this.LoadPages(input);

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <c>true</c> load the entire category tree recursively.</param>
		protected override void GetCategoryMembers(CategoryMembersInput input, bool recurse)
		{
			ThrowNull(input, nameof(input));
			if (recurse)
			{
				this.RecurseCategoryPages(input, new HashSet<string>());
			}
			else
			{
				this.LoadPages(input);
			}
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected override void GetDuplicateFiles(DuplicateFilesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFiles(AllImagesInput input) => this.LoadPages(input);

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFileUsage(AllFileUsagesInput input) => this.LoadPages(input);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void GetFileUsage(FileUsageInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetLinksToNamespace(AllLinksInput input) => this.LoadPages(input);

		/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetNamespace(AllPagesInput input) => this.LoadPages(input);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageCategories(CategoriesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageLinks(LinksInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose backlinks should be loaded.</param>
		protected override void GetPageLinksHere(LinksHereInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTransclusions(TemplatesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that transclude the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTranscludedIn(TranscludedInInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPagesWithProperty(PagesWithPropertyInput input) => this.LoadPages(input);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPrefixSearchResults(PrefixSearchInput input) => this.LoadPages(input);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected override void GetQueryPage(QueryPageInput input) => this.LoadPages(input);

		/// <summary>Gets a random set of pages from the wiki.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRandomPages(RandomInput input) => this.LoadPages(input);

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRecentChanges(RecentChangesInput input) => this.LoadPages(input);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRedirectsToNamespace(AllRedirectsInput input) => this.LoadPages(input);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRevisions(AllRevisionsInput input) => this.LoadPages(input);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetSearchResults(SearchInput input) => this.LoadPages(input);

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetTransclusions(AllTransclusionsInput input) => this.LoadPages(input);

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistChanged(WatchlistInput input) => this.LoadPages(input);

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistRaw(WatchlistRawInput input) => this.LoadPages(input);

		/// <summary>Inserts an item into the <see cref="PageCollection">collection</see>.</summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item.</param>
		/// <remarks>This method underlies all of the various add methods, and can be overridden in derived classes.</remarks>
		protected override void InsertItem(int index, Page item)
		{
			ThrowNull(item, nameof(item));
			if (this.IsTitleInLimits(item))
			{
				base.InsertItem(index, item);
			}
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Adds pages to the collection from a series of titles.</summary>
		/// <param name="options">The page load options.</param>
		/// <param name="titles">The titles.</param>
		protected virtual void GetTitles(PageLoadOptions options, IEnumerable<ISimpleTitle> titles) => this.LoadPages(options, new QueryPageSetInput(titles.ToFullPageNames()));

		/// <summary>Loads pages from the wiki based on a page set specifier.</summary>
		/// <param name="options">The page load options.</param>
		/// <param name="pageSetInput">The page set input.</param>
		/// <param name="pageValidator">A function which validates whether a page can be added to the collection.</param>
		protected virtual void LoadPages(PageLoadOptions options, QueryPageSetInput pageSetInput, Func<Page, bool> pageValidator)
		{
			ThrowNull(options, nameof(options));
			ThrowNull(pageSetInput, nameof(pageSetInput));
			ThrowNull(pageValidator, nameof(pageValidator));
			pageSetInput.ConvertTitles = options.ConvertTitles;
			pageSetInput.Redirects = options.FollowRedirects;
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, this.PageCreator.GetPropertyInputs(options), this.PageCreator.CreatePageItem);
			this.PopulateMapCollections(result);
			foreach (var item in result)
			{
				var page = this.CreatePage(item.Value.Title);
				page.Populate(item.Value);
				page.LoadOptions = options;
				if (pageValidator != null && pageValidator(page))
				{
					this[page.Key] = page;
					this.PageLoaded?.Invoke(this, page);
				}
			}
		}
		#endregion

		#region Private Methods
		private void LoadPages(IGeneratorInput generator) => this.LoadPages(this.LoadOptions, new QueryPageSetInput(generator));

		private void LoadPages(IGeneratorInput generator, IEnumerable<ISimpleTitle> titles) => this.LoadPages(this.LoadOptions, new QueryPageSetInput(generator, titles.ToFullPageNames()));

		private void LoadPages(PageLoadOptions options, QueryPageSetInput pageSetInput) => this.LoadPages(options, pageSetInput, this.IsTitleInLimits);

		private bool RecurseCategoryHandler(Page page)
		{
			if (page.Namespace.Id == MediaWikiNamespaces.Category)
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
			if (!categoryTree.Add(input.Title))
			{
				return;
			}

			this.recurseCategories.Clear();
			this.LoadPages(this.LoadOptions, new QueryPageSetInput(input), this.RecurseCategoryHandler);
			var copy = new List<string>(this.recurseCategories);
			this.recurseCategories.Clear();
			foreach (var category in copy)
			{
				input.ChangeTitle(category);
				this.RecurseCategoryPages(input, categoryTree);
			}
		}
		#endregion
	}
}