namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	// TODO: Consider rewriting to use some kind of dictionary lookup all around...something like Dictionary<Title, int offset> and List<Title>. If List<Title> returns null, it's treated as a NotFoundException. Then have an Optimize or TrimExcess type thing to remove all null values and renumber. The idea here is that Remove operations could become O(log n) instead of O(n), although Removes should be the exception, so there's an argument to leave things as is, too.
	#region Public Enumerations

	/// <summary>Specifies how to limit any pages added to the collection.</summary>
	public enum LimitationType
	{
		/// <summary>Ignore all limitations.</summary>
		None,

		/// <summary>Disallow pages with namespaces specified in <see cref="TitleCollection{TTitle}.NamespaceLimitations"/> from the collection.</summary>
		Disallow,

		/// <summary>Automatically limit pages in the collection to only those with namespaces specified in <see cref="TitleCollection{TTitle}.NamespaceLimitations"/>.</summary>
		OnlyAllow,
	}

	/// <summary>The page protection types.</summary>
	[Flags]
	public enum ProtectionLevels
	{
		/// <summary>No protection level filtering.</summary>
		None = 0,

		/// <summary>Pages that are limited to autoconfirmed or higher access.</summary>
		AutoConfirmed = 1,

		/// <summary>Pages that are limited to sysop access.</summary>
		Sysop = 1 << 1,

		/// <summary>All protected pages.</summary>
		All = AutoConfirmed | Sysop
	}

	/// <summary>The page protection types.</summary>
	[Flags]
	public enum ProtectionTypes
	{
		/// <summary>No protection type filtering.</summary>
		None = 0,

		/// <summary>Pages that are Edit protected.</summary>
		Edit = 1,

		/// <summary>Pages that are Move protected.</summary>
		Move = 1 << 1,

		/// <summary>Pages that are Upload protected.</summary>
		Upload = 1 << 2,

		/// <summary>All protected pages.</summary>
		All = Edit | Move | Upload
	}
	#endregion

	/// <summary>Provides a base class to manipulate a collection of titles.</summary>
	/// <typeparam name="TTitle">The type of the title.</typeparam>
	/// <seealso cref="IList{TTitle}" />
	/// <seealso cref="IReadOnlyCollection{TTitle}" />
	/// <remarks>This collection class functions similarly to a KeyedCollection. Unlike a KeyedCollection, however, new items will automatically overwrite previous ones rather than throwing an error. TitleCollection also does not support changing an item's key. You must use Remove/Add in combination.</remarks>
	public abstract class TitleCollection<TTitle> : IList<TTitle>, IReadOnlyCollection<TTitle>, ISiteSpecific, IWikiData
		where TTitle : Title
	{
		#region Fields
		private readonly List<TTitle> items = new();
		private readonly Dictionary<Title, TTitle> lookup;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleCollection{TTitle}" /> class.</summary>
		/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
		protected TitleCollection([NotNull, ValidatedNotNull] Site site)
			: this(site, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection{TTitle}" /> class.</summary>
		/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
		/// <param name="equalityComparer">The <see cref="IEqualityComparer{T}"/> to use for lookups.</param>
		protected TitleCollection([NotNull, ValidatedNotNull] Site site, IEqualityComparer<Title>? equalityComparer)
		{
			this.Site = site.NotNull();
			this.lookup = new Dictionary<Title, TTitle>(equalityComparer ?? SimpleTitleComparer.Instance);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the number of elements contained in the <see cref="TitleCollection">collection</see>.</summary>
		/// <value>The number of elements contained in the <see cref="TitleCollection">collection</see>.</value>
		public int Count => this.items.Count;

		/// <summary>Gets a value indicating whether the <see cref="TitleCollection">collection</see> is read-only.</summary>
		/// <value><see langword="true"/> if the collection is read-only.</value>
		public bool IsReadOnly => false;

		/// <summary>Gets the site for the collection.</summary>
		/// <value>The site.</value>
		public Site Site { get; }
		#endregion

		#region Protected Properties

		/// <summary>Gets or sets a value indicating whether <see cref="NamespaceLimitations"/> specifies namespaces to be removed from the collection or only allowing those namepaces.</summary>
		/// <value>The type of the namespace limitation.</value>
		/// <remarks>Changing this property only affects newly added pages and does not affect any existing items in the collection. Use <see cref="FilterByLimitationRules"/> to do so, if needed.</remarks>
		protected LimitationType LimitationType { get; set; } = LimitationType.Disallow;

		/// <summary>Gets the namespace limitations.</summary>
		/// <value>A set of namespace IDs that will be filtered out or filtered down to automatically as pages are added.</value>
		/// <remarks>Changing the contents of this collection only affects newly added pages and does not affect any existing items in the collection. Use <see cref="FilterByLimitationRules"/> to do so, if needed.</remarks>
		protected ICollection<int> NamespaceLimitations { get; } = new HashSet<int>
		{
			MediaWikiNamespaces.Media,
			MediaWikiNamespaces.MediaWiki,
			MediaWikiNamespaces.Special,
			MediaWikiNamespaces.Template,
			MediaWikiNamespaces.User,
		};
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="Title">Title</see> at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="Title">Title</see> at the specified index.</returns>
		public TTitle this[int index]
		{
			get => this.items[index];
			set
			{
				this.items[index] = value.NotNull();
				this.lookup[value] = value;
			}
		}

		/// <summary>Gets or sets the <see cref="Title">Title</see> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The <see cref="Title">Title</see>.</returns>
		/// <remarks>Like a <see cref="Dictionary{TKey, TValue}"/>, this indexer will add a new entry on set if the requested entry isn't found.</remarks>
		public virtual TTitle this[string key]
		{
			get => this[TitleFactory.FromUnvalidated(this.Site, key.NotNull())];
			set
			{
				var titleKey = TitleFactory.FromUnvalidated(this.Site, key.NotNull());
				if (this.lookup.ContainsKey(titleKey))
				{
					var index = this.IndexOf(titleKey);
					this.items[index] = value;
				}
				else
				{
					this.items.Add(value);
				}

				this.lookup[value] = value;
			}
		}

		/// <summary>Gets or sets the <see cref="Title"/> with the specified key.</summary>
		/// <param name="title">The key.</param>
		/// <returns>The <see cref="Title">Title</see>.</returns>
		/// <remarks>Like a <see cref="Dictionary{TKey, TValue}"/>, this indexer will add a new entry on set if the requested entry isn't found.</remarks>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="title"/> is null.</exception>
		public virtual TTitle this[Title title]
		{
			get => this.lookup[title.NotNull()];
			set
			{
				var index = this.IndexOf(title.NotNull());
				if (index < 0)
				{
					this.items.Add(value);
				}
				else
				{
					this.items[index] = value;
				}

				this.lookup[value] = value;
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Adds an item to the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to add to the <see cref="TitleCollection">collection</see>.</param>
		/// <summary>Inserts an item into the <see cref="TitleCollection">collection</see>.</summary>
		/// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
		/// <exception cref="ArgumentNullException">The item is null.</exception>
		/// <exception cref="InvalidOperationException">The item's site does not match the collection's site.</exception>
		/// <remarks>This method underlies all methods that insert pages into the collection, and can be overridden in derived classes.</remarks>
		public void Add(TTitle item) => this.InsertItem(this.items.Count, item);

		/// <summary>Adds multiple titles to the <see cref="TitleCollection">collection</see> at once.</summary>
		/// <param name="titles">The titles to add.</param>
		/// <remarks>This method is for convenience only. Unlike the equivalent <see cref="List{T}" /> function, it simply calls <see cref="Add(TTitle)" /> repeatedly and provides no performance benefit.</remarks>
		public void AddRange(IEnumerable<TTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.TryAdd(title);
				}
			}
		}

		/// <summary>Determines whether the <see cref="TitleCollection">collection</see> contains a specific value.</summary>
		/// <param name="item">The object to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if <paramref name="item" /> is found in the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
		public bool Contains(Title item) => this.lookup.ContainsKey(item.NotNull());

		/// <summary>Determines whether the <see cref="TitleCollection">collection</see> contains a specific value.</summary>
		/// <param name="item">The object to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if <paramref name="item" /> is found in the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
		public bool Contains(TTitle item) => this.lookup.ContainsKey(item.NotNull());

		/// <summary>Determines whether the collection contains an item with the specified key.</summary>
		/// <param name="key">The key to search for.</param>
		/// <returns><see langword="true" /> if the collection contains an item with the specified key; otherwise, <see langword="true" />.</returns>
		public bool Contains(string key)
		{
			var title = TitleFactory.FromUnvalidated(this.Site, key.NotNull());
			return this.lookup.ContainsKey(title);
		}

		/// <summary>Copies the elements of the <see cref="TitleCollection">collection</see> to an <see cref="Array" />, starting at a particular <see cref="Array" /> index.</summary>
		/// <param name="array">The one-dimensional <see cref="Array" /> that is the destination of the elements copied from <see cref="TitleCollection">collection</see>. The <see cref="Array" /> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(TTitle[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex);

		/// <summary>Reapplies the namespace limitations in <see cref="NamespaceLimitations"/> to the existing collection.</summary>
		public void FilterByLimitationRules()
		{
			if (this.LimitationType == LimitationType.Disallow)
			{
				this.RemoveNamespaces(this.NamespaceLimitations);
			}
			else if (this.LimitationType == LimitationType.OnlyAllow)
			{
				this.FilterToNamespaces(this.NamespaceLimitations);
			}
		}

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(IEnumerable<int> namespaces)
		{
			HashSet<int> hash = new(namespaces);
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

		/// <inheritdoc/>
		public void GetBacklinks(string title) => this.GetBacklinks(title, BacklinksTypes.Backlinks | BacklinksTypes.EmbeddedIn, true, Filter.Any);

		/// <inheritdoc/>
		public void GetBacklinks(string title, BacklinksTypes linkTypes) => this.GetBacklinks(title, linkTypes, true, Filter.Any);

		/// <inheritdoc/>
		public void GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles) => this.GetBacklinks(title, linkTypes, includeRedirectedTitles, Filter.Any);

		/// <inheritdoc/>
		public void GetBacklinks(string title, BacklinksTypes linkTypes, Filter redirects) => this.GetBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects });

		/// <inheritdoc/>
		public void GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects) => this.GetBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Redirect = includeRedirectedTitles });

		/// <inheritdoc/>
		public void GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects, int ns) => this.GetBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Namespace = ns, Redirect = includeRedirectedTitles });

		/// <inheritdoc/>
		public void GetCategories() => this.GetCategories(new AllCategoriesInput());

		/// <inheritdoc/>
		public void GetCategories(string prefix) => this.GetCategories(new AllCategoriesInput { Prefix = prefix });

		/// <inheritdoc/>
		public void GetCategories(string from, string to) => this.GetCategories(new AllCategoriesInput { From = from, To = to });

		/// <inheritdoc/>
		public void GetCategoryMembers(string category) => this.GetCategoryMembers(category, CategoryMemberTypes.All, null, null, false);

		/// <inheritdoc/>
		public void GetCategoryMembers(string category, bool recurse) => this.GetCategoryMembers(category, CategoryMemberTypes.All, null, null, recurse);

		/// <inheritdoc/>
		public void GetCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, bool recurse) => this.GetCategoryMembers(category, categoryMemberTypes, null, null, recurse);

		/// <inheritdoc/>
		public void GetCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, string? from, string? to, bool recurse)
		{
			var cat = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Category], category);
			CategoryMembersInput input = new(cat.FullPageName)
			{
				Properties = CategoryMembersProperties.Title,
				Type = categoryMemberTypes,
				StartSortKeyPrefix = from,
				EndSortKeyPrefix = to,
			};

			if (recurse)
			{
				input.Properties |= CategoryMembersProperties.Type;
				input.Type |= CategoryMemberTypes.Subcat;
			}

			this.GetCategoryMembers(input, recurse);
		}

		/// <inheritdoc/>
		public void GetDuplicateFiles(IEnumerable<Title> titles) => this.GetDuplicateFiles(titles, false);

		/// <inheritdoc/>
		public void GetDuplicateFiles(IEnumerable<Title> titles, bool localOnly) => this.GetDuplicateFiles(new DuplicateFilesInput() { LocalOnly = localOnly }, titles);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<TTitle> GetEnumerator() => this.items.GetEnumerator();

		/// <summary>Object-based enumerator.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

		/// <inheritdoc/>
		public void GetFiles(string user) => this.GetFiles(new AllImagesInput { User = user });

		/// <inheritdoc/>
		public void GetFiles(string from, string to) => this.GetFiles(new AllImagesInput { From = from, To = to });

		/// <inheritdoc/>
		public void GetFiles(DateTime start, DateTime end) => this.GetFiles(new AllImagesInput { Start = start, End = end });

		/// <inheritdoc/>
		public void GetFileUsage() => this.GetFileUsage(new AllFileUsagesInput { Unique = true });

		/// <inheritdoc/>
		public void GetFileUsage(string prefix) => this.GetFileUsage(new AllFileUsagesInput { Prefix = prefix, Unique = true });

		/// <inheritdoc/>
		public void GetFileUsage(string from, string to) => this.GetFileUsage(new AllFileUsagesInput { From = from, To = to, Unique = true });

		/// <inheritdoc/>
		public void GetFileUsage(IEnumerable<Title> titles) => this.GetFileUsage(new FileUsageInput(), titles);

		/// <inheritdoc/>
		public void GetFileUsage(IEnumerable<Title> titles, Filter redirects) => this.GetFileUsage(new FileUsageInput() { FilterRedirects = redirects }, titles);

		/// <inheritdoc/>
		public void GetFileUsage(IEnumerable<Title> titles, Filter redirects, IEnumerable<int> namespaces) => this.GetFileUsage(new FileUsageInput() { Namespaces = namespaces, FilterRedirects = redirects }, titles);

		/// <inheritdoc/>
		public void GetLinksToNamespace(int ns) => this.GetLinksToNamespace(new AllLinksInput { Namespace = ns });

		/// <inheritdoc/>
		public void GetLinksToNamespace(int ns, string prefix) => this.GetLinksToNamespace(new AllLinksInput { Namespace = ns, Prefix = prefix });

		/// <inheritdoc/>
		public void GetLinksToNamespace(int ns, string from, string to) => this.GetLinksToNamespace(new AllLinksInput { Namespace = ns, From = from, To = to });

		/// <inheritdoc/>
		public void GetNamespace(int ns) => this.GetPages(new AllPagesInput { Namespace = ns });

		/// <inheritdoc/>
		public void GetNamespace(int ns, Filter redirects) => this.GetPages(new AllPagesInput { FilterRedirects = redirects, Namespace = ns });

		/// <inheritdoc/>
		public void GetNamespace(int ns, Filter redirects, string prefix) => this.GetPages(new AllPagesInput { FilterRedirects = redirects, Namespace = ns, Prefix = prefix });

		/// <inheritdoc/>
		public void GetNamespace(int ns, Filter redirects, string from, string to) => this.GetPages(new AllPagesInput { FilterRedirects = redirects, From = from, Namespace = ns, To = to });

		/// <inheritdoc/>
		public void GetPageCategories(IEnumerable<Title> titles) => this.GetPageCategories(new CategoriesInput(), titles);

		/// <inheritdoc/>
		public void GetPageCategories(IEnumerable<Title> titles, Filter hidden) => this.GetPageCategories(new CategoriesInput { FilterHidden = hidden }, titles);

		/// <inheritdoc/>
		public void GetPageCategories(IEnumerable<Title> titles, Filter hidden, IEnumerable<string> limitTo) => this.GetPageCategories(new CategoriesInput { Categories = limitTo, FilterHidden = hidden }, titles);

		/// <inheritdoc/>
		public void GetPageLinks(IEnumerable<Title> titles) => this.GetPageLinks(titles, null);

		/// <inheritdoc/>
		public void GetPageLinks(IEnumerable<Title> titles, IEnumerable<int>? namespaces) => this.GetPageLinks(new LinksInput() { Namespaces = namespaces }, titles);

		/// <inheritdoc/>
		public void GetPageLinksHere(IEnumerable<Title> titles) => this.GetPageLinksHere(new LinksHereInput(), titles);

		/// <inheritdoc/>
		public void GetPagesWithProperty(string property) => this.GetPagesWithProperty(new PagesWithPropertyInput(property));

		/// <inheritdoc/>
		public void GetPageTranscludedIn(IEnumerable<Title> titles) => this.GetPageTranscludedIn(new TranscludedInInput(), titles);

		/// <inheritdoc/>
		public void GetPageTranscludedIn(IEnumerable<string> titles) => this.GetPageTranscludedIn(new TitleCollection(this.Site, titles));

		/// <inheritdoc/>
		public void GetPageTranscludedIn(params string[] titles) => this.GetPageTranscludedIn(titles as IEnumerable<string>);

		/// <inheritdoc/>
		public void GetPageTransclusions(IEnumerable<Title> titles) => this.GetPageTransclusions(new TemplatesInput(), titles);

		/// <inheritdoc/>
		public void GetPageTransclusions(IEnumerable<Title> titles, IEnumerable<string> limitTo) => this.GetPageTransclusions(new TemplatesInput() { Templates = limitTo }, titles);

		/// <inheritdoc/>
		public void GetPageTransclusions(IEnumerable<Title> titles, IEnumerable<int> namespaces) => this.GetPageTransclusions(new TemplatesInput() { Namespaces = namespaces }, titles);

		/// <inheritdoc/>
		public void GetPrefixSearchResults(string prefix) => this.GetPrefixSearchResults(new PrefixSearchInput(prefix));

		/// <inheritdoc/>
		public void GetPrefixSearchResults(string prefix, IEnumerable<int> namespaces) => this.GetPrefixSearchResults(new PrefixSearchInput(prefix) { Namespaces = namespaces });

		/// <inheritdoc/>
		public void GetProtectedPages() => this.GetProtectedPages(ProtectionTypes.All, ProtectionLevels.None);

		/// <inheritdoc/>
		public void GetProtectedPages(ProtectionTypes protectionTypes) => this.GetProtectedPages(protectionTypes, ProtectionLevels.None);

		/// <inheritdoc/>
		public void GetProtectedPages(ProtectionTypes protectionTypes, ProtectionLevels protectionLevels)
		{
			List<string> typeList = new();
			foreach (var protType in protectionTypes.GetUniqueFlags())
			{
				if (Enum.GetName(typeof(ProtectionTypes), protType) is string name)
				{
					typeList.Add(name.ToLowerInvariant());
				}
			}

			List<string> levelList = new();
			foreach (var protLevel in protectionLevels.GetUniqueFlags())
			{
				if (Enum.GetName(typeof(ProtectionTypes), protLevel) is string name)
				{
					levelList.Add(name.ToLowerInvariant());
				}
			}

			this.GetProtectedPages(typeList, levelList);
		}

		/// <inheritdoc/>
		public void GetProtectedPages(IEnumerable<string> protectionTypes, IEnumerable<string>? protectionLevels)
		{
			if (protectionTypes.NotNull().IsEmpty())
			{
				throw new InvalidOperationException("You must specify at least one value for protectionTypes");
			}

			this.GetPages(new AllPagesInput() { ProtectionTypes = protectionTypes, ProtectionLevels = protectionLevels });
		}

		/// <inheritdoc/>
		public void GetQueryPage(string page) => this.GetQueryPage(new QueryPageInput(page));

		/// <inheritdoc/>
		public void GetQueryPage(string page, IReadOnlyDictionary<string, string> parameters) => this.GetQueryPage(new QueryPageInput(page) { Parameters = parameters });

		/// <inheritdoc/>
		public void GetRandom(int numPages) => this.GetRandomPages(new RandomInput() { MaxItems = numPages });

		/// <inheritdoc/>
		public void GetRandom(int numPages, IEnumerable<int> namespaces) => this.GetRandomPages(new RandomInput() { MaxItems = numPages, Namespaces = namespaces });

		/// <inheritdoc/>
		public void GetRecentChanges() => this.GetRecentChanges(new RecentChangesInput());

		/// <inheritdoc/>
		public void GetRecentChanges(IEnumerable<int> namespaces) => this.GetRecentChanges(new RecentChangesInput { Namespaces = namespaces, });

		/// <inheritdoc/>
		public void GetRecentChanges(string tag) => this.GetRecentChanges(new RecentChangesInput { Tag = tag });

		/// <inheritdoc/>
		public void GetRecentChanges(RecentChangesTypes types) => this.GetRecentChanges(new RecentChangesInput { Types = types });

		/// <inheritdoc/>
		public void GetRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects) => this.GetRecentChanges(new RecentChangesInput { FilterAnonymous = anonymous, FilterBots = bots, FilterMinor = minor, FilterPatrolled = patrolled, FilterRedirects = redirects });

		/// <inheritdoc/>
		public void GetRecentChanges(DateTime? start, DateTime? end) => this.GetRecentChanges(new RecentChangesInput { Start = start, End = end });

		/// <inheritdoc/>
		public void GetRecentChanges(DateTime start, bool newer) => this.GetRecentChanges(start, newer, 0);

		/// <inheritdoc/>
		public void GetRecentChanges(DateTime start, bool newer, int count) => this.GetRecentChanges(new RecentChangesInput { Start = start, SortAscending = newer, MaxItems = count });

		/// <inheritdoc/>
		public void GetRecentChanges(string user, bool exclude) => this.GetRecentChanges(new RecentChangesInput { User = user, ExcludeUser = exclude });

		/// <inheritdoc/>
		public void GetRecentChanges(RecentChangesOptions options) => this.GetRecentChanges(options.NotNull().ToWallEInput);

		/// <inheritdoc/>
		public void GetRedirectsToNamespace(int ns) => this.GetRedirectsToNamespace(new AllRedirectsInput { Namespace = ns });

		/// <inheritdoc/>
		public void GetRedirectsToNamespace(int ns, string prefix) => this.GetRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, Prefix = prefix });

		/// <inheritdoc/>
		public void GetRedirectsToNamespace(int ns, string from, string to) => this.GetRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, From = from, To = to });

		/// <inheritdoc/>
		public void GetRevisions(DateTime start, bool newer) => this.GetRevisions(start, newer, 0);

		/// <inheritdoc/>
		public void GetRevisions(DateTime start, bool newer, int count) => this.GetRevisions(new AllRevisionsInput { Start = start, SortAscending = newer, MaxItems = count });

		/// <inheritdoc/>
		public void GetSearchResults(string search) => this.GetSearchResults(new SearchInput(search) { Properties = SearchProperties.None });

		/// <inheritdoc/>
		public void GetSearchResults(string search, IEnumerable<int> namespaces) => this.GetSearchResults(new SearchInput(search) { Namespaces = namespaces, Properties = SearchProperties.None });

		/// <inheritdoc/>
		public void GetSearchResults(string search, WhatToSearch whatToSearch) => this.GetSearchResults(new SearchInput(search) { What = whatToSearch, Properties = SearchProperties.None });

		/// <inheritdoc/>
		public void GetSearchResults(string search, WhatToSearch whatToSearch, IEnumerable<int> namespaces) => this.GetSearchResults(new SearchInput(search) { Namespaces = namespaces, What = whatToSearch, Properties = SearchProperties.None });

		/// <inheritdoc/>
		public void GetTransclusions() => this.GetTransclusions(new AllTransclusionsInput());

		/// <inheritdoc/>
		public void GetTransclusions(int ns) => this.GetTransclusions(new AllTransclusionsInput { Namespace = ns });

		/// <inheritdoc/>
		public void GetTransclusions(string prefix) => this.GetTransclusions(new AllTransclusionsInput { Prefix = prefix });

		/// <inheritdoc/>
		public void GetTransclusions(int ns, string prefix) => this.GetTransclusions(new AllTransclusionsInput { Namespace = ns, Prefix = prefix });

		/// <inheritdoc/>
		public void GetTransclusions(string from, string to) => this.GetTransclusions(new AllTransclusionsInput { From = from, To = to });

		/// <inheritdoc/>
		public void GetTransclusions(int ns, string from, string to) => this.GetTransclusions(new AllTransclusionsInput { Namespace = ns, From = from, To = to });

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		// Only basic full-watchlist functionality is implemented because I don't think watchlists are commonly used by the type of bot this framework is geared towards. If more functionality is desired, it's easy enough to add.
		public void GetWatchlistChanged() => this.GetWatchlistChanged(new WatchlistInput());

		/// <inheritdoc/>
		public void GetWatchlistChanged(string owner, string token) => this.GetWatchlistChanged(new WatchlistInput { Owner = owner, Token = token });

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		public void GetWatchlistRaw() => this.GetWatchlistRaw(new WatchlistRawInput());

		/// <inheritdoc/>
		public void GetWatchlistRaw(string owner, string token) => this.GetWatchlistRaw(new WatchlistRawInput { Owner = owner, Token = token });

		/// <summary>Determines the index of a specific item in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		public int IndexOf(TTitle item) => this.IndexOf(item as Title);

		/// <summary>Determines the index of a specific item in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the item could not be found.</exception>
		public int IndexOf(Title item)
		{
			// ContainsKey is O(1), so check to see if key exists; if not, iterate looking for Namespace/PageName match.
			if (this.lookup.ContainsKey(item.NotNull()))
			{
				for (var i = 0; i < this.items.Count; i++)
				{
					if (this.items[i].SimpleEquals(item))
					{
						return i;
					}
				}

				throw new KeyNotFoundException(Resources.DictionaryListOutOfSync);
			}

			return -1;
		}

		/// <summary>Determines the index of a specific item in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="key">The key of the item to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns>The index of the item with the specified <paramref name="key" /> if found in the list; otherwise, -1.</returns>
		public int IndexOf(string key) => this.IndexOf(TitleFactory.FromUnvalidated(this.Site, key.NotNull()));

		/// <summary>Inserts an item into the <see cref="TitleCollection">collection</see> at the specified index.</summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The item to insert into the <see cref="TitleCollection">collection</see>.</param>
		public void Insert(int index, TTitle item) => this.InsertItem(index, item);

		/// <summary>Removes a specific item from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to remove from the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="TitleCollection">collection</see>.</returns>
		public bool Remove(TTitle item) => this.Remove(item as Title);

		/// <summary>Removes the item with the specified key from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="key">The key of the item to remove from the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if and item with the specified <paramref name="key" /> was successfully removed from the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if an item with the specified <paramref name="key" /> is not found in the original <see cref="TitleCollection">collection</see>.</returns>
		public bool Remove(string key)
		{
			var title = TitleFactory.FromUnvalidated(this.Site, key.NotNull());
			return this.Remove(title);
		}

		/// <summary>Removes a specific item from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="item">The item to remove from the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="TitleCollection">collection</see>.</returns>
		public bool Remove(Title item)
		{
			if (this.lookup.Remove(item))
			{
				for (var i = 0; i < this.items.Count; i++)
				{
					if (this.items[i].SimpleEquals(item))
					{
						this.items.RemoveAt(i);
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>Removes a series of items from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="titles">The titless to remove.</param>
		/// <returns><see langword="true" /> if any of the <paramref name="titles" /> were removed; otherwise, <see langword="false" />.</returns>
		public bool Remove(IEnumerable<Title> titles)
		{
			titles.ThrowNull();
			var removed = false;
			foreach (var item in titles)
			{
				removed |= this.Remove(item);
			}

			return removed;
		}

		/// <summary>Removes the <see cref="TitleCollection">collection</see> item at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			if (index >= this.items.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			this.lookup.Remove(this.items[index]);
			this.items.RemoveAt(index);
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(IEnumerable<int> namespaces) => this.RemoveNamespaces(false, namespaces);

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(params int[] namespaces) => this.RemoveNamespaces(false, namespaces as IEnumerable<int>);

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="removeTalk">Whether to remove talk spaces along with <paramref name="namespaces"/>.</param>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(bool removeTalk, IEnumerable<int>? namespaces)
		{
			if (namespaces is not HashSet<int> hash)
			{
				hash = new(namespaces ?? Array.Empty<int>());
			}

			for (var i = this.Count - 1; i >= 0; i--)
			{
				var ns = this[i].Namespace;
				if ((removeTalk && ns.IsTalkSpace) || hash.Contains(ns.Id))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="removeTalk">Whether to remove talk spaces along with <paramref name="namespaces"/>.</param>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(bool removeTalk, params int[] namespaces) => this.RemoveNamespaces(removeTalk, namespaces as IEnumerable<int>);

		/// <summary>Removes all talk spaces from the collection.</summary>
		public void RemoveTalkNamespaces() => this.RemoveNamespaces(true, null as IEnumerable<int>);

		/// <summary>Sets namespace limitations for the Load() methods.</summary>
		/// <param name="limitationType">Type of the limitation.</param>
		/// <param name="namespaceLimitations">The namespace limitations to apply to the PageCollection returned.</param>
		/// <remarks>Limitations apply only to the current collection; result collections will inherently be unfiltered to allow for cross-namespace redirection. Filtering can be added to result collections after they are returned.</remarks>
		public void SetLimitations(LimitationType limitationType, params int[] namespaceLimitations) => this.SetLimitations(limitationType, namespaceLimitations as IEnumerable<int>);

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> by namespace, then pagename.</summary>
		public void Sort() => this.Sort(SimpleTitleComparer.Instance);

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> using the specified <see cref="Comparison{T}" />.</summary>
		/// <param name="comparison">The comparison.</param>
		public void Sort(Comparison<TTitle> comparison) => this.items.Sort(comparison);

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> using the specified <see cref="IComparer{T}" />.</summary>
		/// <param name="comparer">The comparer.</param>
		public void Sort(IComparer<TTitle> comparer) => this.items.Sort(comparer);

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

		/// <summary>Attempts to add the given item to the list, gracefully skipping the item if it's already present in the text.</summary>
		/// <param name="item">The item to try to add.</param>
		/// <returns><see langword="true"/> if the item was added; otherwise, <see langword="false"/>.</returns>
		public bool TryAdd(TTitle item)
		{
			if (this.Contains(item))
			{
				return false;
			}

			this.InsertItem(this.items.Count, item);
			return true;
		}

		/// <summary>Attempts to add the given item to the list, gracefully skipping the item if it's already present in the text.</summary>
		/// <param name="items">The items to try to add.</param>
		/// <returns><see langword="true"/> if any items were added; otherwise, <see langword="false"/>.</returns>
		public bool TryAdd(IEnumerable<TTitle> items)
		{
			ArgumentNullException.ThrowIfNull(items);
			var retval = false;
			foreach (var item in items)
			{
				retval |= this.TryAdd(item);
			}

			return retval;
		}

		/// <summary>Returns the requested value, or null if not found.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public TTitle? ValueOrDefault(Title key) => this.ValueOrDefault(key.NotNull().FullPageName);

		/// <summary>Returns the requested value, or null if not found.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public TTitle? ValueOrDefault(string key) =>
			key != null &&
			this.lookup.TryGetValue(TitleFactory.FromUnvalidated(this.Site, key.NotNull()), out var retval)
				? retval
				: null;
		#endregion

		#region Public Abstract Methods

		/// <summary>Adds pages returned by a custom generator.</summary>
		/// <param name="generatorInput">The generator input.</param>
		public abstract void GetCustomGenerator(IGeneratorInput generatorInput);

		/// <summary>Adds pages to the collection from their revision IDs.</summary>
		/// <param name="revisionIds">The revision IDs.</param>
		public abstract void GetRevisionIds(IEnumerable<long> revisionIds);
		#endregion

		#region Public Virtual Methods

		/// <summary>Removes all items from the <see cref="TitleCollection">collection</see>.</summary>
		public virtual void Clear()
		{
			this.items.Clear();
			this.lookup.Clear();
		}

		/// <summary>Sets the namespace limitations to new values, clearing out any previous limitations.</summary>
		/// <param name="limitationType">The type of namespace limitations to apply.</param>
		/// <param name="namespaceLimitations">The namespace limitations. If null, only the limitation type is applied; the namespace set will remain unchanged.</param>
		/// <remarks>If the <paramref name="namespaceLimitations"/> parameter is null, no changes will be made to either of the limitation properties. This allows current/default limitations to remain in place if needed.</remarks>
		public virtual void SetLimitations(LimitationType limitationType, IEnumerable<int>? namespaceLimitations)
		{
			this.LimitationType = limitationType;
			this.NamespaceLimitations.Clear();
			if (limitationType != LimitationType.None && namespaceLimitations != null)
			{
				this.NamespaceLimitations.AddRange(namespaceLimitations);
			}
		}

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public virtual bool TryGetValue(TTitle key, [MaybeNullWhen(false)] out TTitle value) => this.lookup.TryGetValue(key.NotNull(), out value);

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public virtual bool TryGetValue(Title key, [MaybeNullWhen(false)] out TTitle value) => this.lookup.TryGetValue(key.NotNull(), out value);

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public virtual bool TryGetValue(string key, [MaybeNullWhen(false)] out TTitle value)
		{
			var title = TitleFactory.FromUnvalidated(this.Site, key.NotNull());
			return this.lookup.TryGetValue(title, out value!);
		}
		#endregion

		#region Protected Methods

		/// <summary>Gets a value indicating whether the page title is within the collection's limitations.</summary>
		/// <param name="title">The title.</param>
		/// <returns><see langword="true"/> if the page is within the collection's limitations and can be added to it; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the <see cref="LimitationType"/> is not one of the recognized values.</exception>
		protected bool IsTitleInLimits(Title title) =>
			title != null &&
			this.LimitationType switch
			{
				LimitationType.None => true,
				LimitationType.Disallow => !this.NamespaceLimitations.Contains(title.Namespace.Id),
				LimitationType.OnlyAllow => this.NamespaceLimitations.Contains(title.Namespace.Id),
				_ => throw new ArgumentOutOfRangeException(Resources.InvalidLimitationType)
			};
		#endregion

		#region Protected Override Methods

		/// <summary>Inserts an item into the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
		/// <exception cref="ArgumentNullException">The item is null.</exception>
		/// <exception cref="InvalidOperationException">The item's site does not match the collection's site.</exception>
		/// <remarks>This method underlies all methods that insert pages into the collection, and can be overridden in derived classes.</remarks>
		protected virtual void InsertItem(int index, TTitle item)
		{
			ArgumentNullException.ThrowIfNull(item);
			if (item.Site != this.Site)
			{
				throw new InvalidOperationException(Resources.InvalidSite);
			}

			if (this.IsTitleInLimits(item))
			{
				this.lookup.Add(item, item);
				this.items.Insert(index, item);
			}
		}
		#endregion

		#region Protected Abstract Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetBacklinks(BacklinksInput input);

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetCategories(AllCategoriesInput input);

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <see langword="true"/> load the entire category tree recursively.</param>
		protected abstract void GetCategoryMembers(CategoryMembersInput input, bool recurse);

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected abstract void GetDuplicateFiles(DuplicateFilesInput input, IEnumerable<Title> titles);

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetFiles(AllImagesInput input);

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetFileUsage(AllFileUsagesInput input);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected abstract void GetFileUsage(FileUsageInput input, IEnumerable<Title> titles);

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetLinksToNamespace(AllLinksInput input);

		/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetPages(AllPagesInput input);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected abstract void GetPageCategories(CategoriesInput input, IEnumerable<Title> titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected abstract void GetPageLinks(LinksInput input, IEnumerable<Title> titles);

		/// <summary>Adds pages that link to the given pages.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected abstract void GetPageLinksHere(LinksHereInput input, IEnumerable<Title> titles);

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetPagesWithProperty(PagesWithPropertyInput input);

		/// <summary>Adds pages that transclude the given pages.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected abstract void GetPageTranscludedIn(TranscludedInInput input, IEnumerable<Title> titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected abstract void GetPageTransclusions(TemplatesInput input, IEnumerable<Title> titles);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetPrefixSearchResults(PrefixSearchInput input);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected abstract void GetQueryPage(QueryPageInput input);

		/// <summary>Gets a random set of pages from the wiki.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetRandomPages(RandomInput input);

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetRecentChanges(RecentChangesInput input);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetRedirectsToNamespace(AllRedirectsInput input);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetRevisions(AllRevisionsInput input);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetSearchResults(SearchInput input);

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetTransclusions(AllTransclusionsInput input);

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetWatchlistChanged(WatchlistInput input);

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected abstract void GetWatchlistRaw(WatchlistRawInput input);
		#endregion
	}
}