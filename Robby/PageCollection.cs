namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using Design;
	using Pages;
	using WallE.Base;
	using WikiCommon;
	using static WikiCommon.Globals;

	public class PageCollection : TitleCollectionBase<Page>, IMessageSource
	{
		#region Fields
		private Dictionary<string, Title> titleMap = new Dictionary<string, Title>();
		#endregion

		#region Constructors
		public PageCollection(Site site)
			: this(site, null, null)
		{
		}

		public PageCollection(Site site, PageModules modules)
			: this(site, new PageLoadOptions(modules), null)
		{
		}

		public PageCollection(Site site, PageLoadOptions options)
			: this(site, options, null)
		{
		}

		public PageCollection(Site site, PageLoadOptions options, PageBuilderBase builder)
			: base(site)
		{
			this.LoadOptions = options ?? site.DefaultLoadOptions;
			this.PageBuilder = builder ?? site.PageBuilder;
		}
		#endregion

		#region Public Properties
		public PageLoadOptions LoadOptions { get; set; }

		public PageBuilderBase PageBuilder { get; set; }

		public IReadOnlyDictionary<string, Title> TitleMap => this.titleMap;
		#endregion

		#region Public Indexers
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

		#region Public Methods
		public void AddAllCategories() => this.FillFromPageSet(new AllCategoriesInput());

		public void AddAllCategories(string prefix) => this.FillFromPageSet(new AllCategoriesInput { Prefix = prefix });

		public void AddAllCategories(string from, string to) => this.FillFromPageSet(new AllCategoriesInput { From = from, To = to });

		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { Redirect = includeRedirectedTitles });

		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Redirect = includeRedirectedTitles });

		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects, int ns) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Namespace = ns, Redirect = includeRedirectedTitles });

		public void AddCategoryMembers(string category, bool recurse) => this.AddCategoryMembers(category, recurse, CategoryTypes.All);

		public void AddCategoryMembers(string category, bool recurse, CategoryTypes categoryTypes)
		{
			var cat = Title.ForcedNamespace(this.Site, MediaWikiNamespaces.Category, category);
			HashSet<Title> recursionSet = null;
			if (recurse)
			{
				recursionSet = new HashSet<Title>(new WikiTitleEqualityComparer());
			}

			this.AddCategoryMembers(new CategoryMembersInput(cat.FullPageName)
			{
				Type = categoryTypes,
			}, recursionSet);
		}

		public void AddCategoryMembers(string category, CategoryTypes categoryTypes, string fromPrefix, string toPrefix)
		{
			var cat = Title.ForcedNamespace(this.Site, MediaWikiNamespaces.Category, category);
			this.FillFromPageSet(new CategoryMembersInput(cat.FullPageName)
			{
				Type = categoryTypes,
				StartSortKeyPrefix = fromPrefix,
				EndSortKeyPrefix = toPrefix,
			});
		}

		public void AddDuplicateFiles(IEnumerable<string> titles) => this.FillFromPageSet(new DuplicateFilesInput(), new TitleCollection(this.Site, MediaWikiNamespaces.File, titles));

		public void AddDuplicateFiles(IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(new DuplicateFilesInput(), titles);

		public void AddFiles(string user) => this.FillFromPageSet(new AllImagesInput { User = user });

		public void AddFiles(string from, string to) => this.FillFromPageSet(new AllImagesInput { From = from, To = to });

		public void AddFiles(DateTime? start, DateTime? end) => this.FillFromPageSet(new AllImagesInput { Start = start, End = end });

		public void AddFileUsage() => this.FillFromPageSet(new AllFileUsagesInput());

		public void AddFileUsage(string prefix) => this.FillFromPageSet(new AllFileUsagesInput() { Prefix = prefix });

		public void AddFileUsage(string from, string to) => this.FillFromPageSet(new AllFileUsagesInput() { From = from, To = to });

		public void AddFileUsage(IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(new FileUsageInput(), titles);

		public void AddFileUsage(IEnumerable<IWikiTitle> titles, Filter redirects) => this.FillFromPageSet(new FileUsageInput() { FilterRedirects = redirects }, titles);

		public void AddFileUsage(IEnumerable<IWikiTitle> titles, Filter redirects, IEnumerable<int> namespaces) => this.FillFromPageSet(new FileUsageInput() { Namespaces = namespaces, FilterRedirects = redirects }, titles);

		public void AddFromLinksOnPage(IEnumerable<IWikiTitle> titles) => this.AddFromLinksOnPage(titles, null);

		public void AddFromLinksOnPage(IEnumerable<IWikiTitle> titles, IEnumerable<int> namespaces) => this.FillFromPageSet(new LinksInput() { Namespaces = namespaces }, titles);

		public void AddFromTransclusionsOnPage(IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(new TemplatesInput(), titles);

		public void AddFromTransclusionsOnPage(IEnumerable<IWikiTitle> titles, IEnumerable<string> onlyThese) => this.FillFromPageSet(new TemplatesInput() { Templates = onlyThese }, titles);

		public void AddFromTransclusionsOnPage(IEnumerable<IWikiTitle> titles, IEnumerable<int> namespaces) => this.FillFromPageSet(new TemplatesInput() { Namespaces = namespaces }, titles);

		public void AddLinksToNamespace(int ns) => this.FillFromPageSet(new AllLinksInput() { Namespace = ns });

		public void AddLinksToNamespace(int ns, string prefix) => this.FillFromPageSet(new AllLinksInput() { Namespace = ns, Prefix = prefix });

		public void AddLinksToNamespace(int ns, string from, string to) => this.FillFromPageSet(new AllLinksInput() { Namespace = ns, From = from, To = to });

		public void AddNamespace(int ns, Filter redirects) => this.FillFromPageSet(new AllPagesInput { FilterRedirects = redirects, Namespace = ns });

		public void AddNamespace(int ns, Filter redirects, string prefix) => this.FillFromPageSet(new AllPagesInput { FilterRedirects = redirects, Namespace = ns, Prefix = prefix });

		public void AddNamespace(int ns, Filter redirects, string fromPage, string toPage) => this.FillFromPageSet(new AllPagesInput { FilterRedirects = redirects, From = fromPage, Namespace = ns, To = toPage });

		public void AddPageCategories(IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(new CategoriesInput(), titles);

		public void AddPageCategories(IEnumerable<IWikiTitle> titles, Filter hidden) => this.FillFromPageSet(new CategoriesInput { FilterHidden = hidden }, titles);

		public void AddPageCategories(IEnumerable<IWikiTitle> titles, Filter hidden, IEnumerable<string> limitTo) => this.FillFromPageSet(new CategoriesInput { Categories = limitTo, FilterHidden = hidden }, titles);

		public void AddPagesWithProperty(string property) => this.FillFromPageSet(new PagesWithPropertyInput(property));

		public void AddPrefixSearchResults(string prefix) => this.FillFromPageSet(new PrefixSearchInput(prefix));

		public void AddPrefixSearchResults(string prefix, IEnumerable<int> namespaces) => this.FillFromPageSet(new PrefixSearchInput(prefix) { Namespaces = namespaces });

		public void AddQueryPage(string page) => this.FillFromPageSet(new QueryPageInput(page));

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Design supports any Dictionary-like construct, which is not otherwise possible.")]
		public void AddQueryPage(string page, IEnumerable<KeyValuePair<string, string>> parameters) => this.FillFromPageSet(new QueryPageInput(page) { Parameters = parameters });

		public void AddRecentChanges() => this.FillFromPageSet(new RecentChangesInput());

		public void AddRecentChanges(int ns) => this.FillFromPageSet(new RecentChangesInput() { Namespace = ns });

		public void AddRecentChanges(string tag) => this.FillFromPageSet(new RecentChangesInput() { Tag = tag });

		public void AddRecentChanges(RecentChangesTypes types) => this.FillFromPageSet(new RecentChangesInput() { Types = types });

		public void AddRecentChanges(RecentChangesFilters showOnly, RecentChangesFilters hide) => this.AddRecentChanges(new RecentChangesOptions() { ShowOnly = showOnly, Hide = hide });

		public void AddRecentChanges(RecentChangesFilters showOnly, RecentChangesFilters hide, RecentChangesTypes types) => this.AddRecentChanges(new RecentChangesOptions() { ShowOnly = showOnly, Hide = hide, Types = types });

		public void AddRecentChanges(DateTime? start, DateTime? end) => this.FillFromPageSet(new RecentChangesInput() { Start = start, End = end });

		public void AddRecentChanges(DateTime? start, DateTime? end, RecentChangesFilters showOnly, RecentChangesFilters hide, RecentChangesTypes types) => this.AddRecentChanges(new RecentChangesOptions() { Start = start, End = end, ShowOnly = showOnly, Hide = hide, Types = types });

		public void AddRecentChanges(DateTime start, bool newer) => this.AddRecentChanges(start, newer, 0);

		public void AddRecentChanges(DateTime start, bool newer, int count) => this.FillFromPageSet(new RecentChangesInput() { Start = start, SortAscending = newer, MaxItems = count });

		public void AddRecentChanges(string user, bool exclude) => this.FillFromPageSet(new RecentChangesInput() { User = user, ExcludeUser = exclude });

		public void AddRecentChanges(RecentChangesOptions options)
		{
			ThrowNull(options, nameof(options));
			var input = new RecentChangesInput()
			{
				Start = options.Start,
				End = options.End,
				SortAscending = options.Newer,
				User = options.User,
				ExcludeUser = options.ExcludeUser,
				Namespace = options.Namespace,
				Tag = options.Tag,
				Types = options.Types,
				FilterAnonymous = FlagToFilter(options.ShowOnly, options.Hide, RecentChangesFilters.Anonymous),
				FilterBot = FlagToFilter(options.ShowOnly, options.Hide, RecentChangesFilters.Bot),
				FilterMinor = FlagToFilter(options.ShowOnly, options.Hide, RecentChangesFilters.Minor),
				FilterPatrolled = FlagToFilter(options.ShowOnly, options.Hide, RecentChangesFilters.Patrolled),
				FilterRedirects = FlagToFilter(options.ShowOnly, options.Hide, RecentChangesFilters.Redirect),
			};

			this.FillFromPageSet(input);
		}

		public void AddRedirectsToNamespace(int ns) => this.FillFromPageSet(new AllRedirectsInput() { Namespace = ns });

		public void AddRedirectsToNamespace(int ns, string prefix) => this.FillFromPageSet(new AllRedirectsInput() { Namespace = ns, Prefix = prefix });

		public void AddRedirectsToNamespace(int ns, string from, string to) => this.FillFromPageSet(new AllRedirectsInput() { Namespace = ns, From = from, To = to });

		// Note that while RevisionsInput() can be used as a generator, I have not implemented it because I can think of no situation in which it would be useful to populate a PageCollection given the existing revisions methods.
		public void AddRevisionIds(IEnumerable<long> ids) => this.FillFromPageSet(PageSetInput.FromRevisionIds(ids), this.LoadOptions);

		public void AddRevisions(DateTime? start, DateTime? end) => this.FillFromPageSet(new AllRevisionsInput() { Start = start, End = end }, new PageLoadOptions(this.LoadOptions.Modules | PageModules.Revisions, start, end));

		public void AddRevisions(DateTime start, bool newer) => this.AddRevisions(start, newer, 0);

		public void AddRevisions(DateTime start, bool newer, int count) => this.FillFromPageSet(new AllRevisionsInput { Start = start, SortAscending = newer, MaxItems = count }, new PageLoadOptions(this.LoadOptions.Modules | PageModules.Revisions, start, newer, count));

		public void AddSearchResults(string search) => this.FillFromPageSet(new SearchInput(search) { Properties = SearchProperties.None });

		public void AddSearchResults(string search, IEnumerable<int> namespaces) => this.FillFromPageSet(new SearchInput(search) { Namespaces = namespaces, Properties = SearchProperties.None });

		public void AddSearchResults(string search, WhatToSearch whatToSearch) => this.FillFromPageSet(new SearchInput(search) { What = whatToSearch, Properties = SearchProperties.None });

		public void AddSearchResults(string search, WhatToSearch whatToSearch, IEnumerable<int> namespaces) => this.FillFromPageSet(new SearchInput(search) { Namespaces = namespaces, What = whatToSearch, Properties = SearchProperties.None });

		public void AddTemplateTransclusions() => this.FillFromPageSet(new AllTransclusionsInput());

		public void AddTemplateTransclusions(string prefix) => this.FillFromPageSet(new AllTransclusionsInput() { Prefix = prefix });

		public void AddTemplateTransclusions(string from, string to) => this.FillFromPageSet(new AllTransclusionsInput() { From = from, To = to });

		public void AddTitles(IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(titles);

		public void AddTitles(params string[] titles) => this.AddTitles(titles as IEnumerable<string>);

		public void AddTitles(IEnumerable<string> titles) => this.FillFromPageSet(new TitleCollection(this.Site, titles));

		public void AddTransclusionsOfNamespace(int ns) => this.FillFromPageSet(new AllTransclusionsInput() { Namespace = ns });

		public void AddTransclusionsOfNamespace(int ns, string prefix) => this.FillFromPageSet(new AllTransclusionsInput() { Namespace = ns, Prefix = prefix });

		public void AddTransclusionsOfNamespace(int ns, string from, string to) => this.FillFromPageSet(new AllTransclusionsInput() { Namespace = ns, From = from, To = to });

		// Only basic full-watchlist functionality is implemented because I don't think watchlists are commonly used by the type of bot this framework is geared towards. If more functionality is desired, it's easy enough to add.
		public void AddWatchlistChanged() => this.FillFromPageSet(new WatchlistInput());

		public void AddWatchlistChanged(string owner, string token) => this.FillFromPageSet(new WatchlistInput() { Owner = owner, Token = token });

		public void AddWatchlistFull() => this.FillFromPageSet(new WatchlistRawInput());

		public void AddWatchlistFull(string owner, string token) => this.FillFromPageSet(new WatchlistRawInput() { Owner = owner, Token = token });

		public void PopulateTitleMap(IPageSetResult result)
		{
			foreach (var item in result.Converted)
			{
				this.titleMap[item.Key] = new Title(this.Site, item.Value);
			}

			foreach (var item in result.Interwiki)
			{
				this.titleMap[item.Key] = new InterwikiTitle(this.Site, item.Value);
			}

			foreach (var item in result.Normalized)
			{
				this.titleMap[item.Key] = new Title(this.Site, item.Value);
			}

			foreach (var item in result.Redirects)
			{
				this.titleMap[item.Key] = new RedirectTitle(this.Site, item.Value);
			}
		}
		#endregion

		#region Public Override Methods
		public override void Clear()
		{
			base.Clear();
			this.titleMap.Clear();
		}
		#endregion

		#region Protected Methods
		protected virtual void FillFromPageSet(PageSetInput pageSetInput, PageLoadOptions options)
		{
			ThrowNull(pageSetInput, nameof(pageSetInput));
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, this.PageBuilder.GetPropertyInputs(options), this.PageBuilder.CreatePageItem);
			foreach (var item in result)
			{
				var page = this.PageBuilder.CreatePage(this.Site, item.Value.Namespace.Value, item.Value.Title);
				page.Populate(item.Value);
				page.LoadOptions = options;
				this[page.Key] = page; // Not using add because we could be loading duplicate pages.
			}

			this.PopulateTitleMap(result);
		}
		#endregion

		#region Private Methods
		private void AddBacklinks(BacklinksInput input)
		{
#pragma warning disable IDE0007
			foreach (BacklinksTypes type in input.LinkTypes.GetUniqueFlags())
#pragma warning restore IDE0007
			{
				this.FillFromPageSet(new BacklinksInput(input.Title, type)
				{
					FilterRedirects = input.FilterRedirects,
					Namespace = input.Namespace,
					Redirect = input.Redirect,
				});
			}
		}

		private void AddCategoryMembers(CategoryMembersInput input, HashSet<Title> recursionSet)
		{
			this.FillFromPageSet(input);
			if (recursionSet != null)
			{
				recursionSet.Add(new Title(this.Site, input.Title));

				var copy = new HashSet<Title>(this);
				foreach (var item in copy)
				{
					if (item.Namespace.Id == MediaWikiNamespaces.Category && !recursionSet.Contains(item))
					{
						recursionSet.Add(item);
						var newInput = new CategoryMembersInput(item.FullPageName)
						{
							Type = input.Type,
							StartSortKeyPrefix = input.StartSortKeyPrefix,
							EndSortKeyPrefix = input.EndSortKeyPrefix
						};

						this.AddCategoryMembers(newInput, recursionSet);
					}
				}
			}
		}

		private void FillFromPageSet(IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(new PageSetInput(titles.AsFullPageNames()), this.LoadOptions);

		private void FillFromPageSet(IGeneratorInput generator) => this.FillFromPageSet(new PageSetInput(generator), this.LoadOptions);

		private void FillFromPageSet(IGeneratorInput generator, PageLoadOptions options) => this.FillFromPageSet(new PageSetInput(generator), options);

		private void FillFromPageSet(IGeneratorInput generator, IEnumerable<IWikiTitle> titles) => this.FillFromPageSet(new PageSetInput(generator, titles.AsFullPageNames()), this.LoadOptions);
		#endregion
	}
}