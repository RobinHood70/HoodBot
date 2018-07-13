namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.Robby.Properties.Resources;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Provides a base class to manipulate a collection of titles.</summary>
	/// <typeparam name="TTitle">The type of the title.</typeparam>
	/// <seealso cref="System.Collections.Generic.IList{TTitle}" />
	/// <seealso cref="System.Collections.Generic.IReadOnlyCollection{TTitle}" />
	/// <remarks>This collection class functions similarly to a KeyedCollection, but automatically overwrites existing items with new ones. Unlike a KeyedCollection, however, it does not support changing an item's key, since <see cref="IKeyedTitle"/> does not allow this.</remarks>
	public abstract class TitleCollection<TTitle> : IList<TTitle>, IReadOnlyCollection<TTitle>
		where TTitle : IKeyedTitle
	{
		#region Fields
		private readonly Dictionary<string, TTitle> dictionary = new Dictionary<string, TTitle>();
		private readonly List<TTitle> items = new List<TTitle>();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleCollection{TTitle}" /> class.</summary>
		/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
		protected TitleCollection([ValidatedNotNull] Site site)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the number of elements contained in the <see cref="TitleCollection">collection</see>.</summary>
		/// <value>The number of elements contained in the <see cref="TitleCollection">collection</see>.</value>
		public int Count => this.items.Count;

		/// <summary>Gets a value indicating whether the <see cref="TitleCollection">collection</see> is read-only.</summary>
		/// <value><c>true</c> if the collection is read-only.</value>
		public bool IsReadOnly { get; } = false;

		/// <summary>Gets the site for the collection.</summary>
		/// <value>The site.</value>
		public Site Site { get; }
		#endregion

		#region Protected Properties

		/// <summary>Gets the collection's underlying title lookup dictionary.</summary>
		/// <value>The dictionary.</value>
		protected IReadOnlyDictionary<string, TTitle> Dictionary => this.dictionary;

		/// <summary>Gets the list of all items in the collection.</summary>
		/// <value>The items.</value>
		protected IReadOnlyList<TTitle> Items => this.items;
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="ISimpleTitle">Title</see> at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="ISimpleTitle">Title</see> at the specified index.</returns>
		public TTitle this[int index]
		{
			get => this.items[index];
			set
			{
				ThrowNull(value, nameof(value));
				this.items[index] = value;
				this.dictionary[value.Key] = value;
			}
		}

		/// <summary>Gets or sets the <see cref="ISimpleTitle">Title</see> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The <see cref="ISimpleTitle">Title</see>.</returns>
		/// <remarks>Like a <see cref="T:System.Collections.Generic.Dictionary{TKey, TValue}"/>, this indexer will add a new entry if the requested entry isn't found.</remarks>
		public virtual TTitle this[string key]
		{
			get => this.dictionary[key];
			set
			{
				var index = this.IndexOf(key);
				if (index < 0)
				{
					this.items.Add(value);
				}
				else
				{
					this.items[index] = value;
				}

				this.dictionary[key] = value;
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Adds an item to the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The object to add to the <see cref="TitleCollection">collection</see>.</param>
		public void Add(TTitle item)
		{
			var index = this.IndexOf(item);
			if (index != -1)
			{
				// We don't touch the dictionary here because InsertItem will simply write over top of the existing entry.
				this.items.RemoveAt(index);
			}

			this.InsertItem(this.items.Count, item);
		}

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles.</param>
		public void Add(params string[] titles) => this.Add(titles as IEnumerable<string>);

		/// <summary>Adds the specified titles to the collection, coercing them to the given namespace.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="names">The page names, with or without the leading namespace text.</param>
		public void Add(int ns, params string[] names) => this.Add(ns, names as IEnumerable<string>);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		public void AddBacklinks(string title) => this.AddBacklinks(title, BacklinksTypes.Backlinks | BacklinksTypes.EmbeddedIn, true, Filter.Any);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		public void AddBacklinks(string title, BacklinksTypes linkTypes) => this.AddBacklinks(title, linkTypes, true, Filter.Any);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="includeRedirectedTitles">if set to <c>true</c>, pages linking to <paramref name="title"/> via a redirect will be included.</param>
		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles) => this.AddBacklinks(title, linkTypes, includeRedirectedTitles, Filter.Any);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="redirects">Whether or not to include redirects in the results.</param>
		public void AddBacklinks(string title, BacklinksTypes linkTypes, Filter redirects) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects });

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="includeRedirectedTitles">if set to <c>true</c>, pages linking to <paramref name="title"/> via a redirect will be included.</param>
		/// <param name="redirects">Whether or not to include redirects in the results.</param>
		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Redirect = includeRedirectedTitles });

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="includeRedirectedTitles">if set to <c>true</c>, pages linking to <paramref name="title"/> via a redirect will be included.</param>
		/// <param name="redirects">Whether or not to include redirects in the results.</param>
		/// <param name="ns">The namespace to limit the results to.</param>
		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects, int ns) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Namespace = ns, Redirect = includeRedirectedTitles });

		/// <summary>Adds a set of category pages to the collection.</summary>
		public void AddCategories() => this.AddCategories(new AllCategoriesInput());

		/// <summary>Adds a set of category pages that start with the specified prefix to the collection.</summary>
		/// <param name="prefix">The prefix of the categories to load.</param>
		public void AddCategories(string prefix) => this.AddCategories(new AllCategoriesInput { Prefix = prefix });

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="from">The category to start at (inclusive). The category specified does not have to exist.</param>
		/// <param name="to">The category to stop at (inclusive). The category specified does not have to exist.</param>
		public void AddCategories(string from, string to) => this.AddCategories(new AllCategoriesInput { From = from, To = to });

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="category">The category.</param>
		/// <param name="recurse">if set to <c>true</c> recurses through subcategories.</param>
		public void AddCategoryMembers(string category, bool recurse) => this.AddCategoryMembers(category, CategoryMemberTypes.All, null, null, recurse);

		/// <summary>Adds category members of the specified type to the collection, potentially including subcategories and their members.</summary>
		/// <param name="category">The category.</param>
		/// <param name="categoryMemberTypes">The category member types to load.</param>
		/// <param name="recurse">if set to <c>true</c> recurses through subcategories.</param>
		public void AddCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, bool recurse) => this.AddCategoryMembers(category, categoryMemberTypes, null, null, recurse);

		/// <summary>Adds category members of the specified type and within the specified range to the collection, potentially including subcategories and their members.</summary>
		/// <param name="category">The category.</param>
		/// <param name="categoryMemberTypes">The category member types to load.</param>
		/// <param name="from">The category member to start at (inclusive). The member specified does not have to exist.</param>
		/// <param name="to">The category member to stop at (inclusive). The member specified does not have to exist.</param>
		/// <param name="loadSubcategories">if set to <c>true</c> recurses through subcategories.</param>
		/// <remarks>If subcategories are loaded, they will be limited to the <paramref name="categoryMemberTypes"/> requested. However, they will <em>not</em> be limited by the <paramref name="from"/> and <paramref name="to"/> parameters.</remarks>
		public void AddCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, string from, string to, bool loadSubcategories)
		{
			var cat = Title.ForcedNamespace(this.Site.Namespaces[MediaWikiNamespaces.Category], category);
			this.AddCategoryMembers(
				new CategoryMembersInput(cat.FullPageName)
				{
					Type = categoryMemberTypes,
					StartSortKeyPrefix = from,
					EndSortKeyPrefix = to,
				},
				loadSubcategories);
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="titles">The titles to find duplicates of.</param>
		public void AddDuplicateFiles(IEnumerable<ISimpleTitle> titles) => this.AddDuplicateFiles(titles, false);

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="titles">The titles to find duplicates of.</param>
		/// <param name="localOnly">if set to <c>true</c> [local only].</param>
		public void AddDuplicateFiles(IEnumerable<ISimpleTitle> titles, bool localOnly) => this.AddDuplicateFiles(new DuplicateFilesInput() { LocalOnly = localOnly }, titles);

		/// <summary>Adds files uploaded by the specified user to the collection.</summary>
		/// <param name="user">The user.</param>
		public void AddFiles(string user) => this.AddFiles(new AllImagesInput { User = user });

		/// <summary>Adds a range of files to the collection.</summary>
		/// <param name="from">The file name to start at (inclusive).</param>
		/// <param name="to">The file name to end at (inclusive).</param>
		public void AddFiles(string from, string to) => this.AddFiles(new AllImagesInput { From = from, To = to });

		/// <summary>Adds a range of files to the collection based on the most recent version.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="end">The date to end at (inclusive).</param>
		public void AddFiles(DateTime start, DateTime end) => this.AddFiles(new AllImagesInput { Start = start, End = end });

		/// <summary>Adds all files that are in use to the collection.</summary>
		public void AddFileUsage() => this.AddFileUsage(new AllFileUsagesInput { Unique = true });

		/// <summary>Adds in-use files that have a given prefix to the collection.</summary>
		/// <param name="prefix">The prefix of the files to load.</param>
		public void AddFileUsage(string prefix) => this.AddFileUsage(new AllFileUsagesInput { Prefix = prefix, Unique = true });

		/// <summary>Adds a range of in-use files to the collection.</summary>
		/// <param name="from">The file name to start at (inclusive).</param>
		/// <param name="to">The file name to end at (inclusive).</param>
		public void AddFileUsage(string from, string to) => this.AddFileUsage(new AllFileUsagesInput { From = from, To = to, Unique = true });

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="titles">The titles.</param>
		public void AddFileUsage(IEnumerable<ISimpleTitle> titles) => this.AddFileUsage(new FileUsageInput(), titles);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="titles">The titles.</param>
		/// <param name="redirects">Filter for redirects.</param>
		public void AddFileUsage(IEnumerable<ISimpleTitle> titles, Filter redirects) => this.AddFileUsage(new FileUsageInput() { FilterRedirects = redirects }, titles);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="titles">The titles.</param>
		/// <param name="redirects">Filter for redirects.</param>
		/// <param name="namespaces">The namespaces to limit results to.</param>
		public void AddFileUsage(IEnumerable<ISimpleTitle> titles, Filter redirects, IEnumerable<int> namespaces) => this.AddFileUsage(new FileUsageInput() { Namespaces = namespaces, FilterRedirects = redirects }, titles);

		/// <summary>Adds pages that link to a given namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		public void AddLinksToNamespace(int ns) => this.AddLinksToNamespace(new AllLinksInput { Namespace = ns });

		/// <summary>Adds pages that link to a given namespace and begin with a certain prefix to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="prefix">The prefix of the pages to load.</param>
		public void AddLinksToNamespace(int ns, string prefix) => this.AddLinksToNamespace(new AllLinksInput { Namespace = ns, Prefix = prefix });

		/// <summary>Adds pages that link to a given namespace within a given range to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="from">The page name to start at (inclusive).</param>
		/// <param name="to">The page name to end at (inclusive).</param>
		public void AddLinksToNamespace(int ns, string from, string to) => this.AddLinksToNamespace(new AllLinksInput { Namespace = ns, From = from, To = to });

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="redirects">Whether or not to include pages that are redirects.</param>
		public void AddNamespace(int ns, Filter redirects) => this.AddNamespace(new AllPagesInput { FilterRedirects = redirects, Namespace = ns });

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="redirects">Whether or not to include pages that are redirects.</param>
		/// <param name="prefix">The prefix of the pages to load.</param>
		public void AddNamespace(int ns, Filter redirects, string prefix) => this.AddNamespace(new AllPagesInput { FilterRedirects = redirects, Namespace = ns, Prefix = prefix });

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="redirects">Whether or not to include pages that are redirects.</param>
		/// <param name="from">The page name to start at (inclusive).</param>
		/// <param name="to">The page name to end at (inclusive).</param>
		public void AddNamespace(int ns, Filter redirects, string from, string to) => this.AddNamespace(new AllPagesInput { FilterRedirects = redirects, From = from, Namespace = ns, To = to });

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		public void AddPageCategories(IEnumerable<ISimpleTitle> titles) => this.AddPageCategories(new CategoriesInput(), titles);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		/// <param name="hidden">Filter for hidden categories.</param>
		public void AddPageCategories(IEnumerable<ISimpleTitle> titles, Filter hidden) => this.AddPageCategories(new CategoriesInput { FilterHidden = hidden }, titles);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		/// <param name="hidden">Filter for hidden categories.</param>
		/// <param name="limitTo">Limit the results to these categories.</param>
		public void AddPageCategories(IEnumerable<ISimpleTitle> titles, Filter hidden, IEnumerable<string> limitTo) => this.AddPageCategories(new CategoriesInput { Categories = limitTo, FilterHidden = hidden }, titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		public void AddPageLinks(IEnumerable<ISimpleTitle> titles) => this.AddPageLinks(titles, null);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		/// <param name="namespaces">The namespaces to limit results to.</param>
		public void AddPageLinks(IEnumerable<ISimpleTitle> titles, IEnumerable<int> namespaces) => this.AddPageLinks(new LinksInput() { Namespaces = namespaces }, titles);

		/// <summary>Adds pages with a given page property (e.g., notrail, breadCrumbTrail) to the collection.</summary>
		/// <param name="property">The property to find.</param>
		public void AddPagesWithProperty(string property) => this.AddPagesWithProperty(new PagesWithPropertyInput(property));

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		public void AddPageTransclusions(IEnumerable<ISimpleTitle> titles) => this.AddPageTransclusions(new TemplatesInput(), titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		/// <param name="limitTo">Limit the results to these transclusions.</param>
		public void AddPageTransclusions(IEnumerable<ISimpleTitle> titles, IEnumerable<string> limitTo) => this.AddPageTransclusions(new TemplatesInput() { Templates = limitTo }, titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		/// <param name="namespaces">Limit the results to these namespaces.</param>
		public void AddPageTransclusions(IEnumerable<ISimpleTitle> titles, IEnumerable<int> namespaces) => this.AddPageTransclusions(new TemplatesInput() { Namespaces = namespaces }, titles);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="prefix">The prefix to search for.</param>
		/// <remarks>As noted on the API page for PrefixSearch, this is <em>not</em> the same as other prefix-based methods in that it doesn't strictly look for pages to start with the same literal letters. It's run through the installed search engine instead, and may include such things as word substitution, spelling correction, etc.</remarks>
		public void AddPrefixSearchResults(string prefix) => this.AddPrefixSearchResults(new PrefixSearchInput(prefix));

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="prefix">The prefix to search for.</param>
		/// <param name="namespaces">The namespaces to search in.</param>
		/// <remarks>As noted on the API page for PrefixSearch, this is <em>not</em> the same as other prefix-based methods in that it doesn't strictly look for pages to start with the same literal letters. It's run through the installed search engine instead, and may include such things as word substitution, spelling correction, etc.</remarks>
		public void AddPrefixSearchResults(string prefix, IEnumerable<int> namespaces) => this.AddPrefixSearchResults(new PrefixSearchInput(prefix) { Namespaces = namespaces });

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="page">The query-page-compatible module.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		public void AddQueryPage(string page) => this.AddQueryPage(new QueryPageInput(page));

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="page">The query-page-compatible module.</param>
		/// <param name="parameters">The custom parameters to provide to the query page module.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		public void AddQueryPage(string page, IReadOnlyDictionary<string, string> parameters) => this.AddQueryPage(new QueryPageInput(page) { Parameters = parameters });

		/// <summary>Adds multiple titles to the <see cref="TitleCollection">collection</see> at once.</summary>
		/// <param name="titles">The titles to add.</param>
		/// <remarks>This method is for convenience only. Unlike the equivalent <see cref="List{T}" /> function, it simply calls <see cref="Add(TTitle)" /> repeatedly and provides no performance benefit.</remarks>
		public void AddRange(IEnumerable<TTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(title);
				}
			}
		}

		/// <summary>Adds all available recent changes pages to the collection.</summary>
		public void AddRecentChanges() => this.AddRecentChanges(new RecentChangesInput());

		/// <summary>Adds recent changes pages to the collection, filtered to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to limit results to.</param>
		public void AddRecentChanges(IEnumerable<int> namespaces) => this.AddRecentChanges(new RecentChangesInput { Namespaces = namespaces, });

		/// <summary>Adds recent changes pages to the collection, filtered to a specific tag.</summary>
		/// <param name="tag">A tag to limit results to.</param>
		public void AddRecentChanges(string tag) => this.AddRecentChanges(new RecentChangesInput { Tag = tag });

		/// <summary>Adds recent changes pages to the collection, filtered to the specified types of changes.</summary>
		/// <param name="types">The types of changes to limit results to.</param>
		public void AddRecentChanges(RecentChangesTypes types) => this.AddRecentChanges(new RecentChangesInput { Types = types });

		/// <summary>Adds recent changes pages to the collection, filtered based on properties of the change.</summary>
		/// <param name="anonymous">Include anonymous edits in the results.</param>
		/// <param name="bots">Include bot edits in the results.</param>
		/// <param name="minor">Include minor edits in the results.</param>
		/// <param name="patrolled">Include patrolled edits in the results.</param>
		/// <param name="redirects">Include redirects in the results.</param>
		public void AddRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects) => this.AddRecentChanges(new RecentChangesInput { FilterAnonymous = anonymous, FilterBots = bots, FilterMinor = minor, FilterPatrolled = patrolled, FilterRedirects = redirects });

		/// <summary>Adds recent changes pages to the collection, filtered to a date range.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="end">The date to end at (inclusive).</param>
		public void AddRecentChanges(DateTime? start, DateTime? end) => this.AddRecentChanges(new RecentChangesInput { Start = start, End = end });

		/// <summary>Adds recent changes pages to the collection starting at a given date and time and moving forward or backward from there.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <c>true</c>, changes from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		public void AddRecentChanges(DateTime start, bool newer) => this.AddRecentChanges(start, newer, 0);

		/// <summary>Adds a specified number of recent changes pages to the collection starting at a given date and time and moving forward or backward from there.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <c>true</c>, changes from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		/// <param name="count">The number of changes to return.</param>
		public void AddRecentChanges(DateTime start, bool newer, int count) => this.AddRecentChanges(new RecentChangesInput { Start = start, SortAscending = newer, MaxItems = count });

		/// <summary>Adds recent changes pages from a specific user, or excluding that user, to the collection.</summary>
		/// <param name="user">The user.</param>
		/// <param name="exclude">if set to <c>true</c> returns changes by everyone other than the user.</param>
		public void AddRecentChanges(string user, bool exclude) => this.AddRecentChanges(new RecentChangesInput { User = user, ExcludeUser = exclude });

		/// <summary>Adds recent changes pages to the collection based on complex criteria.</summary>
		/// <param name="options">The options to be applied to the results.</param>
		public void AddRecentChanges(RecentChangesOptions options)
		{
			ThrowNull(options, nameof(options));
			this.AddRecentChanges(options.ToWallEInput);
		}

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		public void AddRedirectsToNamespace(int ns) => this.AddRedirectsToNamespace(new AllRedirectsInput { Namespace = ns });

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="prefix">The prefix of the pages to load.</param>
		public void AddRedirectsToNamespace(int ns, string prefix) => this.AddRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, Prefix = prefix });

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="from">The page to start at (inclusive). The page specified does not have to exist.</param>
		/// <param name="to">The page to stop at (inclusive). The page specified does not have to exist.</param>
		public void AddRedirectsToNamespace(int ns, string from, string to) => this.AddRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, From = from, To = to });

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <c>true</c>, revisions from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		public void AddRevisions(DateTime start, bool newer) => this.AddRevisions(start, newer, 0);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <c>true</c>, revisions from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		/// <param name="count">The number of revisions to return.</param>
		public void AddRevisions(DateTime start, bool newer, int count) => this.AddRevisions(new AllRevisionsInput { Start = start, SortAscending = newer, MaxItems = count });

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		public void AddSearchResults(string search) => this.AddSearchResults(new SearchInput(search) { Properties = SearchProperties.None });

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		/// <param name="namespaces">The namespaces to search in.</param>
		public void AddSearchResults(string search, IEnumerable<int> namespaces) => this.AddSearchResults(new SearchInput(search) { Namespaces = namespaces, Properties = SearchProperties.None });

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		/// <param name="whatToSearch">Whether to search the title, text, or use a near-match search.</param>
		/// <remarks>Not all search engines support all <paramref name="whatToSearch"/> options.</remarks>
		public void AddSearchResults(string search, WhatToSearch whatToSearch) => this.AddSearchResults(new SearchInput(search) { What = whatToSearch, Properties = SearchProperties.None });

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		/// <param name="whatToSearch">Whether to search the title, text, or use a near-match search.</param>
		/// <param name="namespaces">The namespaces to search in.</param>
		/// <remarks>Not all search engines support all <paramref name="whatToSearch"/> options.</remarks>
		public void AddSearchResults(string search, WhatToSearch whatToSearch, IEnumerable<int> namespaces) => this.AddSearchResults(new SearchInput(search) { Namespaces = namespaces, What = whatToSearch, Properties = SearchProperties.None });

		/// <summary>Adds all pages with transclusions to the collection.</summary>
		/// <remarks>Note that the templates do not have to exist; only the transclusion itself needs to exist. Similarly, a template that has no transclusions at all would not appear in the results.</remarks>
		public void AddTransclusions() => this.AddTransclusions(new AllTransclusionsInput());

		/// <summary>Adds all pages with  transclusions in the given namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <remarks>Unlike other namespace-specific methods, the namespace for this method applies to the transclusions to search for, <em>not</em> the pages to return. For example, a namespace value of 0 would find all transclusions of main-space pages, even if the transclusion itself is in Help space, for instance. Note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		public void AddTransclusions(int ns) => this.AddTransclusions(new AllTransclusionsInput { Namespace = ns });

		/// <summary>Adds pages with transclusions that begin with the given prefix to the collection.</summary>
		/// <param name="prefix">The prefix of the template transclusions to include.</param>
		/// <remarks>Unlike other prefix methods, the prefix for this method applies to the template transclusion to search for, <em>not</em> the pages to return. For example, a prefix of "Unsigned" would find transclusions for all templates which start with "Unsigned", such as "Unsigned", "Unsigned2", "Unsinged IP", and so forth. Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		public void AddTransclusions(string prefix) => this.AddTransclusions(new AllTransclusionsInput { Prefix = prefix });

		/// <summary>Adds pages with transclusions that are in the given namespace and begin with the given prefix to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="prefix">The prefix of the template transclusions to include.</param>
		/// <remarks>Unlike other namespace and prefix methods, the namespace and prefix for this method apply to the template transclusion to search for, <em>not</em> the pages to return. For example, a namespace of 2 and prefix of "Robby" would find transclusions of all user pages for users with names starting with "Robby". Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		public void AddTransclusions(int ns, string prefix) => this.AddTransclusions(new AllTransclusionsInput { Namespace = ns, Prefix = prefix });

		/// <summary>Adds pages with transclusions within a certain range to the collection.</summary>
		/// <param name="from">The template to start at (inclusive). The template specified does not have to exist.</param>
		/// <param name="to">The template to stop at (inclusive). The template specified does not have to exist.</param>
		/// <remarks>Unlike other page-range methods, the range for this method applies to the template transclusion to search for, <em>not</em> the pages to return. For example, a range of "Uns" to "Unt" would find all "Unsigned" templates, as well as "Unstable" and "Unsure" templatea if there were transclusions to them. Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		public void AddTransclusions(string from, string to) => this.AddTransclusions(new AllTransclusionsInput { From = from, To = to });

		/// <summary>Adds pages with transclusions that are in the given namespace and within a certain range to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="from">The template to start at (inclusive). The template specified does not have to exist.</param>
		/// <param name="to">The template to stop at (inclusive). The template specified does not have to exist.</param>
		/// <remarks>Unlike other namespace and page-range methods, the namespace and range for this method apply to the template transclusion to search for, <em>not</em> the pages to return. For example, a namespace of 2 and a range of "Rob" to "Roc" would find transclusions of all user pages for users with names between "Rob" and "Roc". Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		public void AddTransclusions(int ns, string from, string to) => this.AddTransclusions(new AllTransclusionsInput { Namespace = ns, From = from, To = to });

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		// Only basic full-watchlist functionality is implemented because I don't think watchlists are commonly used by the type of bot this framework is geared towards. If more functionality is desired, it's easy enough to add.
		public void AddWatchlistChanged() => this.AddWatchlistChanged(new WatchlistInput());

		/// <summary>Adds changed watchlist pages to the collection for a specific user, given their watchlist token.</summary>
		/// <param name="owner">The watchlist owner.</param>
		/// <param name="token">The watchlist token.</param>
		public void AddWatchlistChanged(string owner, string token) => this.AddWatchlistChanged(new WatchlistInput { Owner = owner, Token = token });

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		public void AddWatchlistRaw() => this.AddWatchlistRaw(new WatchlistRawInput());

		/// <summary>Adds raw watchlist pages to the collection for a specific user, given their watchlist token.</summary>
		/// <param name="owner">The watchlist owner.</param>
		/// <param name="token">The watchlist token.</param>
		public void AddWatchlistRaw(string owner, string token) => this.AddWatchlistRaw(new WatchlistRawInput { Owner = owner, Token = token });

		/// <summary>Determines whether the <see cref="TitleCollection">collection</see> contains a specific value.</summary>
		/// <param name="item">The object to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if <paramref name="item" /> is found in the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />.</returns>
		public bool Contains(TTitle item) => this.dictionary.ContainsKey(item?.Key);

		/// <summary>Determines whether the collection contains an item with the specified key.</summary>
		/// <param name="key">The key to search for.</param>
		/// <returns><see langword="true" /> if the collection contains an item with the specified key; otherwise, <see langword="true" />.</returns>
		public bool Contains(string key) => this.dictionary.ContainsKey(key);

		/// <summary>Copies the elements of the <see cref="TitleCollection">collection</see> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="TitleCollection">collection</see>. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(TTitle[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex);

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(IEnumerable<Namespace> namespaces)
		{
			var hash = new HashSet<Namespace>(namespaces);
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (!hash.Contains(this[i].Namespace))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(params Namespace[] namespaces) => this.FilterToNamespaces(namespaces as IEnumerable<Namespace>);

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(IEnumerable<int> namespaces)
		{
			var hash = new HashSet<int>(namespaces);
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (!hash.Contains(this[i].Namespace.Id))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(params int[] namespaces) => this.FilterToNamespaces(namespaces as IEnumerable<int>);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<TTitle> GetEnumerator() => this.items.GetEnumerator();

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

		/// <summary>Determines the index of a specific item in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		public int IndexOf(TTitle item) => this.IndexOf(item?.Key);

		/// <summary>Determines the index of a specific item in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="key">The key of the item to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns>The index of the item with the specified <paramref name="key" /> if found in the list; otherwise, -1.</returns>
		public int IndexOf(string key)
		{
			// ContainsKey is O(1), so check to make sure item exists before iterating the collection.
			if (this.dictionary.ContainsKey(key))
			{
				for (var i = 0; i < this.items.Count; i++)
				{
					if (this[i].Key == key)
					{
						return i;
					}
				}
			}

			return -1;
		}

		/// <summary>Inserts an item into the <see cref="TitleCollection">collection</see> at the specified index.</summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The item to insert into the <see cref="TitleCollection">collection</see>.</param>
		public void Insert(int index, TTitle item) => this.InsertItem(index, item);

		/// <summary>Removes a specific item from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to remove from the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="TitleCollection">collection</see>.</returns>
		public bool Remove(TTitle item) => this.Remove(item?.Key);

		/// <summary>Removes the item with the specified key from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="key">The key of the item to remove from the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if and item with the specified <paramref name="key" /> was successfully removed from the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if an item with the specified <paramref name="key" /> is not found in the original <see cref="TitleCollection">collection</see>.</returns>
		public bool Remove(string key)
		{
			ThrowNull(key, nameof(key));
			var index = this.IndexOf(key);
			if (index == -1)
			{
				return false;
			}

			this.RemoveItem(index);
			return true;
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(IEnumerable<Namespace> namespaces)
		{
			var hash = new HashSet<Namespace>(namespaces);
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (hash.Contains(this[i].Namespace))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(params Namespace[] namespaces) => this.RemoveNamespaces(namespaces as IEnumerable<Namespace>);

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(IEnumerable<int> namespaces)
		{
			var hash = new HashSet<int>(namespaces);
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (hash.Contains(this[i].Namespace.Id))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(params int[] namespaces) => this.RemoveNamespaces(namespaces as IEnumerable<int>);

		/// <summary>Removes the <see cref="TitleCollection">collection</see> item at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index) => this.RemoveItem(index);

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> by namespace, then pagename.</summary>
		public void Sort() => this.Sort(new SimpleTitleComparer());

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> using the specified <see cref="T:System.Comparison{T}" />.</summary>
		/// <param name="comparison">The comparison.</param>
		public void Sort(Comparison<ISimpleTitle> comparison) => (this.items as List<ISimpleTitle>).Sort(comparison);

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> using the specified <see cref="T:System.IComparer{T}" />.</summary>
		/// <param name="comparer">The comparer.</param>
		public void Sort(IComparer<ISimpleTitle> comparer) => (this.items as List<ISimpleTitle>).Sort(comparer);

		/// <summary>Enumerates the page names of the collection.</summary>
		/// <returns>The page names of the collection as full page names.</returns>
		public IEnumerable<string> ToStringEnumerable() => this.ToStringEnumerable(MediaWikiNamespaces.Main);

		/// <summary>Enumerates the page names of the collection assuming a specific namespace.</summary>
		/// <param name="ns">The namespace ID.</param>
		/// <returns>The page names of the collection assuming that no namespace is equivalent to the provided namespace (as in template calls).</returns>
		public IEnumerable<string> ToStringEnumerable(int ns)
		{
			foreach (var title in this)
			{
				yield return title.Namespace.AssumedName(ns) + title.PageName;
			}
		}

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public bool TryGetValue(string key, out TTitle value) => this.dictionary.TryGetValue(key, out value);
		#endregion

		#region Public Abstract Methods

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles to add.</param>
		public abstract void Add(IEnumerable<string> titles);

		/// <summary>Adds the specified titles to the collection, coercing them to the given namespace.</summary>
		/// <param name="defaultNamespace">The namespace.</param>
		/// <param name="titles">The titles to add, with or without the leading namespace text.</param>
		public abstract void Add(int defaultNamespace, IEnumerable<string> titles);

		/// <summary>Adds pages to the collection from their revision IDs.</summary>
		/// <param name="revisionIds">The revision IDs.</param>
		public abstract void AddRevisionIds(IEnumerable<long> revisionIds);
		#endregion

		#region Public Virtual Methods

		/// <summary>Removes all items from the <see cref="TitleCollection">collection</see>.</summary>
		public virtual void Clear()
		{
			this.items.Clear();
			this.dictionary.Clear();
		}
		#endregion

		#region Protected Override Methods

		/// <summary>Inserts an item into the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item.</param>
		/// <exception cref="T:System.InvalidOperationException">The item's site does not match the collection's site.</exception>
		/// <remarks>This method underlies the <see cref="M:Insert(int, TTitle)" /> method and, like <see cref="M:System.Collections.ObjectModel.Collection{T}.InsertItem(int, T)" />, can be overridden in derived classes.</remarks>
		protected virtual void InsertItem(int index, TTitle item)
		{
			ThrowNull(item, nameof(item));
			if (item.Namespace.Site != this.Site)
			{
				throw new InvalidOperationException(CurrentCulture(InvalidSite));
			}

			this.dictionary[item.Key] = item;
			this.items.Insert(index, item);
		}

		/// <summary>Removes the item at a specific index in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="index">The index of the item to remove.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is equal to or higher than the number of items in the collection.</exception>
		/// <remarks>This method underlies the <see cref="M:RobinHood70.Robby.TitleCollection{TTitle}.RemoveAt(int)" /> method and, like <see cref="M:System.Collections.ObjectModel.Collection{T}.RemoveItem(int)" />, can be overridden in derived classes.</remarks>
		protected virtual void RemoveItem(int index)
		{
			if (index >= this.items.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			this.dictionary.Remove(this.items[index].Key);
			this.items.RemoveAt(index);
		}
		#endregion

		#region Protected Abstract Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddBacklinks(BacklinksInput input);

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddCategories(AllCategoriesInput input);

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <c>true</c> load the entire category tree recursively.</param>
		protected abstract void AddCategoryMembers(CategoryMembersInput input, bool recurse);

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected abstract void AddDuplicateFiles(DuplicateFilesInput input, IEnumerable<ISimpleTitle> titles);

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddFiles(AllImagesInput input);

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddFileUsage(AllFileUsagesInput input);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected abstract void AddFileUsage(FileUsageInput input, IEnumerable<ISimpleTitle> titles);

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddLinksToNamespace(AllLinksInput input);

		/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddNamespace(AllPagesInput input);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected abstract void AddPageCategories(CategoriesInput input, IEnumerable<ISimpleTitle> titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected abstract void AddPageLinks(LinksInput input, IEnumerable<ISimpleTitle> titles);

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddPagesWithProperty(PagesWithPropertyInput input);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected abstract void AddPageTransclusions(TemplatesInput input, IEnumerable<ISimpleTitle> titles);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddPrefixSearchResults(PrefixSearchInput input);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected abstract void AddQueryPage(QueryPageInput input);

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddRecentChanges(RecentChangesInput input);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddRedirectsToNamespace(AllRedirectsInput input);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddRevisions(AllRevisionsInput input);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddSearchResults(SearchInput input);

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddTransclusions(AllTransclusionsInput input);

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddWatchlistChanged(WatchlistInput input);

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void AddWatchlistRaw(WatchlistRawInput input);
		#endregion
	}
}