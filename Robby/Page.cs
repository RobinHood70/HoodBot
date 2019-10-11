namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a wiki page.</summary>
	/// <seealso cref="Title" />
	public class Page : Title
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Page" /> class based on site and page name.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		public Page(Site site, string fullPageName)
			: base(site, fullPageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Page" /> class based on site, namespace ID, and the page name without a namespace.</summary>
		/// <param name="ns">The namespace to which the page belongs.</param>
		/// <param name="pageName">The name (only) of the page.</param>
		/// <remarks>Absolutely no cleanup or checking is performed when using this version of the constructor. All values are assumed to already have been validated.</remarks>
		public Page(Namespace ns, string pageName)
			: base(ns, pageName)
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
		public event StrongEventHandler<Page, EventArgs> PageLoaded;
		#endregion

		#region Public Properties

		/// <summary>Gets the page categories, if they were requested in the last load operation.</summary>
		/// <value>The categories the page is listed in.</value>
		public IReadOnlyList<Category> Categories { get; } = new List<Category>();

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
					if (!this.LoadOptions.Modules.HasFlag(PageModules.Properties))
					{
						throw new InvalidOperationException(CurrentCulture(Resources.ModuleNotLoaded, nameof(PageModules.Properties), nameof(this.IsDisambiguation)));
					}

					return this.Properties.ContainsKey("disambiguation");
				}

				if (!this.LoadOptions.Modules.HasFlag(PageModules.Templates))
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

		/// <summary>Gets the links on the page, if they were requested in the last load operation.</summary>
		/// <value>The links used on the page.</value>
		public IReadOnlyList<Title> LinksHere { get; } = new List<Title>();

		/// <summary>Gets or sets the load options.</summary>
		/// <value>The load options.</value>
		/// <remarks>If you need to detect disambiguations, you should include Properties for wikis using Disambiguator or Templates for those that aren't. These are not loaded by default.</remarks>
		public PageLoadOptions LoadOptions { get; set; }

		/// <summary>Gets the page properties, if they were requested in the last load operation.</summary>
		/// <value>The list of page properties.</value>
		public IReadOnlyDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

		/// <summary>Gets the page revisions, if they were requested in the last load operation.</summary>
		/// <value>The revisions list.</value>
		public RevisionCollection Revisions { get; } = new RevisionCollection();

		/// <summary>Gets or sets the timestamp when the page was loaded. Used for edit conflict detection.</summary>
		/// <value>The start timestamp.</value>
		public DateTime? StartTimestamp { get; protected set; }

		/// <summary>Gets the templates used on the page, if they were requested in the last load operation.</summary>
		/// <value>The templates used on the page.</value>
		public IReadOnlyCollection<Title> Templates { get; } = new List<Title>();

		/// <summary>Gets or sets the text, if revisions were requested in the last load operation.</summary>
		/// <value>The page text.</value>
		public string Text { get; set; }

		/// <summary>Gets a value indicating whether the <see cref="Text" /> property has been modified.</summary>
		/// <value><see langword="true" /> if the text no longer matches the first revision; otherwise, <see langword="false" />.</value>
		/// <remarks>This is currently simply a shortcut property to compare the Text with Revisions[0]. This may not be an accurate reflection of modification status when loading a specific revision range or in other unusual circumstances.</remarks>
		public bool TextModified => this.Revisions.Current == null ? !string.IsNullOrWhiteSpace(this.Text) : this.Text != this.Revisions.Current.Text;

		/// <summary>Gets the links on the page, if they were requested in the last load operation.</summary>
		/// <value>The links used on the page.</value>
		public IReadOnlyList<Title> TranscludedIn { get; } = new List<Title>();
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
		public void Load()
		{
			this.LoadOptions ??= this.Site.DefaultLoadOptions;
			this.Reload();
		}

		/// <summary>Loads the specified page modules.</summary>
		/// <param name="pageModules">The page modules.</param>
		public void Load(PageModules pageModules) => this.Load(new PageLoadOptions(pageModules));

		/// <summary>Loads the page with the specified load options.</summary>
		/// <param name="options">The options.</param>
		public void Load(PageLoadOptions options)
		{
			this.LoadOptions = options;
			this.Reload();
		}

		/// <summary>Saves the page.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus Save(string editSummary, bool isMinor) => this.Save(editSummary, isMinor, Tristate.Unknown, false, true);

		/// <summary>Saves the page with full options.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="createOnly">Whether the edit should only occur if it would create a new page (<see cref="Tristate.True"/>), if it would not create a new page (<see cref="Tristate.False"/>) or it doesn't matter (<see cref="Tristate.Unknown"/>).</param>
		/// <param name="recreateIfJustDeleted">Whether to recreate the page if it was deleted since being loaded.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus Save(string editSummary, bool isMinor, Tristate createOnly, bool recreateIfJustDeleted) => this.Save(editSummary, isMinor, createOnly, recreateIfJustDeleted, true);

		/// <summary>Saves the page with full options.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="createOnly">Whether the edit should only occur if it would create a new page (<see cref="Tristate.True"/>), if it would not create a new page (<see cref="Tristate.False"/>) or it doesn't matter (<see cref="Tristate.Unknown"/>).</param>
		/// <param name="recreateIfJustDeleted">Whether to recreate the page if it was deleted since being loaded.</param>
		/// <param name="isBotEdit">Whether the edit should be marked as a bot edit.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus Save(string editSummary, bool isMinor, Tristate createOnly, bool recreateIfJustDeleted, bool isBotEdit)
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
							BaseTimestamp = this.Revisions.Current?.Timestamp,
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
		/// <remarks>This item is publicly available so it can be called from other load-like routines if necessary, such as from a PageCollection's LoadPages routine.</remarks>
		internal void Populate(PageItem pageItem)
		{
			// Assumes title-related properties have already been provided in the constructor.
			ThrowNull(pageItem, nameof(pageItem));

			// None
			this.PopulateFlags(pageItem.Flags.HasFlag(PageFlags.Invalid), pageItem.Flags.HasFlag(PageFlags.Missing));

			// Info
			var info = pageItem.Info;
			if (info == null)
			{
				this.IsNew = false;
				this.IsRedirect = false;
				this.StartTimestamp = this.Site.AbstractionLayer.CurrentTimestamp;
			}
			else
			{
				this.IsNew = info.Flags.HasFlag(PageInfoFlags.New);
				this.IsRedirect = info.Flags.HasFlag(PageInfoFlags.Redirect);
				this.StartTimestamp = pageItem.Info.StartTimestamp;
			}

			// Revisions
			var revs = this.Revisions;
			revs.Clear();
			foreach (var rev in pageItem.Revisions)
			{
				revs.Add(new Revision(
					rev.Flags.HasFlag(RevisionFlags.Anonymous),
					rev.Comment,
					rev.RevisionId,
					rev.Flags.HasFlag(RevisionFlags.Minor),
					rev.ParentId,
					rev.Content,
					rev.Timestamp,
					rev.User));
			}

			if (info != null)
			{
				if (revs.TryGetValue(info.LastRevisionId, out var revision))
				{
					revs.Current = revision;
					this.Text = revision.Text;
				}
				else
				{
					// Debug.WriteLine($"Revision {info.LastRevisionId} not found on {pageItem.Title}. Should it have been? Current revision for page is {revs.Current?.Id}.");

					// Blank the text, since it's not the current page text. We don't set revs.Current here because it will either have been set internally by .Add or set by a successful try.
					this.Text = null;
				}
			}

			// Links
			var links = this.Links as List<Title>;
			links.Clear();
			foreach (var link in pageItem.Links)
			{
				links.Add(new Title(this.Site, link.Title));
			}

			// LinksHere
			var linksHere = this.LinksHere as List<Title>;
			linksHere.Clear();
			foreach (var linkHere in pageItem.LinksHere)
			{
				linksHere.Add(new Title(this.Site, linkHere.Title));
			}

			// Properties
			var properties = this.Properties as Dictionary<string, string>;
			properties.Clear();
			if (pageItem.Properties?.Count > 0)
			{
				properties.Clear();
				foreach (var property in pageItem.Properties)
				{
					properties.Add(property.Key, property.Value);
				}
			}

			// Templates
			var templates = this.Templates as List<Title>;
			templates.Clear();
			foreach (var link in pageItem.Templates)
			{
				templates.Add(new Title(this.Site, link.Title));
			}

			// TranscludedIn
			var transcludedIn = this.TranscludedIn as List<Title>;
			transcludedIn.Clear();
			foreach (var transclusionHere in pageItem.TranscludedIn)
			{
				transcludedIn.Add(new Title(this.Site, transclusionHere.Title));
			}

			// Categories
			var categories = this.Categories as List<Category>;
			categories.Clear();
			foreach (var category in pageItem.Categories)
			{
				categories.Add(new Category(new TitleParts(this.Site, category.Title), category.SortKey, category.Hidden));
			}

			// Custom
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

		/// <summary>Reloads the page with the current load options.</summary>
		private void Reload()
		{
			var creator = this.Site.PageCreator;
			var propertyInputs = creator.GetPropertyInputs(this.LoadOptions);
			var pageSetInput = new QueryPageSetInput(new[] { this.FullPageName }) { ConvertTitles = this.LoadOptions.ConvertTitles, Redirects = this.LoadOptions.FollowRedirects };
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, propertyInputs, creator.CreatePageItem);
			this.Populate(result.First());
			this.PageLoaded?.Invoke(this, EventArgs.Empty);
		}
		#endregion
	}
}