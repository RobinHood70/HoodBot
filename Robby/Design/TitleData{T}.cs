namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

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
	/// <typeparam name="T">The type of the title.</typeparam>
	/// <seealso cref="IList{TTitle}" />
	/// <seealso cref="IReadOnlyCollection{TTitle}" />
	/// <remarks>This collection class functions similarly to a KeyedCollection. Unlike a KeyedCollection, however, new items will automatically overwrite previous ones rather than throwing an error. TitleCollection also does not support changing an item's key. You must use Remove/Add in combination.</remarks>
	/// <remarks>Initializes a new instance of the <see cref="TitleData{TTitle}" /> class.</remarks>
	/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
	public abstract class TitleData<T>([NotNull, ValidatedNotNull] Site site) : TitleCollection<T>(site), ISiteSpecific, IWikiData
		where T : ITitle
	{
		#region Public Methods

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
			Title cat = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Category], category);
			CategoryMembersInput input = new(cat.FullPageName())
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
			List<string> typeList = [];
			foreach (var protType in protectionTypes.GetUniqueFlags())
			{
				if (Enum.GetName(typeof(ProtectionTypes), protType) is string name)
				{
					typeList.Add(name.ToLowerInvariant());
				}
			}

			List<string> levelList = [];
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
		public void GetProtectedPages(IEnumerable<string> protectionTypes, IEnumerable<string> protectionLevels)
		{
			ArgumentNullException.ThrowIfNull(protectionTypes);
			if (protectionTypes.IsEmpty())
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
		public void GetRecentChanges(RecentChangesOptions options)
		{
			ArgumentNullException.ThrowIfNull(options);
			this.GetRecentChanges(options.ToWallEInput);
		}

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

		#endregion

		#region Public Abstract Methods

		/// <summary>Adds pages returned by a custom generator.</summary>
		/// <param name="generatorInput">The generator input.</param>
		public abstract void GetCustomGenerator(IGeneratorInput generatorInput);

		/// <summary>Adds pages to the collection from their revision IDs.</summary>
		/// <param name="revisionIds">The revision IDs.</param>
		public abstract void GetRevisionIds(IEnumerable<long> revisionIds);
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