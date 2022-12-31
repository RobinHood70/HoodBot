namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>A collection of Title objects.</summary>
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Class coupling is a factor of using classes to handle complex inputs and is unavoidable.")]
	public class TitleCollection : TitleCollection<Title>, IMessageSource
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class.</summary>
		/// <param name="site">The site the titles are from. All titles in a collection must belong to the same site.</param>
		public TitleCollection(Site site)
			: base(site)
		{
			this.LimitationType = LimitationType.None;
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles.</summary>
		/// <param name="site">The site.</param>
		/// <param name="titles">The titles.</param>
		public TitleCollection(Site site, IEnumerable<string> titles)
			: base(site)
		{
			this.LimitationType = LimitationType.None;
			foreach (var item in titles.NotNull())
			{
				var newTitle = TitleFactory.FromUnvalidated(site, item);
				this.Add(newTitle);
			}
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles.</summary>
		/// <param name="site">The site.</param>
		/// <param name="titles">The titles.</param>
		public TitleCollection(Site site, params string[] titles)
			: this(site, titles as IEnumerable<string>)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles in a given namespace.</summary>
		/// <param name="site">The site.</param>
		/// <param name="ns">The namespace the titles are in.</param>
		/// <param name="titles">The titles. Namespace text is optional for individual titles and will be used in favour of the ns parameter if provided.</param>
		public TitleCollection(Site site, int ns, IEnumerable<string> titles)
			: base(site)
		{
			this.LimitationType = LimitationType.None;
			this.Add(ns, titles.NotNull());
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class with a specific list of titles in a given namespace.</summary>
		/// <param name="site">The site.</param>
		/// <param name="ns">The namespace the titles are in.</param>
		/// <param name="titles">The titles. Namespace text is optional and will be stripped if provided.</param>
		public TitleCollection(Site site, int ns, params string[] titles)
			: this(site, ns, titles as IEnumerable<string>)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleCollection" /> class from another Title collection.</summary>
		/// <param name="site">The site.</param>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection. Note that this creates all new Titles based on the original objects' namespace, page name, and key.</returns>
		public TitleCollection(Site site, IEnumerable<Title> titles)
			: base(site)
		{
			this.LimitationType = LimitationType.None;
			foreach (var title in titles.NotNull())
			{
				this.Add(title);
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Adds the specified titles to the collection, assuming that they are in the provided namespace if no other namespace is specified.</summary>
		/// <param name="defaultNamespace">The namespace to coerce.</param>
		/// <param name="titles">The titles to add, with or without the leading namespace text.</param>
		public void Add(int defaultNamespace, IEnumerable<string> titles)
		{
			foreach (var title in titles.NotNull())
			{
				this.Add(TitleFactory.FromUnvalidated(this.Site[defaultNamespace], title));
			}
		}

		/// <summary>Adds the specified titles to the collection, assuming that they are in the provided namespace if no other namespace is specified.</summary>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="names">The page names, with or without the leading namespace text.</param>
		public void Add(int defaultNamespace, params string[] names) => this.Add(defaultNamespace, names as IEnumerable<string>);

		/// <summary>Converts all MediaWiki messages to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		public void GetMessages(Filter modifiedMessages) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages });

		/// <summary>Converts specific MediaWiki messages to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="messages">The messages to load.</param>
		public void GetMessages(Filter modifiedMessages, IEnumerable<string> messages) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages, Messages = messages });

		/// <summary>Converts MediaWiki messages beginning with the specified prefix to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="prefix">The prefix of the categories to load.</param>
		public void GetMessages(Filter modifiedMessages, string prefix) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages, Prefix = prefix });

		/// <summary>Converts MediaWiki messages within the given range to titles based on their modification status and adds them to the collection.</summary>
		/// <param name="modifiedMessages">Filter for whether the messages have been modified.</param>
		/// <param name="from">The message to start at (inclusive). The message specified does not have to exist.</param>
		/// <param name="to">The message to stop at (inclusive). The message specified does not have to exist.</param>
		public void GetMessages(Filter modifiedMessages, string from, string to) => this.GetMessages(new AllMessagesInput { FilterModified = modifiedMessages, MessageFrom = from, MessageTo = to });

		/// <summary>Adds all protected titles to the collection.</summary>
		public void GetProtectedTitles() => this.GetProtectedTitles(new ProtectedTitlesInput());

		/// <summary>Adds all protected titles in the given namespaces to the collection.</summary>
		/// <param name="namespaces">The namespaces to load from.</param>
		public void GetProtectedTitles(IEnumerable<int> namespaces) => this.GetProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces });

		/// <summary>Adds all protected titles of the specified levels to the collection.</summary>
		/// <param name="levels">The levels of titles to load (typically, one of: "autoconfirmed" or "sysop").</param>
		public void GetProtectedTitles(IEnumerable<string> levels) => this.GetProtectedTitles(new ProtectedTitlesInput() { Levels = levels });

		/// <summary>Adds all protected titles of the specified levels in the given namespaces to the collection.</summary>
		/// <param name="namespaces">The namespaces to load from.</param>
		/// <param name="levels">The levels of titles to load (typically, one of: "autoconfirmed" or "sysop").</param>
		public void GetProtectedTitles(IEnumerable<int> namespaces, IEnumerable<string> levels) => this.GetProtectedTitles(new ProtectedTitlesInput() { Namespaces = namespaces, Levels = levels });

		/// <summary>Loads all pages in the collection.</summary>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load() => this.Load(this.Site.DefaultLoadOptions);

		/// <summary>Loads the specified information for all pages in the collection.</summary>
		/// <param name="modules">The page modules to load, using their default options.</param>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load(PageModules modules) => this.Load(new PageLoadOptions(modules));

		/// <summary>Loads the specified information for all pages in the collection.</summary>
		/// <param name="modules">The page modules to load, using their default options.</param>
		/// <param name="followRedirects">Indicates whether redirects should be followed when loading.</param>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load(PageModules modules, bool followRedirects) => this.Load(new PageLoadOptions(modules, followRedirects));

		/// <summary>Loads the specified information for all pages in the collection.</summary>
		/// <param name="options">The page load options.</param>
		/// <returns>A <see cref="PageCollection"/> containing the specified pages, including status information for pages that could not be loaded.</returns>
		public PageCollection Load(PageLoadOptions options)
		{
			var retval = PageCollection.Unlimited(this.Site, options);
			retval.GetTitles(this);

			return retval;
		}

		// TODO: Might make sense to move Purge, Watch, and Unwatch to PageCollection static or even Site.

		/// <summary>Purges all pages in the collection.</summary>
		/// <returns>A value indicating the change status of the purge along with a page collection with the purge results.</returns>
		public ChangeValue<PageCollection> Purge() => this.Purge(PurgeMethod.Normal, 10);

		/// <summary>Purges all pages in the collection.</summary>
		/// <param name="method">The method.</param>
		/// <param name="batchSize">The number of purge requests to send at once.</param>
		/// <returns>A value indicating the change status of the purge along with a page collection with the purge results.</returns>
		public ChangeValue<PageCollection> Purge(PurgeMethod method, int batchSize)
		{
			if (this.Count == 0)
			{
				return new ChangeValue<PageCollection>(ChangeStatus.NoEffect, PageCollection.Unlimited(this.Site));
			}

			var disabledResult = PageCollection.UnlimitedDefault(this.Site);
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(method)] = method
			};

			return this.Site.PublishChange(disabledResult, this, parameters, ChangeFunc);

			ChangeValue<PageCollection> ChangeFunc()
			{
				var retval = PageCollection.Purge(this.Site, this, method, batchSize);
				return new ChangeValue<PageCollection>(ChangeStatus.Success, retval);
			}
		}

		/// <summary>Unwatches all pages in the collection.</summary>
		/// <returns>A value indicating the change status of the unwatch along with a page collection with the unwatch results.</returns>
		public ChangeValue<PageCollection> Unwatch()
		{
			if (this.Count == 0)
			{
				return new ChangeValue<PageCollection>(ChangeStatus.NoEffect, PageCollection.Unlimited(this.Site));
			}

			var disabledResult = PageCollection.CreateEmptyPages(this.Site, this);
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal);
			return this.Site.PublishChange(disabledResult, this, parameters, ChangeFunc);

			ChangeValue<PageCollection> ChangeFunc()
			{
				var pages = PageCollection.Watch(this.Site, new WatchInput(this.ToFullPageNames()) { Unwatch = true });
				var result = (pages.Count < this.Count)
					? ChangeStatus.Failure
					: ChangeStatus.Success;
				return new ChangeValue<PageCollection>(result, pages);
			}
		}

		/// <summary>Watches all pages in the collection.</summary>
		/// <returns>A value indicating the change status of the watch along with a page collection with the watch results.</returns>
		public ChangeValue<PageCollection> Watch()
		{
			if (this.Count == 0)
			{
				return new ChangeValue<PageCollection>(ChangeStatus.NoEffect, PageCollection.Unlimited(this.Site));
			}

			var disabledResult = PageCollection.CreateEmptyPages(this.Site, this);
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal);

			return this.Site.PublishChange(disabledResult, this, parameters, ChangeFunc);

			ChangeValue<PageCollection> ChangeFunc()
			{
				var pages = PageCollection.Watch(this.Site, new WatchInput(this.ToFullPageNames()) { Unwatch = false });
				var result = (pages.Count < this.Count)
					? ChangeStatus.Failure
					: ChangeStatus.Success;
				return new ChangeValue<PageCollection>(result, pages);
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Adds a new object to the collection with the specified name.</summary>
		/// <param name="title">The title to add.</param>
		public void Add(string title) => this.Add(TitleFactory.FromUnvalidated(this.Site, title));

		/// <summary>Adds new objects to the collection based on an existing <see cref="Title"/> collection.</summary>
		/// <param name="titles">The titles to be added.</param>
		/// <remarks>All items added are newly created, even if the type of the titles provided matches those in the collection.</remarks>
		public void Add(IEnumerable<Title> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(title);
				}
			}
		}

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles.</param>
		public void Add(params string[] titles) => this.Add(titles as IEnumerable<string>);

		/// <summary>Adds the specified titles to the collection, creating new objects for each.</summary>
		/// <param name="titles">The titles to add.</param>
		public void Add(IEnumerable<string> titles)
		{
			foreach (var title in titles.NotNull())
			{
				this.Add(title);
			}
		}

		/// <inheritdoc/>
		public override void GetCustomGenerator(IGeneratorInput generatorInput) => this.LoadPages(new QueryPageSetInput(generatorInput));

		/// <summary>Adds pages to the collection from their revision IDs.</summary>
		/// <param name="revisionIds">The revision IDs.</param>
		public override void GetRevisionIds(IEnumerable<long> revisionIds) => this.LoadPages(QueryPageSetInput.FromRevisionIds(revisionIds));
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
			}

			var result = this.Site.AbstractionLayer.Backlinks(input);
			foreach (var item in result)
			{
				var mainTitle = TitleFactory.CoValidate(this.Site, item.Namespace, item.FullPageName);
				this.Add(mainTitle);
				if (item.Redirects != null)
				{
					foreach (var redirectedItem in item.Redirects)
					{
						var title = TitleFactory.FromUnvalidated(this.Site, redirectedItem.FullPageName);
						this.Add(new Backlink(title, mainTitle));
					}
				}
			}
		}

		/// <summary>Adds a set of category pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetCategories(AllCategoriesInput input)
		{
			var result = this.Site.AbstractionLayer.AllCategories(input);
			foreach (var item in result)
			{
				this.Add(TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Category], item.Category));
			}
		}

		/// <summary>Adds category members to the collection, potentially including subcategories and their members.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="recurse">if set to <see langword="true"/> load the entire category tree recursively.</param>
		protected override void GetCategoryMembers(CategoryMembersInput input, bool recurse)
		{
			var originalProps = input.NotNull().Properties;
			input.Properties |= CategoryMembersProperties.Title;
			if (recurse)
			{
				this.RecurseCategoryPages(input, new HashSet<string>(StringComparer.Ordinal));
			}
			else
			{
				var result = this.Site.AbstractionLayer.CategoryMembers(input);
				this.FillFromTitleItems(result);
			}

			input.Properties = originalProps;
		}

		/// <summary>Adds duplicate files of the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles to find duplicates of.</param>
		protected override void GetDuplicateFiles(DuplicateFilesInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds files to the collection, based on optionally file-specific parameters.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFiles(AllImagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds files that are in use to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetFileUsage(AllFileUsagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages that use the files given in titles (via File/Image/Media links) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void GetFileUsage(FileUsageInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that link to a given namespace.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetLinksToNamespace(AllLinksInput input)
		{
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds category pages that are referenced by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageCategories(CategoriesInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that are linked to by the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose categories should be loaded.</param>
		protected override void GetPageLinks(LinksInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that link to the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles.</param>
		protected override void GetPageLinksHere(LinksHereInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages with the specified filters to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPages(AllPagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllPages(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages with a given property to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPagesWithProperty(PagesWithPropertyInput input)
		{
			var result = this.Site.AbstractionLayer.PagesWithProperty(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTranscludedIn(TranscludedInInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds pages that are transcluded from the given titles to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <param name="titles">The titles whose transclusions should be loaded.</param>
		protected override void GetPageTransclusions(TemplatesInput input, IEnumerable<Title> titles) => this.LoadPages(new QueryPageSetInput(input, titles.ToFullPageNames()));

		/// <summary>Adds prefix-search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetPrefixSearchResults(PrefixSearchInput input)
		{
			var result = this.Site.AbstractionLayer.PrefixSearch(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds query page results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Query pages are a subset of Special pages that conform to a specific standard. You can find a list by using the Help feature of the API (<c>/api.php?action=help&amp;modules=query+querypage</c>). Note that a few of these (e.g., ListDuplicatedFiles) have API equivalents that are more functional and produce the same or more detailed results.</remarks>
		protected override void GetQueryPage(QueryPageInput input)
		{
			var result = this.Site.AbstractionLayer.QueryPage(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Gets a random set of pages from the wiki.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRandomPages(RandomInput input)
		{
			var result = this.Site.AbstractionLayer.Random(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds recent changes pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRecentChanges(RecentChangesInput input)
		{
			var result = this.Site.AbstractionLayer.RecentChanges(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds redirects to a namespace to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRedirectsToNamespace(AllRedirectsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages from a range of revisions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetRevisions(AllRevisionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllRevisions(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds search results to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetSearchResults(SearchInput input)
		{
			var result = this.Site.AbstractionLayer.Search(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds pages with template transclusions to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetTransclusions(AllTransclusionsInput input)
		{
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds changed watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistChanged(WatchlistInput input)
		{
			var result = this.Site.AbstractionLayer.Watchlist(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Adds raw watchlist pages to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected override void GetWatchlistRaw(WatchlistRawInput input)
		{
			var result = this.Site.AbstractionLayer.WatchlistRaw(input);
			this.FillFromTitleItems(result);
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Converts MediaWiki messages to titles and adds them to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void GetMessages(AllMessagesInput input)
		{
			var result = this.Site.AbstractionLayer.AllMessages(input);
			foreach (var item in result)
			{
				this.Add(TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.MediaWiki], item.Name));
			}
		}

		/// <summary>Adds creation-protected titles (pages that are protected but don't exist) to the collection.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void GetProtectedTitles(ProtectedTitlesInput input)
		{
			var result = this.Site.AbstractionLayer.ProtectedTitles(input);
			this.FillFromTitleItems(result);
		}

		/// <summary>Loads pages from the wiki based on a page set specifier.</summary>
		/// <param name="pageSetInput">The pageset inputs.</param>
		protected virtual void LoadPages(QueryPageSetInput pageSetInput)
		{
			if (pageSetInput.NotNull().IsEmpty)
			{
				return;
			}

			PageLoadOptions loadOptions = new(this.Site.DefaultLoadOptions, PageModules.Info);
			var creator = this.Site.PageCreator;
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, creator.GetPropertyInputs(loadOptions), creator.CreatePageItem);
			this.FillFromTitleItems(result);
		}
		#endregion

		#region Private Methods

		private void FillFromTitleItems(IEnumerable<IApiTitle> result)
		{
			foreach (var item in result)
			{
				this.Add(TitleFactory.FromUnvalidated(this.Site, item.FullPageName));
			}
		}

		private void FillFromTitleItems(IEnumerable<IApiTitleOptional> result)
		{
			foreach (var item in result)
			{
				item.FullPageName.PropertyThrowNull(nameof(item));
				this.Add(TitleFactory.FromUnvalidated(this.Site, item.FullPageName));
			}
		}

		private void RecurseCategoryPages(CategoryMembersInput input, HashSet<string> categoryTree)
		{
			input.ThrowNull();
			input.Title.PropertyThrowNull(nameof(input), nameof(input.Title));
			if (!categoryTree.Add(input.Title))
			{
				return;
			}

			var result = this.Site.AbstractionLayer.CategoryMembers(input);
			foreach (var item in result)
			{
				item.FullPageName.PropertyThrowNull(nameof(item));
				var title = TitleFactory.CoValidate(this.Site, item.Namespace, item.FullPageName);
				if (input.Type.HasFlag(item.Type))
				{
					this.Add(title);
				}

				if (item.Type == CategoryMemberTypes.Subcat && item.FullPageName is string itemTitle)
				{
					var originalTitle = input.Title;
					input.ChangeTitle(itemTitle);
					this.RecurseCategoryPages(input, categoryTree);
					input.ChangeTitle(originalTitle);
				}
			}
		}
		#endregion
	}
}