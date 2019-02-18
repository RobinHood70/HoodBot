namespace RobinHood70.Robby
{
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
	/// <seealso cref="Robby.TitleCollection{TTitle}" />
	public class PageCollection : TitleCollection<Page>
	{
		#region Fields
		private readonly Dictionary<string, TitleParts> titleMap = new Dictionary<string, TitleParts>();
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
		/// <remarks>Like a <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>, this indexer will add a new entry if the requested entry isn't found.</remarks>
		public override Page this[string key]
		{
			get
			{
				if (!this.TryGetValue(key, out var retval) && !this.TryGetValue(this.titleMap[key].FullPageName, out retval))
				{
					throw new KeyNotFoundException();
				}

				return retval;
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

		/// <summary>Adds pages to the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void AddTitles(params string[] titles) => this.AddTitles(new TitleCollection(this.Site, titles));

		/// <summary>Adds pages to the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void AddTitles(IEnumerable<string> titles) => this.AddTitles(new TitleCollection(this.Site, titles));

		/// <summary>Adds pages to the collection from a series of titles.</summary>
		/// <param name="titles">The titles.</param>
		public void AddTitles(IEnumerable<ISimpleTitle> titles) => this.AddTitles(this.LoadOptions, titles);

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

		/// <summary>Sets the namespace limitations to new values, clearing out any previous limitations.</summary>
		/// <param name="namespaceLimitations">The namespace limitations. If null, only the limitation type is applied; the namespace set will remain unchanged.</param>
		/// <param name="limitationType">The type of namespace limitations to apply.</param>
		/// <remarks>If the <paramref name="namespaceLimitations"/> parameter is null, no changes will be made to either of the limitation properties. This allows current/default limitations to remain in place if needed.</remarks>
		public void SetLimitations(IEnumerable<int> namespaceLimitations, LimitationType limitationType)
		{
			this.LimitationType = limitationType;
			if (namespaceLimitations != null)
			{
				this.NamespaceLimitations.Clear();
				this.NamespaceLimitations.AddRange(namespaceLimitations);
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles to add.</param>
		/// <remarks>Unlike <see cref="AddTitles(IEnumerable{string})"/> and related methods, which all load data from the wiki, this will simply add blank pages to the result set.</remarks>
		public override void Add(IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				var titleParts = new TitleParts(this.Site, title);
				this.Add(this.PageCreator.CreatePage(titleParts));
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

		/// <summary>Adds pages with the specified revision IDs to the collection.</summary>
		/// <param name="revisionIds">The IDs.</param>
		/// <remarks>General information about the pages for the revision IDs specified will always be loaded, regardless of the LoadOptions setting, though the revisions themselves may not be if the collection's load options would filter them out.</remarks>
		// Note that while RevisionsInput() can be used as a generator, I have not implemented it because I can think of no situation in which it would be useful to populate a PageCollection given the existing revisions methods.
		public override void AddRevisionIds(IEnumerable<long> revisionIds) => this.LoadPages(this.LoadOptions, QueryPageSetInput.FromRevisionIds(revisionIds));

		/// <summary>Removes all items from the <see cref="TitleCollection">collection</see>, as well as those in the <see cref="TitleMap"/>.</summary>
		public override void Clear()
		{
			base.Clear();
			this.titleMap.Clear();
		}
		#endregion

		#region Protected Override Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddBacklinks(BacklinksInput input)
		{
			ThrowNull(input, nameof(input));
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
		protected override void AddCategories(AllCategoriesInput input) => this.LoadPages(input);

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <c>true</c> load the entire category tree recursively.</param>
		protected override void AddCategoryMembers(CategoryMembersInput input, bool recurse)
		{
			ThrowNull(input, nameof(input));
			if (recurse)
			{
				this.LoadPages(input);
			}
			else
			{
				this.LoadPages(input, new HashSet<ISimpleTitle>());
			}
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected override void AddDuplicateFiles(DuplicateFilesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddFiles(AllImagesInput input) => this.LoadPages(input);

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddFileUsage(AllFileUsagesInput input) => this.LoadPages(input);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void AddFileUsage(FileUsageInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddLinksToNamespace(AllLinksInput input) => this.LoadPages(input);

		/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddNamespace(AllPagesInput input) => this.LoadPages(input);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void AddPageCategories(CategoriesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void AddPageLinks(LinksInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose backlinks should be loaded.</param>
		protected override void AddPageLinksHere(LinksHereInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void AddPageTransclusions(TemplatesInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages that transclude the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void AddPageTranscludedIn(TranscludedInInput input, IEnumerable<ISimpleTitle> titles) => this.LoadPages(input, titles);

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddPagesWithProperty(PagesWithPropertyInput input) => this.LoadPages(input);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddPrefixSearchResults(PrefixSearchInput input) => this.LoadPages(input);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected override void AddQueryPage(QueryPageInput input) => this.LoadPages(input);

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddRecentChanges(RecentChangesInput input) => this.LoadPages(input);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddRedirectsToNamespace(AllRedirectsInput input) => this.LoadPages(input);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddRevisions(AllRevisionsInput input) => this.LoadPages(input);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddSearchResults(SearchInput input) => this.LoadPages(input);

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddTransclusions(AllTransclusionsInput input) => this.LoadPages(input);

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddWatchlistChanged(WatchlistInput input) => this.LoadPages(input);

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void AddWatchlistRaw(WatchlistRawInput input) => this.LoadPages(input);

		/// <summary>Inserts an item into the <see cref="PageCollection">collection</see>.</summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item.</param>
		/// <remarks>This method underlies all of the various add methods, and can be overridden in derived classes.</remarks>
		protected override void InsertItem(int index, Page item)
		{
			ThrowNull(item, nameof(item));
			if (
				this.LimitationType == LimitationType.None ||
				(this.LimitationType == LimitationType.Remove && !this.NamespaceLimitations.Contains(item.Namespace.Id)) ||
				(this.LimitationType == LimitationType.FilterTo && this.NamespaceLimitations.Contains(item.Namespace.Id)))
			{
				base.InsertItem(index, item);
			}
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Adds pages to the collection from a series of titles.</summary>
		/// <param name="options">The page load options.</param>
		/// <param name="titles">The titles.</param>
		protected virtual void AddTitles(PageLoadOptions options, IEnumerable<ISimpleTitle> titles) => this.LoadPages(options, new QueryPageSetInput(titles.ToFullPageNames()));

		/// <summary>Loads pages from the wiki based on a page set specifier.</summary>
		/// <param name="options">The page load options.</param>
		/// <param name="pageSetInput">The page set input.</param>
		protected virtual void LoadPages(PageLoadOptions options, QueryPageSetInput pageSetInput)
		{
			ThrowNull(options, nameof(options));
			ThrowNull(pageSetInput, nameof(pageSetInput));
			pageSetInput.ConvertTitles = options.ConvertTitles;
			pageSetInput.Redirects = options.FollowRedirects;
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, this.PageCreator.GetPropertyInputs(options), this.PageCreator.CreatePageItem);
			foreach (var item in result)
			{
				var titleParts = new TitleParts(this.Site, item.Value.Title);
				var page = this.PageCreator.CreatePage(titleParts);
				page.Populate(item.Value);
				page.LoadOptions = options;
				this[page.Key] = page; // Not using add because we could be loading duplicate pages.
			}

			this.ReapplyLimitations(); // Not the ideal way of doing this, but probably the most straight-forward. Other option would be to filter for duplicates first via a HashSet or similar, then add to the collection.
			this.PopulateMapCollections(result);
		}

		/// <summary>Loads category pages recursively.</summary>
		/// <param name="input">The input.</param>
		/// <param name="categoryTree">A hashet used to track which categories have already been loaded. This avoids loading the same category if it appears in the tree more than once, and breaks possible recursion loops.</param>
		protected virtual void LoadPages(CategoryMembersInput input, HashSet<ISimpleTitle> categoryTree)
		{
			ThrowNull(input, nameof(input));
			ThrowNull(categoryTree, nameof(categoryTree));
			if (!categoryTree.Add(new TitleParts(this.Site, input.Title)))
			{
				return;
			}

			var newInput = new CategoryMembersInput(input);
			newInput.Properties |= CategoryMembersProperties.Title | CategoryMembersProperties.Type;
			newInput.Type = newInput.Type | CategoryMemberTypes.Subcat;

			var result = this.Site.AbstractionLayer.LoadPages(new QueryPageSetInput(newInput), this.PageCreator.GetPropertyInputs(this.LoadOptions), this.PageCreator.CreatePageItem);
			this.PopulateMapCollections(result);
			foreach (var item in result)
			{
				var titleParts = new TitleParts(this.Site, item.Value.Title);
				var page = this.PageCreator.CreatePage(titleParts);
				page.Populate(item.Value);
				page.LoadOptions = this.LoadOptions;
				if (input.Type.HasFlag(CategoryMemberTypes.Subcat) || page.Namespace.Id != MediaWikiNamespaces.Category)
				{
					this[page.Key] = page;
				}

				if (page.Namespace.Id == MediaWikiNamespaces.Category)
				{
					var recurseInput = new CategoryMembersInput(page.FullPageName)
					{
						Properties = newInput.Properties,
						Type = newInput.Type,
					};
					this.LoadPages(recurseInput, categoryTree);
				}
			}
		}
		#endregion

		#region Private Methods
		private void LoadPages(IGeneratorInput generator) => this.LoadPages(this.LoadOptions, new QueryPageSetInput(generator));

		private void LoadPages(IGeneratorInput generator, IEnumerable<ISimpleTitle> titles) => this.LoadPages(this.LoadOptions, new QueryPageSetInput(generator, titles.ToFullPageNames()));

		private void PopulateMapCollections(IPageSetResult result)
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
	}
}