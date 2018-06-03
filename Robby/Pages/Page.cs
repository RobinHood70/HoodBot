namespace RobinHood70.Robby.Pages
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	/// <summary>Represents a wiki page.</summary>
	/// <seealso cref="Title" />
	public class Page : Title
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Page" /> class based on site and page name.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		public Page(Site site, string fullPageName)
			: base(site, fullPageName, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Page" /> class based on site, page name, and an arbitrary key.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <param name="key">The key.</param>
		public Page(Site site, string fullPageName, string key)
			: base(site, fullPageName, key)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Page" /> class based on site, namespace id, and the page name without a namespace.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="ns">The namespace to which the page belongs.</param>
		/// <param name="pageName">The name (only) of the page.</param>
		/// <remarks>Absolutely no cleanup or checking is performed when using this version of the constructor. All values are assumed to already have been validated.</remarks>
		public Page(Site site, int ns, string pageName)
			: base(site, ns, pageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Page" /> class from another IWikiTitle-based object.</summary>
		/// <param name="title">The Title object to copy from.</param>
		public Page(IWikiTitle title)
			: base(title)
		{
		}
		#endregion

		#region Public Events

		/// <summary>Occurs when the page is loaded.</summary>
		public event StrongEventHandler<Page, EventArgs> PageLoaded;
		#endregion

		#region Public Properties

		/// <summary>Gets the page categories, if they were requested in the last load operation.</summary>
		/// <value>The categories the page is listed in.</value>
		public IReadOnlyList<CategoryTitle> Categories { get; } = new List<CategoryTitle>();

		/// <summary>Gets a value indicating whether this <see cref="Page" /> exists.</summary>
		/// <value><c>true</c> if the page exists; otherwise, <c>false</c>.</value>
		public bool Exists => !this.Missing && !this.Invalid;

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is invalid.</summary>
		/// <value><c>true</c> if invalid; otherwise, <c>false</c>.</value>
		public bool Invalid { get; protected set; }

		/// <summary>Gets the links on the page, if they were requested in the last load operation.</summary>
		/// <value>The links used on the page.</value>
		public IReadOnlyList<Title> Links { get; } = new List<Title>();

		/// <summary>Gets a value indicating whether this <see cref="Page" /> has been loaded.</summary>
		/// <value><c>true</c> if loaded; otherwise, <c>false</c>.</value>
		public bool Loaded { get; private set; }

		/// <summary>Gets or sets the load options.</summary>
		/// <value>The load options.</value>
		public PageLoadOptions LoadOptions { get; set; }

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is missing.</summary>
		/// <value><c>true</c> if the page is missing; otherwise, <c>false</c>.</value>
		public bool Missing { get; protected set; } = true;

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is new.</summary>
		/// <value><c>true</c> if the page is new; otherwise, <c>false</c>.</value>
		public bool New { get; protected set; }

		/// <summary>Gets the page properties, if they were requested in the last load operation.</summary>
		/// <value>The list of page properties.</value>
		public IReadOnlyDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is a redirect.</summary>
		/// <value><c>true</c> if the page is a redirect; otherwise, <c>false</c>.</value>
		public bool Redirect { get; protected set; }

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
		/// <value><c>true</c> if the text no longer matches the first revision; otherwise, <c>false</c>.</value>
		/// <remarks>This is currently simply a shortcut property to compare the Text with Revisions[0]. This may not be an accurate reflection of modification status when loading a specific revision range or in other unusual circumstances.</remarks>
		public bool TextModified
		{
			get
			{
				if (this.Revisions.Count > 0)
				{
					return this.Text != this.Revisions[0].Text;
				}

				return false;
			}
		}
		#endregion

		#region Public Static Methods

		/// <summary>Returns a value indicating whether the page exists.</summary>
		/// <param name="site">The site to search on.</param>
		/// <param name="fullName">The full name.</param>
		/// <returns>A value indicating whether the page exists.</returns>
		public static bool CheckExistence(Site site, string fullName) => new Page(site, fullName).CheckExistence();
		#endregion

		#region Public Methods

		/// <summary>Returns a value indicating if the page exists. This will trigger a Load operation, if necessary.</summary>
		/// <returns><c>true</c> if the page exists; otherwise <c>false</c>.</returns>
		public bool CheckExistence()
		{
			if (!this.Loaded)
			{
				this.Load(PageModules.None);
			}

			return this.Exists;
		}

		/// <summary>Determines whether the page represents a disambiguation page.</summary>
		/// <returns><c>true</c> if this instance is disambiguation; otherwise, <c>false</c>.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the page properties (MW 1.21+) or templates (MW 1.20-) weren't loaded prior to checking.</exception>
		public bool IsDisambiguation()
		{
			if (this.Site.DisambiguatorAvailable)
			{
				// Disambiguator is 1.21+, so we don't need to worry about the fact that page properties are 1.17+.
				if (!this.LoadOptions.Modules.HasFlag(PageModules.Properties))
				{
					throw new InvalidOperationException(CurrentCulture(ModuleNotLoaded, nameof(PageModules.Properties), nameof(this.IsDisambiguation)));
				}

				return this.Properties.ContainsKey("disambiguation");
			}

			if (!this.LoadOptions.Modules.HasFlag(PageModules.Templates))
			{
				throw new InvalidOperationException(CurrentCulture(ModuleNotLoaded, nameof(PageModules.Templates), nameof(this.IsDisambiguation)));
			}

			var templates = new HashSet<Title>(this.Templates, new WikiTitleEqualityComparer());
			templates.IntersectWith(this.Site.DisambiguationTemplates);
			return templates.Count > 0;
		}

		/// <summary>Loads or reloads the page.</summary>
		public void Load()
		{
			this.LoadOptions = this.LoadOptions ?? this.Site.DefaultLoadOptions;
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

		/// <summary>Populates page data from the specified WallE PageItem.</summary>
		/// <param name="pageItem">The page item.</param>
		/// <remarks>This item is publicly available so it can be called from other load-like routines if necessary, such as from a PageCollection's bulk load.</remarks>
		public void Populate(PageItem pageItem)
		{
			// Assumes title-related properties have already been provided in the constructor.
			ThrowNull(pageItem, nameof(pageItem));
			var flags = pageItem.Flags;

			var categories = this.Categories as List<CategoryTitle>;
			categories.Clear();
			foreach (var category in pageItem.Categories)
			{
				categories.Add(new CategoryTitle(this.Site, category.Title, category.SortKey, category.Hidden));
			}

			var links = this.Links as List<Title>;
			links.Clear();
			foreach (var link in pageItem.Links)
			{
				links.Add(new Title(this.Site, link.Title));
			}

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

			var templates = this.Templates as List<Title>;
			templates.Clear();
			if (pageItem.Templates.Count > 0)
			{
				templates.Clear();
				foreach (var link in pageItem.Templates)
				{
					templates.Add(new Title(this.Site, link.Title));
				}
			}

			var revs = this.Revisions;
			revs.Clear();
			foreach (var rev in pageItem.Revisions)
			{
				revs.Add(new Revision(rev));
			}

			this.Invalid = flags.HasFlag(PageFlags.Invalid);
			this.Missing = flags.HasFlag(PageFlags.Missing);
			this.StartTimestamp = this.Site.AbstractionLayer.CurrentTimestamp;

			var info = pageItem.Info;
			if (info == null)
			{
				this.New = false;
				this.Redirect = false;
			}
			else
			{
				this.New = info.Flags.HasFlag(PageInfoFlags.New);
				this.Redirect = info.Flags.HasFlag(PageInfoFlags.Redirect);
				try
				{
					revs.Current = revs[info.LastRevisionId];
					this.Text = revs.Current.Text;
				}
				catch (KeyNotFoundException)
				{
					// We don't set revs.Current here because it will either have been set internally by .Add or set by a successful try.
					this.Text = null;
				}
			}

			this.PopulateCustomResults(pageItem);
		}

		/// <summary>Saves the page.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		public void Save(string editSummary, bool isMinor) => this.Save(editSummary, isMinor, true, false);

		/// <summary>Saves the page with full options.</summary>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="isBotEdit">Whether the edit should be marked as a bot edit.</param>
		/// <param name="recreateIfJustDeleted">Whether to recreate the page if it was deleted since being loaded.</param>
		public void Save(string editSummary, bool isMinor, bool isBotEdit, bool recreateIfJustDeleted)
		{
			if (!this.Site.AllowEditing)
			{
				return;
			}

			var input = new EditInput(this.FullPageName, this.Text)
			{
				BaseTimestamp = this.Revisions?.Current?.Timestamp,
				StartTimestamp = this.StartTimestamp,
				Bot = isBotEdit,
				Minor = isMinor.ToTristate(),
				Recreate = recreateIfJustDeleted,
				Summary = editSummary,
			};
			this.Site.AbstractionLayer.Edit(input);
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

		/// <summary>This should be called whenever the page is loaded.</summary>
		protected virtual void OnLoaded() => this.PageLoaded?.Invoke(this, EventArgs.Empty);
		#endregion

		#region Private Methods

		/// <summary>Reloads the page with the current load options.</summary>
		private void Reload()
		{
			var creator = this.Site.PageCreator;
			var propertyInputs = creator.GetPropertyInputs(this.LoadOptions);
			var pageSetInput = new DefaultPageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, propertyInputs, creator.CreatePageItem);
			this.Populate(result.First());
			this.Loaded = true;
			this.OnLoaded();
		}
		#endregion
	}
}