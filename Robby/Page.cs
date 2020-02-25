namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a wiki page.</summary>
	/// <seealso cref="Title" />
	public class Page : Title
	{
		#region Fields
		private Uri? canonicalPath;
		private Revision? currentRevision;
		private Uri? editPath;
		private PageLoadOptions? loadOptionsUsed;
		private string text = string.Empty;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Page" /> class based on site and page name.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		public Page(Site site, string fullPageName)
			: base(site, fullPageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Page" /> class using the namespace and page name.</summary>
		/// <param name="site">The site this page is from.</param>
		/// <param name="ns">The namespace ID the page is in.</param>
		/// <param name="pageName">The name of the page without the namespace.</param>
		/// <remarks>This constructor will always assume the namespace given is correct, even if the pageName begins with something that looks like a namespace.</remarks>
		public Page(Site site, int ns, string pageName)
			: base(site, ns, pageName, true)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Page" /> class from another ISimpleTitle-based object.</summary>
		/// <param name="title">The Title object to copy from.</param>
		public Page(ISimpleTitle title)
			: base(title)
		{
		}
		#endregion

		#region Public Events

		/// <summary>Occurs when the page is loaded.</summary>
		/// <remarks>Note that this event is only raised when the page is loaded individually.</remarks>
		/// <seealso cref="PageCollection.PageLoaded"/>
		public event StrongEventHandler<Page, EventArgs>? PageLoaded;
		#endregion

		#region Public Properties

		/// <summary>Gets the backlinks on the page, if they were requested in the last load operation.</summary>
		/// <value>The links used on the page.</value>
		/// <remarks>This includes links, transclusions, and file usage.</remarks>
		public IReadOnlyDictionary<Title, BacklinksTypes> Backlinks { get; } = new Dictionary<Title, BacklinksTypes>(SimpleTitleEqualityComparer.Instance);

		/// <summary>Gets or sets the canonical article path.</summary>
		/// <value>The canonical article path.</value>
		public Uri CanonicalPath
		{
			get => this.canonicalPath ?? this.Site.GetArticlePath(this.FullPageName);
			set => this.canonicalPath = value;
		}

		/// <summary>Gets the page categories, if they were requested in the last load operation.</summary>
		/// <value>The categories the page is listed in.</value>
		public IReadOnlyList<Category> Categories { get; } = new List<Category>();

		/// <summary>Gets the current revision.</summary>
		/// <value>The current revision.</value>
		/// <remarks>If revisions are loaded which do not include the current revision, this will be null.</remarks>
		public Revision? CurrentRevision => this.currentRevision ?? (this.currentRevision = (this.Revisions as List<Revision>)!.Find(item => item.Id == this.CurrentRevisionId));

		/// <summary>Gets the ID of the current revision.</summary>
		/// <value>The ID of the current revision.</value>
		/// <remarks>Even if this is populated, the current revision isn't guaranteed to be.</remarks>
		public long CurrentRevisionId { get; internal set; }

		/// <summary>Gets or sets the URI to edit the article in a browser.</summary>
		/// <value>The edit path.</value>
		public Uri EditPath
		{
			get => this.editPath ?? new UriBuilder(this.Site.ScriptPath)
			{
				Query = "title=" + Uri.EscapeDataString(this.FullPageName) + "&action=edit"
			}.Uri;
			set => this.editPath = value;
		}

		/// <summary>Gets a value indicating whether this <see cref="Page" /> exists.</summary>
		/// <value><see langword="true" /> if the page exists; otherwise, <see langword="false" />.</value>
		public bool Exists => !this.IsMissing && !this.IsInvalid;

		/// <summary>Gets a value indicating whether the page represents a disambiguation page.</summary>
		/// <returns><see langword="true" /> if this instance is disambiguation; otherwise, <see langword="false" />.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the page properties (MW 1.21+) or templates (MW 1.20-) weren't loaded prior to checking.</exception>
		public bool IsDisambiguation
		{
			get
			{
				if (this.Site.DisambiguatorAvailable)
				{
					// Disambiguator is 1.21+, so we don't need to worry about the fact that page properties are 1.17+.
					if (this.loadOptionsUsed == null || !this.loadOptionsUsed.Modules.HasFlag(PageModules.Properties))
					{
						throw new InvalidOperationException(CurrentCulture(Resources.ModuleNotLoaded, nameof(PageModules.Properties), nameof(this.IsDisambiguation)));
					}

					return this.Properties.ContainsKey("disambiguation");
				}

				if (this.loadOptionsUsed == null || !this.loadOptionsUsed.Modules.HasFlag(PageModules.Templates))
				{
					throw new InvalidOperationException(CurrentCulture(Resources.ModuleNotLoaded, nameof(PageModules.Templates), nameof(this.IsDisambiguation)));
				}

				var templates = new HashSet<Title>(this.Templates);
				templates.IntersectWith(this.Site.DisambiguationTemplates);

				return templates.Count > 0;
			}
		}

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is invalid.</summary>
		/// <value><see langword="true" /> if invalid; otherwise, <see langword="false" />.</value>
		public bool IsInvalid { get; protected set; }

		/// <summary>Gets a value indicating whether this <see cref="Page" /> has been loaded.</summary>
		/// <value><see langword="true" /> if loaded; otherwise, <see langword="false" />.</value>
		public bool IsLoaded { get; private set; }

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is missing.</summary>
		/// <value><see langword="true" /> if the page is missing; otherwise, <see langword="false" />.</value>
		public bool IsMissing { get; protected set; } = true;

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is new.</summary>
		/// <value><see langword="true" /> if the page is new; otherwise, <see langword="false" />.</value>
		public bool IsNew { get; protected set; }

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is a redirect.</summary>
		/// <value><see langword="true" /> if the page is a redirect; otherwise, <see langword="false" />.</value>
		public bool IsRedirect { get; protected set; }

		/// <summary>Gets the links on the page, if they were requested in the last load operation.</summary>
		/// <value>The links used on the page.</value>
		public IReadOnlyList<Title> Links { get; } = new List<Title>();

		/// <summary>Gets the page properties, if they were requested in the last load operation.</summary>
		/// <value>The list of page properties.</value>
		public IReadOnlyDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

		/// <summary>Gets the page revisions, if they were requested in the last load operation.</summary>
		/// <value>The revisions list.</value>
		public IReadOnlyList<Revision> Revisions { get; } = new List<Revision>();

		/// <summary>Gets or sets the timestamp when the page was loaded. Used for edit conflict detection.</summary>
		/// <value>The start timestamp.</value>
		public DateTime? StartTimestamp { get; protected set; }

		/// <summary>Gets the templates used on the page, if they were requested in the last load operation.</summary>
		/// <value>The templates used on the page.</value>
		public IReadOnlyCollection<Title> Templates { get; } = new List<Title>();

		/// <summary>Gets or sets the text, if revisions were requested in the last load operation.</summary>
		/// <value>The page text.</value>
		[AllowNull]
		public string Text
		{
			get => this.text;
			set => this.text = value ?? string.Empty;
		}

		/// <summary>Gets a value indicating whether the <see cref="Text" /> property has been modified.</summary>
		/// <value><see langword="true" /> if the text no longer matches the first revision; otherwise, <see langword="false" />.</value>
		/// <remarks>This is currently simply a shortcut property to compare the Text with Revisions[0]. This may not be an accurate reflection of modification status when loading a specific revision range or in other unusual circumstances.</remarks>
		public bool TextModified => this.Text != (this.CurrentRevision?.Text ?? string.Empty);
		#endregion

		#region Public Static Methods

		/// <summary>Returns a value indicating whether the page exists.</summary>
		/// <param name="site">The site to search on.</param>
		/// <param name="fullName">The full name.</param>
		/// <returns>A value indicating whether the page exists.</returns>
		public static bool CheckExistence(Site site, string fullName) => new Page(site, fullName).CheckExistence();
		#endregion

		#region Public Methods

		/// <summary>Returns a value indicating if the page exists. This will trigger a Load operation if necessary.</summary>
		/// <returns><see langword="true" /> if the page exists; otherwise <see langword="false" />.</returns>
		public bool CheckExistence()
		{
			if (!this.IsLoaded)
			{
				this.Load(PageModules.None);
			}

			return this.Exists;
		}

		/// <summary>Loads or reloads the page.</summary>
		public void Load() => this.Load(this.Site.DefaultLoadOptions);

		/// <summary>Loads the specified page modules.</summary>
		/// <param name="pageModules">The page modules.</param>
		public void Load(PageModules pageModules) => this.Load(new PageLoadOptions(pageModules));

		/// <summary>Loads the page with the specified load options.</summary>
		/// <param name="options">The options.</param>
		public void Load(PageLoadOptions options)
		{
			var creator = this.Site.PageCreator;
			var propertyInputs = creator.GetPropertyInputs(options);
			var pageSetInput = new QueryPageSetInput(new[] { this.FullPageName }) { ConvertTitles = options.ConvertTitles, Redirects = options.FollowRedirects };
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, propertyInputs, creator.CreatePageItem);
			if (result.Count == 1)
			{
				this.Populate(result[0], options);
				this.PageLoaded?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>Saves the page.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus Save([Localizable(true)] string editSummary, bool isMinor) => this.Save(editSummary, isMinor, Tristate.Unknown, false, true);

		/// <summary>Saves the page with full options.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="createOnly">Whether the edit should only occur if it would create a new page (<see cref="Tristate.True"/>), if it would not create a new page (<see cref="Tristate.False"/>) or it doesn't matter (<see cref="Tristate.Unknown"/>).</param>
		/// <param name="recreateIfJustDeleted">Whether to recreate the page if it was deleted since being loaded.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus Save([Localizable(true)] string editSummary, bool isMinor, Tristate createOnly, bool recreateIfJustDeleted) => this.Save(editSummary, isMinor, createOnly, recreateIfJustDeleted, true);

		/// <summary>Saves the page with full options.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="createOnly">Whether the edit should only occur if it would create a new page (<see cref="Tristate.True"/>), if it would not create a new page (<see cref="Tristate.False"/>) or it doesn't matter (<see cref="Tristate.Unknown"/>).</param>
		/// <param name="recreateIfJustDeleted">Whether to recreate the page if it was deleted since being loaded.</param>
		/// <param name="isBotEdit">Whether the edit should be marked as a bot edit.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus Save([Localizable(true)] string editSummary, bool isMinor, Tristate createOnly, bool recreateIfJustDeleted, bool isBotEdit)
		{
			if (!this.TextModified)
			{
				return ChangeStatus.NoEffect;
			}

			var changeArgs = new PageTextChangeArgs(this, nameof(this.Save), editSummary, isMinor, isBotEdit, recreateIfJustDeleted);
			return this.Site.PublishPageTextChange(
				changeArgs,
				() => // Modification status re-checked here because a subscriber may have reverted the page.
					!this.TextModified ? ChangeStatus.NoEffect :
						this.Site.AbstractionLayer.Edit(new EditInput(this.FullPageName, this.Text)
						{
							BaseTimestamp = this.CurrentRevision?.Timestamp,
							StartTimestamp = this.StartTimestamp,
							Bot = changeArgs.BotEdit,
							Minor = changeArgs.Minor ? Tristate.True : Tristate.False,
							Recreate = changeArgs.RecreateIfJustDeleted,
							Summary = changeArgs.EditSummary,
							RequireNewPage = createOnly,
						}).Result == "Success" ? ChangeStatus.Success : ChangeStatus.Failure);
		}

		/// <summary>Sets <see cref="StartTimestamp"/> to 2000-01-01. This allows the Save function to detect if a page has ever been deleted.</summary>
		public void SetMinimalStartTimestamp() => this.StartTimestamp = new DateTime(2000, 1, 1);
		#endregion

		#region Internal Methods

		/// <summary>Populates page data from the specified WallE PageItem.</summary>
		/// <param name="pageItem">The page item.</param>
		/// <param name="optionsUsed">The options that were used to load the page. This can be used to reload the page.</param>
		/// <remarks>This item is publicly available so it can be called from other load-like routines if necessary, such as from a PageCollection's LoadPages routine.</remarks>
		internal void Populate(PageItem pageItem, PageLoadOptions optionsUsed)
		{
			// Assumes title-related properties have already been provided in the constructor.
			ThrowNull(pageItem, nameof(pageItem));
			this.loadOptionsUsed = optionsUsed;
			this.PopulateFlags(pageItem.Flags.HasFlag(PageFlags.Invalid), pageItem.Flags.HasFlag(PageFlags.Missing));
			this.PopulateRevisions(pageItem);
			this.PopulateInfo(pageItem);
			this.PopulateLinks(pageItem);
			this.PopulateBacklinks(pageItem);
			this.PopulateProperties(pageItem);
			this.PopulateTemplates(pageItem);
			this.PopulateCategories(pageItem);
			this.PopulateCustomResults(pageItem);
			this.IsLoaded = true;
		}

		/// <summary>Populates only flag data. This is useful for results that return more than straight titles, but less than full page data (e.g., Purge, Watch).</summary>
		/// <param name="invalid">Whether the page is invalid.</param>
		/// <param name="missing">Whether the page is missing.</param>
		internal void PopulateFlags(bool invalid, bool missing)
		{
			this.IsInvalid = invalid;
			this.IsMissing = missing;
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>When overridden in a derived class, allows custom property inputs to be specified as necessary.</summary>
		protected virtual void BuildCustomPropertyInputs()
		{
		}

		/// <summary>When overridden in a derived class, populates custom page properties with custom data from the WallE PageItem.</summary>
		/// <param name="pageItem">The page item.</param>
		protected virtual void PopulateCustomResults(PageItem pageItem)
		{
		}
		#endregion

		#region Private Methods
		private void PopulateBacklinks(PageItem pageItem)
		{
			var backlinks = (Dictionary<Title, BacklinksTypes>)this.Backlinks;
			backlinks.Clear();
			this.PopulateBacklinksType(backlinks, pageItem.FileUsages, BacklinksTypes.ImageUsage);
			this.PopulateBacklinksType(backlinks, pageItem.LinksHere, BacklinksTypes.Backlinks);
			this.PopulateBacklinksType(backlinks, pageItem.TranscludedIn, BacklinksTypes.EmbeddedIn);
		}

		private void PopulateBacklinksType(Dictionary<Title, BacklinksTypes> backlinks, IReadOnlyList<ITitleOptional> list, BacklinksTypes type)
		{
			foreach (var link in list)
			{
				var title = new Title(this.Site, link.Title);
				if (backlinks.ContainsKey(title))
				{
					backlinks[title] |= type;
				}
				else
				{
					backlinks[title] = type;
				}
			}
		}

		private void PopulateCategories(PageItem pageItem)
		{
			var categories = (List<Category>)this.Categories;
			categories.Clear();
			foreach (var category in pageItem.Categories)
			{
				categories.Add(new Category(new FullTitle(this.Site, category.Title), category.SortKey, category.Hidden));
			}
		}

		private void PopulateInfo(PageItem pageItem)
		{
			if (pageItem.Info is PageInfo info)
			{
				this.canonicalPath = info.CanonicalUrl;
				this.CurrentRevisionId = info.LastRevisionId;
				this.editPath = info.EditUrl;
				this.IsNew = info.Flags.HasFlag(PageInfoFlags.New);
				this.IsRedirect = info.Flags.HasFlag(PageInfoFlags.Redirect);
				this.StartTimestamp = pageItem.Info.StartTimestamp;
				this.Text = this.CurrentRevisionId != 0 ? this.CurrentRevision?.Text : null;
			}
			else
			{
				this.canonicalPath = null;
				this.CurrentRevisionId = 0;
				this.editPath = null;
				this.IsNew = false;
				this.IsRedirect = false;
				this.StartTimestamp = this.Site.AbstractionLayer.CurrentTimestamp;
				this.Text = null;
			}
		}

		private void PopulateLinks(PageItem pageItem)
		{
			var links = (List<Title>)this.Links;
			links.Clear();
			foreach (var link in pageItem.Links)
			{
				links.Add(new Title(this.Site, link.Title));
			}
		}

		private void PopulateProperties(PageItem pageItem)
		{
			var properties = (Dictionary<string, string>)this.Properties;
			properties.Clear();
			if (pageItem.Properties?.Count > 0)
			{
				properties.Clear();
				properties.AddRange(pageItem.Properties);
			}
		}

		private void PopulateRevisions(PageItem pageItem)
		{
			var revs = (List<Revision>)this.Revisions;
			revs.Clear();
			this.currentRevision = null;
			foreach (var rev in pageItem.Revisions)
			{
				revs.Add(new Revision(rev));
			}
		}

		private void PopulateTemplates(PageItem pageItem)
		{
			var templates = (List<Title>)this.Templates;
			templates.Clear();
			foreach (var link in pageItem.Templates)
			{
				templates.Add(new Title(this.Site, link.Title));
			}
		}
		#endregion
	}
}