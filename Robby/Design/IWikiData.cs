namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	/// <summary>Includes all methods that a wiki data collector should provide.</summary>
	public interface IWikiData
	{
		#region Methods

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		void GetBacklinks(string title);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		void GetBacklinks(string title, BacklinksTypes linkTypes);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="includeRedirectedTitles">if set to <see langword="true"/>, pages linking to <paramref name="title"/> via a redirect will be included.</param>
		void GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="redirects">Whether or not to include redirects in the results.</param>
		void GetBacklinks(string title, BacklinksTypes linkTypes, Filter redirects);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="includeRedirectedTitles">if set to <see langword="true"/>, pages linking to <paramref name="title"/> via a redirect will be included.</param>
		/// <param name="redirects">Whether or not to include redirects in the results.</param>
		void GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects);

		/// <summary>Adds backlinks (aka, What Links Here) of the specified title to the collection.</summary>
		/// <param name="title">The title.</param>
		/// <param name="linkTypes">The link types of the pages to retrieve.</param>
		/// <param name="includeRedirectedTitles">if set to <see langword="true"/>, pages linking to <paramref name="title"/> via a redirect will be included.</param>
		/// <param name="redirects">Whether or not to include redirects in the results.</param>
		/// <param name="ns">The namespace to limit the results to.</param>
		void GetBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects, int ns);

		/// <summary>Adds a set of category pages to the collection.</summary>
		void GetCategories();

		/// <summary>Adds a set of category pages that start with the specified prefix to the collection.</summary>
		/// <param name="prefix">The prefix of the categories to load.</param>
		void GetCategories(string prefix);

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="from">The category to start at (inclusive). The category specified does not have to exist.</param>
		/// <param name="to">The category to stop at (inclusive). The category specified does not have to exist.</param>
		void GetCategories(string from, string to);

		/// <summary>Adds category members to the collection, not including subcategories.</summary>
		/// <param name="category">The category.</param>
		void GetCategoryMembers(string category);

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="category">The category.</param>
		/// <param name="recurse">if set to <see langword="true"/> recurses through subcategories; otherwise, subcategories will appear only as titles within the collection.</param>
		void GetCategoryMembers(string category, bool recurse);

		/// <summary>Adds category members of the specified type to the collection, potentially including subcategories and their members.</summary>
		/// <param name="category">The category.</param>
		/// <param name="categoryMemberTypes">The category member types to load.</param>
		/// <param name="recurse">if set to <see langword="true"/> recurses through subcategories.</param>
		void GetCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, bool recurse);

		/// <summary>Adds category members of the specified type and within the specified range to the collection, potentially including subcategories and their members.</summary>
		/// <param name="category">The category.</param>
		/// <param name="categoryMemberTypes">The category member types to load.</param>
		/// <param name="from">The category member to start at (inclusive). The member specified does not have to exist.</param>
		/// <param name="to">The category member to stop at (inclusive). The member specified does not have to exist.</param>
		/// <param name="recurse">if set to <see langword="true"/> recurses through subcategories.</param>
		/// <remarks>If subcategories are loaded, they will be limited to the <paramref name="categoryMemberTypes"/> requested. However, they will <em>not</em> be limited by the <paramref name="from"/> and <paramref name="to"/> parameters.</remarks>
		public void GetCategoryMembers(string category, CategoryMemberTypes categoryMemberTypes, string? from, string? to, bool recurse);

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="titles">The titles to find duplicates of.</param>
		void GetDuplicateFiles(IEnumerable<Title> titles);

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="titles">The titles to find duplicates of.</param>
		/// <param name="localOnly">if set to <see langword="true"/> [local only].</param>
		void GetDuplicateFiles(IEnumerable<Title> titles, bool localOnly);

		/// <summary>Adds files uploaded by the specified user to the collection.</summary>
		/// <param name="user">The user.</param>
		void GetFiles(string user);

		/// <summary>Adds a range of files to the collection.</summary>
		/// <param name="from">The file name to start at (inclusive).</param>
		/// <param name="to">The file name to end at (inclusive).</param>
		void GetFiles(string from, string to);

		/// <summary>Adds a range of files to the collection based on the most recent version.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="end">The date to end at (inclusive).</param>
		void GetFiles(DateTime start, DateTime end);

		/// <summary>Adds all files that are in use to the collection.</summary>
		void GetFileUsage();

		/// <summary>Adds in-use files that have a given prefix to the collection.</summary>
		/// <param name="prefix">The prefix of the files to load.</param>
		void GetFileUsage(string prefix);

		/// <summary>Adds a range of in-use files to the collection.</summary>
		/// <param name="from">The file name to start at (inclusive).</param>
		/// <param name="to">The file name to end at (inclusive).</param>
		void GetFileUsage(string from, string to);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="titles">The titles.</param>
		void GetFileUsage(IEnumerable<Title> titles);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="titles">The titles.</param>
		/// <param name="redirects">Filter for redirects.</param>
		void GetFileUsage(IEnumerable<Title> titles, Filter redirects);

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="titles">The titles.</param>
		/// <param name="redirects">Filter for redirects.</param>
		/// <param name="namespaces">The namespaces to limit results to.</param>
		void GetFileUsage(IEnumerable<Title> titles, Filter redirects, IEnumerable<int> namespaces);

		/// <summary>Adds pages that link to a given namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		void GetLinksToNamespace(int ns);

		/// <summary>Adds pages that link to a given namespace and begin with a certain prefix to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="prefix">The prefix of the pages to load.</param>
		void GetLinksToNamespace(int ns, string prefix);

		/// <summary>Adds pages that link to a given namespace within a given range to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="from">The page name to start at (inclusive).</param>
		/// <param name="to">The page name to end at (inclusive).</param>
		void GetLinksToNamespace(int ns, string from, string to);

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		void GetNamespace(int ns);

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="redirects">Whether or not to include pages that are redirects.</param>
		void GetNamespace(int ns, Filter redirects);

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="redirects">Whether or not to include pages that are redirects.</param>
		/// <param name="prefix">The prefix of the pages to load.</param>
		void GetNamespace(int ns, Filter redirects, string prefix);

		/// <summary>Adds pages in the given the namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="redirects">Whether or not to include pages that are redirects.</param>
		/// <param name="from">The page name to start at (inclusive).</param>
		/// <param name="to">The page name to end at (inclusive).</param>
		void GetNamespace(int ns, Filter redirects, string from, string to);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		void GetPageCategories(IEnumerable<Title> titles);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		/// <param name="hidden">Filter for hidden categories.</param>
		void GetPageCategories(IEnumerable<Title> titles, Filter hidden);

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		/// <param name="hidden">Filter for hidden categories.</param>
		/// <param name="limitTo">Limit the results to these categories.</param>
		void GetPageCategories(IEnumerable<Title> titles, Filter hidden, IEnumerable<string> limitTo);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		void GetPageLinks(IEnumerable<Title> titles);

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		/// <param name="namespaces">The namespaces to limit results to.</param>
		void GetPageLinks(IEnumerable<Title> titles, IEnumerable<int>? namespaces);

		/// <summary>Adds pages that link to the given titles to the collection.</summary>
		/// <param name="titles">The titles.</param>
		void GetPageLinksHere(IEnumerable<Title> titles);

		/// <summary>Adds pages with a given page property (e.g., notrail, breadCrumbTrail) to the collection.</summary>
		/// <param name="property">The property to find.</param>
		void GetPagesWithProperty(string property);

		/// <summary>Adds pages that transclude the given titles to the collection.</summary>
		/// <param name="titles">The titles.</param>
		void GetPageTranscludedIn(IEnumerable<Title> titles);

		/// <summary>Adds pages that transclude the given titles to the collection.</summary>
		/// <param name="titles">The titles.</param>
		void GetPageTranscludedIn(IEnumerable<string> titles);

		/// <summary>Adds pages that transclude the given titles to the collection.</summary>
		/// <param name="titles">The titles.</param>
		void GetPageTranscludedIn(params string[] titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		void GetPageTransclusions(IEnumerable<Title> titles);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		/// <param name="limitTo">Limit the results to these transclusions.</param>
		void GetPageTransclusions(IEnumerable<Title> titles, IEnumerable<string> limitTo);

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		/// <param name="namespaces">Limit the results to these namespaces.</param>
		void GetPageTransclusions(IEnumerable<Title> titles, IEnumerable<int> namespaces);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="prefix">The prefix to search for.</param>
		/// <remarks>As noted on the API page for PrefixSearch, this is <em>not</em> the same as other prefix-based methods in that it doesn't strictly look for pages to start with the same literal letters. It's run through the installed search engine instead, and may include such things as word substitution, spelling correction, etc.</remarks>
		void GetPrefixSearchResults(string prefix);

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="prefix">The prefix to search for.</param>
		/// <param name="namespaces">The namespaces to search in.</param>
		/// <remarks>As noted on the API page for PrefixSearch, this is <em>not</em> the same as other prefix-based methods in that it doesn't strictly look for pages to start with the same literal letters. It's run through the installed search engine instead, and may include such things as word substitution, spelling correction, etc.</remarks>
		void GetPrefixSearchResults(string prefix, IEnumerable<int> namespaces);

		/// <summary>Adds protected page results to the collection.</summary>
		void GetProtectedPages();

		/// <summary>Adds protected page results to the collection.</summary>
		/// <param name="protectionTypes">The protection types to retrieve.</param>
		void GetProtectedPages(ProtectionTypes protectionTypes);

		/// <summary>Adds protected page results to the collection.</summary>
		/// <param name="protectionTypes">The protection types to retrieve.</param>
		/// <param name="protectionLevels">The levels to retrieve.</param>
		void GetProtectedPages(ProtectionTypes protectionTypes, ProtectionLevels protectionLevels);

		/// <summary>Adds protected page results to the collection.</summary>
		/// <param name="protectionTypes">The protection types to retrieve.</param>
		/// <param name="protectionLevels">The levels to retrieve.</param>
		/// <exception cref="InvalidOperationException">Thrown when there are no valid values for <paramref name="protectionLevels"/>.</exception>
		void GetProtectedPages(IEnumerable<string> protectionTypes, IEnumerable<string>? protectionLevels);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="page">The query-page-compatible module.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		void GetQueryPage(string page);

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="page">The query-page-compatible module.</param>
		/// <param name="parameters">The custom parameters to provide to the query page module.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		void GetQueryPage(string page, IReadOnlyDictionary<string, string> parameters);

		/// <summary>Adds a random set of pages to the collection.</summary>
		/// <param name="numPages">The number pages.</param>
		void GetRandom(int numPages);

		/// <summary>Adds a random set of pages from the specified namespaces to the collection.</summary>
		/// <param name="numPages">The number pages.</param>
		/// <param name="namespaces">The namespaces.</param>
		void GetRandom(int numPages, IEnumerable<int> namespaces);

		/// <summary>Adds all available recent changes pages to the collection.</summary>
		void GetRecentChanges();

		/// <summary>Adds recent changes pages to the collection, filtered to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to limit results to.</param>
		void GetRecentChanges(IEnumerable<int> namespaces);

		/// <summary>Adds recent changes pages to the collection, filtered to a specific tag.</summary>
		/// <param name="tag">A tag to limit results to.</param>
		void GetRecentChanges(string tag);

		/// <summary>Adds recent changes pages to the collection, filtered to the specified types of changes.</summary>
		/// <param name="types">The types of changes to limit results to.</param>
		void GetRecentChanges(RecentChangesTypes types);

		/// <summary>Adds recent changes pages to the collection, filtered based on properties of the change.</summary>
		/// <param name="anonymous">Include anonymous edits in the results.</param>
		/// <param name="bots">Include bot edits in the results.</param>
		/// <param name="minor">Include minor edits in the results.</param>
		/// <param name="patrolled">Include patrolled edits in the results.</param>
		/// <param name="redirects">Include redirects in the results.</param>
		void GetRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects);

		/// <summary>Adds recent changes pages to the collection, filtered to a date range.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="end">The date to end at (inclusive).</param>
		void GetRecentChanges(DateTime? start, DateTime? end);

		/// <summary>Adds recent changes pages to the collection starting at a given date and time and moving forward or backward from there.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <see langword="true"/>, changes from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		void GetRecentChanges(DateTime start, bool newer);

		/// <summary>Adds a specified number of recent changes pages to the collection starting at a given date and time and moving forward or backward from there.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <see langword="true"/>, changes from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		/// <param name="count">The number of changes to return.</param>
		void GetRecentChanges(DateTime start, bool newer, int count);

		/// <summary>Adds recent changes pages from a specific user, or excluding that user, to the collection.</summary>
		/// <param name="user">The user.</param>
		/// <param name="exclude">if set to <see langword="true"/> returns changes by everyone other than the user.</param>
		void GetRecentChanges(string user, bool exclude);

		/// <summary>Adds recent changes pages to the collection based on complex criteria.</summary>
		/// <param name="options">The options to be applied to the results.</param>
		void GetRecentChanges(RecentChangesOptions options);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		void GetRedirectsToNamespace(int ns);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="prefix">The prefix of the pages to load.</param>
		void GetRedirectsToNamespace(int ns, string prefix);

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="from">The page to start at (inclusive). The page specified does not have to exist.</param>
		/// <param name="to">The page to stop at (inclusive). The page specified does not have to exist.</param>
		void GetRedirectsToNamespace(int ns, string from, string to);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <see langword="true"/>, revisions from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		void GetRevisions(DateTime start, bool newer);

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="start">The date to start at (inclusive).</param>
		/// <param name="newer">if set to <see langword="true"/>, revisions from the start date to the most recent will be returned; otherwise, changes from the start date to the oldest will be returned.</param>
		/// <param name="count">The number of revisions to return.</param>
		void GetRevisions(DateTime start, bool newer, int count);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		void GetSearchResults(string search);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		/// <param name="namespaces">The namespaces to search in.</param>
		void GetSearchResults(string search, IEnumerable<int> namespaces);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		/// <param name="whatToSearch">Whether to search the title, text, or use a near-match search.</param>
		/// <remarks>Not all search engines support all <paramref name="whatToSearch"/> options.</remarks>
		void GetSearchResults(string search, WhatToSearch whatToSearch);

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="search">What to search for.</param>
		/// <param name="whatToSearch">Whether to search the title, text, or use a near-match search.</param>
		/// <param name="namespaces">The namespaces to search in.</param>
		/// <remarks>Not all search engines support all <paramref name="whatToSearch"/> options.</remarks>
		void GetSearchResults(string search, WhatToSearch whatToSearch, IEnumerable<int> namespaces);

		/// <summary>Adds all pages with transclusions to the collection.</summary>
		/// <remarks>Note that the templates do not have to exist; only the transclusion itself needs to exist. Similarly, a template that has no transclusions at all would not appear in the results.</remarks>
		void GetTransclusions();

		/// <summary>Adds all pages with transclusions in the given namespace to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <remarks>Unlike other namespace-specific methods, the namespace for this method applies to the transclusions to search for, <em>not</em> the pages to return. For example, a namespace value of 0 would find all transclusions of main-space pages, even if the transclusion itself is in Help space, for instance. Note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		void GetTransclusions(int ns);

		/// <summary>Adds pages with transclusions that begin with the given prefix to the collection.</summary>
		/// <param name="prefix">The prefix of the template transclusions to include.</param>
		/// <remarks>Unlike other prefix methods, the prefix for this method applies to the template transclusion to search for, <em>not</em> the pages to return. For example, a prefix of "Unsigned" would find transclusions for all templates which start with "Unsigned", such as "Unsigned", "Unsigned2", "Unsinged IP", and so forth. Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		void GetTransclusions(string prefix);

		/// <summary>Adds pages with transclusions that are in the given namespace and begin with the given prefix to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="prefix">The prefix of the template transclusions to include.</param>
		/// <remarks>Unlike other namespace and prefix methods, the namespace and prefix for this method apply to the template transclusion to search for, <em>not</em> the pages to return. For example, a namespace of 2 and prefix of "Robby" would find transclusions of all user pages for users with names starting with "Robby". Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		void GetTransclusions(int ns, string prefix);

		/// <summary>Adds pages with transclusions within a certain range to the collection.</summary>
		/// <param name="from">The template to start at (inclusive). The template specified does not have to exist.</param>
		/// <param name="to">The template to stop at (inclusive). The template specified does not have to exist.</param>
		/// <remarks>Unlike other page-range methods, the range for this method applies to the template transclusion to search for, <em>not</em> the pages to return. For example, a range of "Uns" to "Unt" would find all "Unsigned" templates, as well as "Unstable" and "Unsure" templatea if there were transclusions to them. Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		void GetTransclusions(string from, string to);

		/// <summary>Adds pages with transclusions that are in the given namespace and within a certain range to the collection.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="from">The template to start at (inclusive). The template specified does not have to exist.</param>
		/// <param name="to">The template to stop at (inclusive). The template specified does not have to exist.</param>
		/// <remarks>Unlike other namespace and page-range methods, the namespace and range for this method apply to the template transclusion to search for, <em>not</em> the pages to return. For example, a namespace of 2 and a range of "Rob" to "Roc" would find transclusions of all user pages for users with names between "Rob" and "Roc". Also note that the transcluded pages do not have to exist; only the transclusion itself needs to exist. Similarly, a page that has no transclusions at all would not appear in the results.</remarks>
		void GetTransclusions(int ns, string from, string to);

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		// Only basic full-watchlist functionality is implemented because I don't think watchlists are commonly used by the type of bot this framework is geared towards. If more functionality is desired, it's easy enough to add.
		void GetWatchlistChanged();

		/// <summary>Adds changed watchlist pages to the collection for a specific user, given their watchlist token.</summary>
		/// <param name="owner">The watchlist owner.</param>
		/// <param name="token">The watchlist token.</param>
		void GetWatchlistChanged(string owner, string token);

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		void GetWatchlistRaw();

		/// <summary>Adds raw watchlist pages to the collection for a specific user, given their watchlist token.</summary>
		/// <param name="owner">The watchlist owner.</param>
		/// <param name="token">The watchlist token.</param>
		void GetWatchlistRaw(string owner, string token);
		#endregion
	}
}