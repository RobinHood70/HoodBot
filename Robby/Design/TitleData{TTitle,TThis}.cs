namespace RobinHood70.Robby.Design;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
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
/// <typeparam name="TTitle">The type of the title.</typeparam>
/// <typeparam name="TThis">Concrete class for fluent interface.</typeparam>
/// <seealso cref="IList{TTitle}" />
/// <seealso cref="IReadOnlyCollection{TTitle}" />
/// <remarks>This collection class functions similarly to a KeyedCollection. Unlike a KeyedCollection, however, new items will automatically overwrite previous ones rather than throwing an error. TitleCollection also does not support changing an item's key. You must use Remove/Add in combination.</remarks>
/// <remarks>Initializes a new instance of the <see cref="TitleData{TTitle, TThis}" /> class.</remarks>
/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
public abstract class TitleData<TTitle, TThis>([NotNull, ValidatedNotNull] Site site) : TitleCollection<TTitle>(site), IWikiData<TThis>
	where TTitle : ITitle
	where TThis : TitleData<TTitle, TThis>, IWikiData<TThis>
{
	#region Public Methods

	/// <inheritdoc/>
	public TThis GetBacklinks(string title) => this.GetBacklinks(title, BacklinksTypes.Backlinks | BacklinksTypes.EmbeddedIn, true, Filter.Any);

	/// <inheritdoc/>
	public TThis GetBacklinks(string title, BacklinksTypes linkTypes) => this.GetBacklinks(title, linkTypes, true, Filter.Any);

	/// <inheritdoc/>
	public TThis GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles) => this.GetBacklinks(title, linkTypes, includeRedirectedTitles, Filter.Any);

	/// <inheritdoc/>
	public TThis GetBacklinks(string title, BacklinksTypes linkTypes, Filter redirects) => this.GetBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects });

	/// <inheritdoc/>
	public TThis GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects) => this.GetBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Redirect = includeRedirectedTitles });

	/// <inheritdoc/>
	public TThis GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects, int ns) => this.GetBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Namespace = ns, Redirect = includeRedirectedTitles });

	/// <inheritdoc/>
	public TThis GetCategories() => this.GetCategories(new AllCategoriesInput());

	/// <inheritdoc/>
	public TThis GetCategories(string prefix) => this.GetCategories(new AllCategoriesInput { Prefix = prefix });

	/// <inheritdoc/>
	public TThis GetCategories(string from, string to) => this.GetCategories(new AllCategoriesInput { From = from, To = to });

	/// <inheritdoc/>
	public TThis GetCategoryMembers(string category) => this.GetCategoryMembers(category, CategoryMemberTypes.All, null, null, false);

	/// <inheritdoc/>
	public TThis GetCategoryMembers(string category, bool recurse) => this.GetCategoryMembers(category, CategoryMemberTypes.All, null, null, recurse);

	/// <inheritdoc/>
	public TThis GetCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, bool recurse) => this.GetCategoryMembers(category, categoryMemberTypes, null, null, recurse);

	/// <inheritdoc/>
	public TThis GetCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, string? from, string? to, bool recurse)
	{
		// TODO: Rejig this so subcats are retrieved separately from everything else for recursion. Otherwise, output gets polluted with possibly-undesired categories.
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

		return this.GetCategoryMembers(input, recurse);
	}

	/// <inheritdoc/>
	public TThis GetDuplicateFiles(IEnumerable<Title> titles) => this.GetDuplicateFiles(titles, false);

	/// <inheritdoc/>
	public TThis GetDuplicateFiles(IEnumerable<Title> titles, bool localOnly) => this.GetDuplicateFiles(new DuplicateFilesInput() { LocalOnly = localOnly }, titles);

	/// <inheritdoc/>
	public TThis GetFiles(string user) => this.GetFiles(new AllImagesInput { User = user });

	/// <inheritdoc/>
	public TThis GetFiles(string from, string to) => this.GetFiles(new AllImagesInput { From = from, To = to });

	/// <inheritdoc/>
	public TThis GetFiles(DateTime start, DateTime end) => this.GetFiles(new AllImagesInput { Start = start, End = end });

	/// <inheritdoc/>
	public TThis GetFileUsage() => this.GetFileUsage(new AllFileUsagesInput { Unique = true });

	/// <inheritdoc/>
	public TThis GetFileUsage(string prefix) => this.GetFileUsage(new AllFileUsagesInput { Prefix = prefix, Unique = true });

	/// <inheritdoc/>
	public TThis GetFileUsage(string from, string to) => this.GetFileUsage(new AllFileUsagesInput { From = from, To = to, Unique = true });

	/// <inheritdoc/>
	public TThis GetFileUsage(IEnumerable<Title> titles) => this.GetFileUsage(new FileUsageInput(), titles);

	/// <inheritdoc/>
	public TThis GetFileUsage(IEnumerable<Title> titles, Filter redirects) => this.GetFileUsage(new FileUsageInput() { FilterRedirects = redirects }, titles);

	/// <inheritdoc/>
	public TThis GetFileUsage(IEnumerable<Title> titles, Filter redirects, IEnumerable<int> namespaces) => this.GetFileUsage(new FileUsageInput() { Namespaces = namespaces, FilterRedirects = redirects }, titles);

	/// <inheritdoc/>
	public TThis GetLinksToNamespace(int ns) => this.GetLinksToNamespace(new AllLinksInput { Namespace = ns });

	/// <inheritdoc/>
	public TThis GetLinksToNamespace(int ns, string prefix) => this.GetLinksToNamespace(new AllLinksInput { Namespace = ns, Prefix = prefix });

	/// <inheritdoc/>
	public TThis GetLinksToNamespace(int ns, string from, string to) => this.GetLinksToNamespace(new AllLinksInput { Namespace = ns, From = from, To = to });

	/// <inheritdoc/>
	public TThis GetNamespace(int ns) => this.GetPages(new AllPagesInput { Namespace = ns });

	/// <inheritdoc/>
	public TThis GetNamespace(int ns, Filter redirects) => this.GetPages(new AllPagesInput { FilterRedirects = redirects, Namespace = ns });

	/// <inheritdoc/>
	public TThis GetNamespace(int ns, Filter redirects, string prefix) => this.GetPages(new AllPagesInput { FilterRedirects = redirects, Namespace = ns, Prefix = prefix });

	/// <inheritdoc/>
	public TThis GetNamespace(int ns, Filter redirects, string from, string to) => this.GetPages(new AllPagesInput { FilterRedirects = redirects, From = from, Namespace = ns, To = to });

	/// <inheritdoc/>
	public TThis GetPageCategories(IEnumerable<Title> titles) => this.GetPageCategories(new CategoriesInput(), titles);

	/// <inheritdoc/>
	public TThis GetPageCategories(IEnumerable<Title> titles, Filter hidden) => this.GetPageCategories(new CategoriesInput { FilterHidden = hidden }, titles);

	/// <inheritdoc/>
	public TThis GetPageCategories(IEnumerable<Title> titles, Filter hidden, IEnumerable<string> limitTo) => this.GetPageCategories(new CategoriesInput { Categories = limitTo, FilterHidden = hidden }, titles);

	/// <inheritdoc/>
	public TThis GetPageLinks(IEnumerable<Title> titles) => this.GetPageLinks(titles, null);

	/// <inheritdoc/>
	public TThis GetPageLinks(IEnumerable<Title> titles, IEnumerable<int>? namespaces) => this.GetPageLinks(new LinksInput() { Namespaces = namespaces }, titles);

	/// <inheritdoc/>
	public TThis GetPageLinksHere(IEnumerable<Title> titles) => this.GetPageLinksHere(new LinksHereInput(), titles);

	/// <inheritdoc/>
	public TThis GetPagesWithProperty(string property) => this.GetPagesWithProperty(new PagesWithPropertyInput(property));

	/// <inheritdoc/>
	public TThis GetPageTranscludedIn(IEnumerable<Title> titles) => this.GetPageTranscludedIn(new TranscludedInInput(), titles);

	/// <inheritdoc/>
	public TThis GetPageTranscludedIn(IEnumerable<string> titles) => this.GetPageTranscludedIn(new TitleCollection(this.Site, titles));

	/// <inheritdoc/>
	public TThis GetPageTranscludedIn(params string[] titles) => this.GetPageTranscludedIn(titles as IEnumerable<string>);

	/// <inheritdoc/>
	public TThis GetPageTransclusions(IEnumerable<Title> titles) => this.GetPageTransclusions(new TemplatesInput(), titles);

	/// <inheritdoc/>
	public TThis GetPageTransclusions(IEnumerable<Title> titles, IEnumerable<string> limitTo) => this.GetPageTransclusions(new TemplatesInput() { Templates = limitTo }, titles);

	/// <inheritdoc/>
	public TThis GetPageTransclusions(IEnumerable<Title> titles, IEnumerable<int> namespaces) => this.GetPageTransclusions(new TemplatesInput() { Namespaces = namespaces }, titles);

	/// <inheritdoc/>
	public TThis GetPrefixSearchResults(string prefix) => this.GetPrefixSearchResults(new PrefixSearchInput(prefix));

	/// <inheritdoc/>
	public TThis GetPrefixSearchResults(string prefix, IEnumerable<int> namespaces) => this.GetPrefixSearchResults(new PrefixSearchInput(prefix) { Namespaces = namespaces });

	/// <inheritdoc/>
	public TThis GetProtectedPages() => this.GetProtectedPages(ProtectionTypes.All, ProtectionLevels.None);

	/// <inheritdoc/>
	public TThis GetProtectedPages(ProtectionTypes protectionTypes) => this.GetProtectedPages(protectionTypes, ProtectionLevels.None);

	/// <inheritdoc/>
	public TThis GetProtectedPages(ProtectionTypes protectionTypes, ProtectionLevels protectionLevels)
	{
		List<string> typeList = [];
		foreach (var protType in protectionTypes.GetUniqueFlags())
		{
			if (Enum.GetName(protType) is string name)
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

		return this.GetProtectedPages(typeList, levelList);
	}

	/// <inheritdoc/>
	public TThis GetProtectedPages(IEnumerable<string> protectionTypes, IEnumerable<string> protectionLevels)
	{
		ArgumentNullException.ThrowIfNull(protectionTypes);
		return protectionTypes.Any()
			? this.GetPages(new AllPagesInput() { ProtectionTypes = protectionTypes, ProtectionLevels = protectionLevels })
			: throw new InvalidOperationException("You must specify at least one value for protectionTypes");
	}

	/// <inheritdoc/>
	public TThis GetQueryPage(string page) => this.GetQueryPage(new QueryPageInput(page));

	/// <inheritdoc/>
	public TThis GetQueryPage(string page, IReadOnlyDictionary<string, string> parameters) => this.GetQueryPage(new QueryPageInput(page) { Parameters = parameters });

	/// <inheritdoc/>
	public TThis GetRandom(int numPages) => this.GetRandomPages(new RandomInput() { MaxItems = numPages });

	/// <inheritdoc/>
	public TThis GetRandom(int numPages, IEnumerable<int> namespaces) => this.GetRandomPages(new RandomInput() { MaxItems = numPages, Namespaces = namespaces });

	/// <inheritdoc/>
	public TThis GetRecentChanges() => this.GetRecentChanges(new RecentChangesInput());

	/// <inheritdoc/>
	public TThis GetRecentChanges(IEnumerable<int> namespaces) => this.GetRecentChanges(new RecentChangesInput { Namespaces = namespaces, });

	/// <inheritdoc/>
	public TThis GetRecentChanges(string tag) => this.GetRecentChanges(new RecentChangesInput { Tag = tag });

	/// <inheritdoc/>
	public TThis GetRecentChanges(RecentChangesTypes types) => this.GetRecentChanges(new RecentChangesInput { Types = types });

	/// <inheritdoc/>
	public TThis GetRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects) => this.GetRecentChanges(new RecentChangesInput { FilterAnonymous = anonymous, FilterBots = bots, FilterMinor = minor, FilterPatrolled = patrolled, FilterRedirects = redirects });

	/// <inheritdoc/>
	public TThis GetRecentChanges(DateTime? start, DateTime? end) => this.GetRecentChanges(new RecentChangesInput { Start = start, End = end });

	/// <inheritdoc/>
	public TThis GetRecentChanges(DateTime start, bool newer) => this.GetRecentChanges(start, newer, 0);

	/// <inheritdoc/>
	public TThis GetRecentChanges(DateTime start, bool newer, int count) => this.GetRecentChanges(new RecentChangesInput { Start = start, SortAscending = newer, MaxItems = count });

	/// <inheritdoc/>
	public TThis GetRecentChanges(string user, bool exclude) => this.GetRecentChanges(new RecentChangesInput { User = user, ExcludeUser = exclude });

	/// <inheritdoc/>
	public TThis GetRecentChanges(RecentChangesOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		return this.GetRecentChanges(options.ToWallEInput);
	}

	/// <inheritdoc/>
	public TThis GetRedirectsToNamespace(int ns) => this.GetRedirectsToNamespace(new AllRedirectsInput { Namespace = ns });

	/// <inheritdoc/>
	public TThis GetRedirectsToNamespace(int ns, string prefix) => this.GetRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, Prefix = prefix });

	/// <inheritdoc/>
	public TThis GetRedirectsToNamespace(int ns, string from, string to) => this.GetRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, From = from, To = to });

	/// <inheritdoc/>
	public TThis GetRevisions(DateTime start, bool newer) => this.GetRevisions(start, newer, 0);

	/// <inheritdoc/>
	public TThis GetRevisions(DateTime start, bool newer, int count) => this.GetRevisions(new AllRevisionsInput { Start = start, SortAscending = newer, MaxItems = count });

	/// <inheritdoc/>
	public TThis GetSearchResults(string search) => this.GetSearchResults(new SearchInput(search) { Properties = SearchProperties.None });

	/// <inheritdoc/>
	public TThis GetSearchResults(string search, IEnumerable<int> namespaces) => this.GetSearchResults(new SearchInput(search) { Namespaces = namespaces, Properties = SearchProperties.None });

	/// <inheritdoc/>
	public TThis GetSearchResults(string search, WhatToSearch whatToSearch) => this.GetSearchResults(new SearchInput(search) { What = whatToSearch, Properties = SearchProperties.None });

	/// <inheritdoc/>
	public TThis GetSearchResults(string search, WhatToSearch whatToSearch, IEnumerable<int> namespaces) => this.GetSearchResults(new SearchInput(search) { Namespaces = namespaces, What = whatToSearch, Properties = SearchProperties.None });

	/// <inheritdoc/>
	public TThis GetTransclusions() => this.GetTransclusions(new AllTransclusionsInput());

	/// <inheritdoc/>
	public TThis GetTransclusions(int ns) => this.GetTransclusions(new AllTransclusionsInput { Namespace = ns });

	/// <inheritdoc/>
	public TThis GetTransclusions(string prefix) => this.GetTransclusions(new AllTransclusionsInput { Prefix = prefix });

	/// <inheritdoc/>
	public TThis GetTransclusions(int ns, string prefix) => this.GetTransclusions(new AllTransclusionsInput { Namespace = ns, Prefix = prefix });

	/// <inheritdoc/>
	public TThis GetTransclusions(string from, string to) => this.GetTransclusions(new AllTransclusionsInput { From = from, To = to });

	/// <inheritdoc/>
	public TThis GetTransclusions(int ns, string from, string to) => this.GetTransclusions(new AllTransclusionsInput { Namespace = ns, From = from, To = to });

	/// <summary>Adds changed watchlist pages to the collection.</summary>
	// Only basic full-watchlist functionality is implemented because I don't think watchlists are commonly used by the type of bot this framework is geared towards. If more functionality is desired, it's easy enough to add.
	public TThis GetWatchlistChanged() => this.GetWatchlistChanged(new WatchlistInput());

	/// <inheritdoc/>
	public TThis GetWatchlistChanged(string owner, string token) => this.GetWatchlistChanged(new WatchlistInput { Owner = owner, Token = token });

	/// <summary>Adds raw watchlist pages to the collection.</summary>
	public TThis GetWatchlistRaw() => this.GetWatchlistRaw(new WatchlistRawInput());

	/// <inheritdoc/>
	public TThis GetWatchlistRaw(string owner, string token) => this.GetWatchlistRaw(new WatchlistRawInput { Owner = owner, Token = token });

	#endregion

	#region Public Abstract Methods

	/// <summary>Adds pages returned by a custom generator.</summary>
	/// <param name="generatorInput">The generator input.</param>
	public abstract TThis GetCustomGenerator(IGeneratorInput generatorInput);

	/// <summary>Adds pages to the collection from their revision IDs.</summary>
	/// <param name="revisionIds">The revision IDs.</param>
	public abstract TThis GetRevisionIds(IEnumerable<long> revisionIds);
	#endregion

	#region Protected Abstract Methods

	/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetBacklinks(BacklinksInput input);

	/// <summary>Adds a set of category pages to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetCategories(AllCategoriesInput input);

	/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="recurse">if set to <see langword="true"/> load the entire category tree recursively.</param>
	protected abstract TThis GetCategoryMembers(CategoryMembersInput input, bool recurse);

	/// <summary>Adds duplicate files of the given titles to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles to find duplicates of.</param>
	protected abstract TThis GetDuplicateFiles(DuplicateFilesInput input, IEnumerable<Title> titles);

	/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetFiles(AllImagesInput input);

	/// <summary>Adds files that are in use to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetFileUsage(AllFileUsagesInput input);

	/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles.</param>
	protected abstract TThis GetFileUsage(FileUsageInput input, IEnumerable<Title> titles);

	/// <summary>Adds pages that link to a given namespace.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetLinksToNamespace(AllLinksInput input);

	/// <summary>Adds pages from a given namespace to the collection. Parameters allow filtering to a specific range of pages.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetPages(AllPagesInput input);

	/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles whose categories should be loaded.</param>
	protected abstract TThis GetPageCategories(CategoriesInput input, IEnumerable<Title> titles);

	/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles whose categories should be loaded.</param>
	protected abstract TThis GetPageLinks(LinksInput input, IEnumerable<Title> titles);

	/// <summary>Adds pages that link to the given pages.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles.</param>
	protected abstract TThis GetPageLinksHere(LinksHereInput input, IEnumerable<Title> titles);

	/// <summary>Adds pages with a given property to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetPagesWithProperty(PagesWithPropertyInput input);

	/// <summary>Adds pages that transclude the given pages.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles.</param>
	protected abstract TThis GetPageTranscludedIn(TranscludedInInput input, IEnumerable<Title> titles);

	/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	/// <param name="titles">The titles whose transclusions should be loaded.</param>
	protected abstract TThis GetPageTransclusions(TemplatesInput input, IEnumerable<Title> titles);

	/// <summary>Adds prefix-search results to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetPrefixSearchResults(PrefixSearchInput input);

	/// <summary>Adds query page results to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
	protected abstract TThis GetQueryPage(QueryPageInput input);

	/// <summary>Gets a random set of pages from the wiki.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetRandomPages(RandomInput input);

	/// <summary>Adds recent changes pages to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetRecentChanges(RecentChangesInput input);

	/// <summary>Adds redirects to a namespace to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetRedirectsToNamespace(AllRedirectsInput input);

	/// <summary>Adds pages from a range of revisions to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetRevisions(AllRevisionsInput input);

	/// <summary>Adds search results to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetSearchResults(SearchInput input);

	/// <summary>Adds pages with template transclusions to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetTransclusions(AllTransclusionsInput input);

	/// <summary>Adds changed watchlist pages to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetWatchlistChanged(WatchlistInput input);

	/// <summary>Adds raw watchlist pages to the collection.</summary>
	/// <param name="input">The input parameters.</param>
	protected abstract TThis GetWatchlistRaw(WatchlistRawInput input);
	#endregion
}