namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	/// <summary>A collection of Title objects.</summary>
	/// <remarks>This collection class functions similar to a KeyedCollection, but automatically overwrites existing items with new ones. Because Title objects don't support changing item keys, neither does this.</remarks>
	public class TitleCollection : TitleCollection<Title>, IEnumerable<Title>, IMessageSource
	{
		#region Constructors
		public TitleCollection(Site site)
			: base(site)
		{
		}

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

		public TitleCollection(Site site, params string[] titles)
			: this(site, titles as IEnumerable<string>)
		{
		}

		public TitleCollection(Site site, int ns, IEnumerable<string> titles)
			: base(site)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var item in titles)
			{
				var newTitle = Title.ForcedNamespace(site, ns, item);
				this.Add(newTitle);
			}
		}

		public TitleCollection(Site site, int ns, params string[] titles)
			: this(site, ns, titles as IEnumerable<string>)
		{
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class from individual Title items.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection.</returns>
		public static TitleCollection CopyFrom(params Title[] titles) => CopyFrom(titles as IEnumerable<Title>);

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class from another Title collection.</summary>
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
		public void Add(params string[] titles) => this.Add(titles as IEnumerable<string>);

		public void Add(IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				this.Add(new Title(this.Site, title));
			}
		}

		public void Add(IEnumerable<IWikiTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(new Title(title));
				}
			}
		}

		public void AddBacklinks(string title) => this.AddBacklinks(title, BacklinksTypes.Backlinks | BacklinksTypes.EmbeddedIn, true);

		public void AddBacklinks(string title, BacklinksTypes linkTypes) => this.AddBacklinks(title, linkTypes, true);

		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { Redirect = includeRedirectedTitles });

		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Redirect = includeRedirectedTitles });

		public void AddBacklinks(string title, BacklinksTypes linkTypes, bool includeRedirectedTitles, Filter redirects, int ns) => this.AddBacklinks(new BacklinksInput(title, linkTypes) { FilterRedirects = redirects, Namespace = ns, Redirect = includeRedirectedTitles });

		public void AddCategories() => this.AddCategories(new AllCategoriesInput { Properties = AllCategoriesProperties.All });

		public void AddCategories(string prefix) => this.AddCategories(new AllCategoriesInput { Prefix = prefix, Properties = AllCategoriesProperties.All });

		public void AddCategories(string from, string to) => this.AddCategories(new AllCategoriesInput { From = from, To = to, Properties = AllCategoriesProperties.Hidden });

		public void AddCategoryMembers(string category, bool recurse) => this.AddCategoryMembers(category, recurse, CategoryTypes.All);

		public void AddCategoryMembers(string category, bool recurse, CategoryTypes categoryTypes)
		{
			var cat = Title.ForcedNamespace(this.Site, MediaWikiNamespaces.Category, category);
			HashSet<Title> recursionSet = null;
			if (recurse)
			{
				recursionSet = new HashSet<Title>(new WikiTitleEqualityComparer());
			}

			this.AddCategoryMembers(new CategoryMembersInput(cat.FullPageName) { Type = categoryTypes }, recursionSet);
		}

		public void AddCategoryMembers(string category, CategoryTypes categoryTypes, string fromPrefix, string toPrefix)
		{
			var cat = Title.ForcedNamespace(this.Site, MediaWikiNamespaces.Category, category);
			this.AddCategoryMembers(
				new CategoryMembersInput(cat.FullPageName)
				{
					Type = categoryTypes,
					StartSortKeyPrefix = fromPrefix,
					EndSortKeyPrefix = toPrefix,
				}, null);
		}

		public void AddFiles(string user) => this.AddFiles(new AllImagesInput { User = user });

		public void AddFiles(string from, string to) => this.AddFiles(new AllImagesInput { From = from, To = to });

		public void AddFiles(DateTime? start, DateTime? end) => this.AddFiles(new AllImagesInput { Start = start, End = end });

		public void AddFileUsage() => this.AddFileUsage(new AllFileUsagesInput());

		public void AddFileUsage(string prefix) => this.AddFileUsage(new AllFileUsagesInput { Prefix = prefix });

		public void AddFileUsage(string from, string to) => this.AddFileUsage(new AllFileUsagesInput { From = from, To = to });

		public void AddLinksToNamespace(int ns) => this.AddLinksToNamespace(new AllLinksInput { Namespace = ns });

		public void AddLinksToNamespace(int ns, string prefix) => this.AddLinksToNamespace(new AllLinksInput { Namespace = ns, Prefix = prefix });

		public void AddLinksToNamespace(int ns, string from, string to) => this.AddLinksToNamespace(new AllLinksInput { Namespace = ns, From = from, To = to });

		public void AddMessages(Filter modifiedMessages) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages });

		public void AddMessages(Filter modifiedMessages, IEnumerable<string> messages) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages, Messages = messages });

		public void AddMessages(Filter modifiedMessages, string prefix) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages, Prefix = prefix });

		public void AddMessages(Filter modifiedMessages, string from, string to) => this.AddMessages(new AllMessagesInput { FilterModified = modifiedMessages, MessageFrom = from, MessageTo = to });

		public void AddNamespace(int ns, Filter redirects) => this.AddNamespace(new AllPagesInput { Namespace = ns, FilterRedirects = redirects });

		public void AddNamespace(int ns, Filter redirects, string startsWith) => this.AddNamespace(new AllPagesInput { FilterRedirects = redirects, Namespace = ns, Prefix = startsWith });

		public void AddNamespace(int ns, Filter redirects, string fromPage, string toPage) => this.AddNamespace(new AllPagesInput() { FilterRedirects = redirects, From = fromPage, Namespace = ns, To = toPage });

		public void AddProtectedTitles() => this.AddProtectedTitles(new ProtectedTitlesInput());

		public void AddProtectedTitles(IEnumerable<int> namespaces) => this.AddProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces });

		public void AddProtectedTitles(IEnumerable<string> levels) => this.AddProtectedTitles(new ProtectedTitlesInput() { Levels = levels });

		public void AddProtectedTitles(IEnumerable<int> namespaces, IEnumerable<string> levels) => this.AddProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces, Levels = levels });

		public void AddQueryPage(string pageName) => this.AddQueryPage(new QueryPageInput(pageName));

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Design supports any Dictionary-like construct, which is not otherwise possible.")]
		public void AddQueryPage(string pageName, IEnumerable<KeyValuePair<string, string>> parameters) => this.AddQueryPage(new QueryPageInput(pageName) { Parameters = parameters });

		public void AddRedirectsToNamespace(int ns) => this.AddRedirectsToNamespace(new AllRedirectsInput { Namespace = ns });

		public void AddRedirectsToNamespace(int ns, string prefix) => this.AddRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, Prefix = prefix });

		public void AddRedirectsToNamespace(int ns, string from, string to) => this.AddRedirectsToNamespace(new AllRedirectsInput { Namespace = ns, From = from, To = to });

		public void AddRevisions(DateTime? start, DateTime? end) => this.AddRevisions(new AllRevisionsInput { Start = start, End = end });

		public void AddRevisions(DateTime start, bool newer) => this.AddRevisions(start, newer, 0);

		public void AddRevisions(DateTime start, bool newer, int count) => this.AddRevisions(new AllRevisionsInput { Start = start, SortAscending = newer, MaxItems = count });

		public void AddSearchResults(string search) => this.AddSearchResults(new SearchInput(search) { Properties = SearchProperties.None });

		public void AddSearchResults(string search, IEnumerable<int> namespaces) => this.AddSearchResults(new SearchInput(search) { Namespaces = namespaces, Properties = SearchProperties.None });

		public void AddSearchResults(string search, WhatToSearch whatToSearch) => this.AddSearchResults(new SearchInput(search) { What = whatToSearch, Properties = SearchProperties.None });

		public void AddSearchResults(string search, WhatToSearch whatToSearch, IEnumerable<int> namespaces) => this.AddSearchResults(new SearchInput(search) { Namespaces = namespaces, What = whatToSearch, Properties = SearchProperties.None });

		public void AddTemplateTransclusions() => this.AddTemplateTransclusions(new AllTransclusionsInput());

		public void AddTemplateTransclusions(string prefix) => this.AddTemplateTransclusions(new AllTransclusionsInput { Prefix = prefix });

		public void AddTemplateTransclusions(string from, string to) => this.AddTemplateTransclusions(new AllTransclusionsInput { From = from, To = to });

		public void AddTransclusionsOfNamespace(int ns) => this.AddTransclusionsOfNamespace(new AllTransclusionsInput { Namespace = ns });

		public void AddTransclusionsOfNamespace(int ns, string prefix) => this.AddTransclusionsOfNamespace(new AllTransclusionsInput { Namespace = ns, Prefix = prefix });

		public void AddTransclusionsOfNamespace(int ns, string from, string to) => this.AddTransclusionsOfNamespace(new AllTransclusionsInput { Namespace = ns, From = from, To = to });

		public PageCollection Load() => this.Load(this.Site.DefaultLoadOptions);

		public PageCollection Load(PageLoadOptions options)
		{
			var retval = new PageCollection(this.Site, options);
			retval.AddTitles(this);
			return retval;
		}

		public PageCollection Purge() => this.Purge(PurgeMethod.Normal);

		public PageCollection Purge(PurgeMethod method)
		{
			var input = new PurgeInput(this.AsFullPageNames()) { Method = method };
			var result = this.Site.AbstractionLayer.Purge(input);

			var retval = new PageCollection(this.Site);
			retval.PopulateTitleMap(result);
			foreach (var item in result)
			{
				var purgePage = item.Value;
				var flags = purgePage.Flags;
				if (flags.HasFlag(PurgeFlags.Purged))
				{
					retval.Add(this.Site.PageCreator.CreatePage(this.Site, purgePage.Namespace.Value, purgePage.Title));
				}
				else if (!purgePage.Flags.HasFlag(PurgeFlags.Missing))
				{
					this.Site.PublishWarning(this, CurrentCulture(PurgeFailed, purgePage.Title));
				}
			}

			return retval;
		}

		public PageCollection Watch() => this.Watch(true);

		public PageCollection Unwatch() => this.Watch(false);
		#endregion

		#region Private Methods
		private void AddBacklinks(BacklinksInput input)
		{
			var result = this.Site.AbstractionLayer.Backlinks(input);
			this.FillFromTitleItems(result);
		}

		private void AddCategories(AllCategoriesInput input)
		{
			var result = this.Site.AbstractionLayer.AllCategories(input);
			this.FillFromTitleItems(result);
		}

		private void AddCategoryMembers(CategoryMembersInput input, HashSet<Title> recursionSet)
		{
			var result = this.Site.AbstractionLayer.CategoryMembers(input);
			this.FillFromTitleItems(result);

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

		private void AddFiles(AllImagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		private void AddFileUsage(AllFileUsagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		private void AddLinksToNamespace(AllLinksInput input)
		{
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		private void AddMessages(AllMessagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllMessages(input);
			this.FillFromTitleItems(result);
		}

		private void AddNamespace(AllPagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllPages(input);
			this.FillFromTitleItems(result);
		}

		private void AddProtectedTitles(ProtectedTitlesInput input)
		{
			var result = this.Site.AbstractionLayer.ProtectedTitles(input);
			this.FillFromTitleItems(result);
		}

		private void AddQueryPage(QueryPageInput input)
		{
			var result = this.Site.AbstractionLayer.QueryPage(input);
			this.FillFromTitleItems(result);
		}

		private void AddRedirectsToNamespace(AllRedirectsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		private void AddRevisions(AllRevisionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRevisions(input);
			this.FillFromTitleItems(result);
		}

		private void AddSearchResults(SearchInput input)
		{
			var result = this.Site.AbstractionLayer.Search(input);
			this.FillFromTitleItems(result);
		}

		private void AddTemplateTransclusions(AllTransclusionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		private void AddTransclusionsOfNamespace(AllTransclusionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		private void FillFromTitleItems(IEnumerable<ITitleOnly> result)
		{
			foreach (var item in result)
			{
				this.Add(new Title(this.Site, item.Title));
			}
		}

		private PageCollection Watch(bool watch)
		{
			var retval = new PageCollection(this.Site);
			if (!this.Site.AllowEditing)
			{
				foreach (var item in this)
				{
					retval.Add(this.Site.PageCreator.CreatePage(this.Site, item.Namespace.Id, item.FullPageName));
				}

				return retval;
			}

			var input = new WatchInput(this.AsFullPageNames()) { Unwatch = !watch };
			var result = this.Site.AbstractionLayer.Watch(input);

			retval.PopulateTitleMap(result);
			foreach (var item in result)
			{
				var watchPage = item.Value;
				var flags = watchPage.Flags;
				if (flags.HasFlag(WatchFlags.Watched) || flags.HasFlag(WatchFlags.Unwatched))
				{
					retval.Add(this.Site.PageCreator.CreatePage(this.Site, watchPage.Namespace.Value, watchPage.Title));
				}

				// WatchFlags has no invalid value so, unlike Purge, no warnings are raised here.
			}

			return retval;
		}
		#endregion
	}
}