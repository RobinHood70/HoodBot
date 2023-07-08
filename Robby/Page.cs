namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>Represents a wiki page.</summary>
	/// <seealso cref="Title" />
	public class Page : Title
	{
		#region Fields
		private Uri? canonicalPath;
		private Revision? currentRevision;
		private Uri? editPath;
		private string text = string.Empty;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Page"/> class.</summary>
		/// <param name="title">The <see cref="Title"/> to copy values from.</param>
		/// <param name="options">The load options used for this page. Can be used to detect if default-valued information is legitimate or was never loaded.</param>
		/// <param name="apiItem">The API item to extract information from.</param>
		protected internal Page([NotNull, ValidatedNotNull] Title title, PageLoadOptions options, IApiTitle? apiItem)
			: base(title)
		{
			// TODO: This should probably be re-written as some kind of inheritance thing, but I'm not qute sure how that would work and it's not the priority right now.
			this.LoadOptions = options;
			switch (apiItem)
			{
				case null:
					break;
				case PurgeItem purgeItem:
					this.IsInvalid = purgeItem.Flags.HasAnyFlag(PurgeFlags.Invalid);
					this.IsMissing = purgeItem.Flags.HasAnyFlag(PurgeFlags.Missing);
					break;
				case WatchItem watchItem:
					this.IsInvalid = false;
					this.IsMissing = watchItem.Flags.HasAnyFlag(WatchFlags.Missing);
					break;
				case PageItem pageItem:
					this.IsInvalid = pageItem.Flags.HasAnyFlag(PageFlags.Invalid);
					this.IsMissing = pageItem.Flags.HasAnyFlag(PageFlags.Missing);
					PopulateRevisions(pageItem);
					PopulateInfo(pageItem);
					this.PreviouslyDeleted = this.IsMissing && pageItem.DeletedRevisions.Count > 0;
					PopulateLinks(pageItem);
					PopulateBacklinks(pageItem);
					PopulateProperties(pageItem);
					PopulateTemplates(pageItem);
					PopulateCategories(pageItem);

					this.IsLoaded = true;
					break;
			}

			void PopulateBacklinks(PageItem pageItem)
			{
				var backlinks = (Dictionary<Title, BacklinksTypes>)this.Backlinks;
				backlinks.Clear();
				PopulateBacklinksType(backlinks, pageItem.FileUsages, BacklinksTypes.ImageUsage);
				PopulateBacklinksType(backlinks, pageItem.LinksHere, BacklinksTypes.Backlinks);
				PopulateBacklinksType(backlinks, pageItem.TranscludedIn, BacklinksTypes.EmbeddedIn);
			}

			void PopulateBacklinksType(Dictionary<Title, BacklinksTypes> backlinks, IReadOnlyList<IApiTitleOptional> list, BacklinksTypes type)
			{
				foreach (var link in list)
				{
					link.FullPageName.PropertyThrowNull(nameof(link));
					Title title = TitleFactory.FromUnvalidated(this.Site, link.FullPageName);
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

			void PopulateCategories(PageItem pageItem)
			{
				var categories = (List<Category>)this.Categories;
				categories.Clear();
				foreach (var category in pageItem.Categories)
				{
					var factory = TitleFactory.CoValidate(this.Site, category.Namespace, category.FullPageName);
					categories.Add(new Category(factory, category.SortKey, category.Hidden));
				}
			}

			void PopulateInfo(PageItem pageItem)
			{
				var protections = (Dictionary<string, ProtectionEntry>)this.Protections;
				if (pageItem.Info is PageInfo info)
				{
					this.canonicalPath = info.CanonicalUrl;
					this.CurrentRevisionId = info.LastRevisionId;
					this.editPath = info.EditUrl;
					this.IsNew = info.Flags.HasAnyFlag(PageInfoFlags.New);
					this.IsRedirect = info.Flags.HasAnyFlag(PageInfoFlags.Redirect);
					this.StartTimestamp = pageItem.Info.StartTimestamp ?? this.Site.AbstractionLayer.CurrentTimestamp;
					this.Text = this.CurrentRevisionId != 0 ? this.CurrentRevision?.Text : null;
					foreach (var protItem in pageItem.Info.Protections)
					{
						protections.Add(protItem.Type, new ProtectionEntry(protItem));
					}
				}
				else
				{
					this.canonicalPath = null;
					this.CurrentRevisionId = 0;
					this.editPath = null;
					this.IsNew = false;
					this.IsRedirect = false;
					protections.Clear();
					this.StartTimestamp = this.Site.AbstractionLayer.CurrentTimestamp;
					this.Text = null;
				}
			}

			void PopulateLinks(PageItem pageItem)
			{
				var links = (List<Title>)this.Links;
				links.Clear();
				foreach (var link in pageItem.Links)
				{
					links.Add(TitleFactory.FromUnvalidated(this.Site, link.FullPageName));
				}
			}

			void PopulateProperties(PageItem pageItem)
			{
				var properties = (Dictionary<string, string>)this.Properties;
				properties.Clear();
				if (pageItem.Properties?.Count > 0)
				{
					properties.Clear();
					properties.AddRange(pageItem.Properties);
				}
			}

			void PopulateRevisions(PageItem pageItem)
			{
				var revs = (List<Revision>)this.Revisions;
				revs.Clear();
				this.currentRevision = null;
				foreach (var rev in pageItem.Revisions)
				{
					revs.Add(new Revision(rev));
				}
			}

			void PopulateTemplates(PageItem pageItem)
			{
				var templates = (List<Title>)this.Templates;
				templates.Clear();
				foreach (var link in pageItem.Templates)
				{
					templates.Add(TitleFactory.FromUnvalidated(this.Site, link.FullPageName));
				}
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the backlinks on the page if they were requested in the last load operation.</summary>
		/// <value>The links used on the page.</value>
		/// <remarks>This includes links, transclusions, and file usage.</remarks>
		public IReadOnlyDictionary<Title, BacklinksTypes> Backlinks { get; } = new Dictionary<Title, BacklinksTypes>();

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
		public Revision? CurrentRevision => this.CurrentRevisionId == 0
			? null
			: this.currentRevision ??= ((List<Revision>)this.Revisions).Find(item => this.CurrentRevisionId > 0 && item.Id == this.CurrentRevisionId);

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
		/// <returns><see langword="true" /> if this instance is disambiguation; otherwise, <see langword="false" />. On wikis where the Disambiguator extension isn't in use, this will fall back to a template-based approach. In that event, this can also return <see langword="null"/> if the <see cref="Templates"/> for the page aren't loaded or if there are no disambiguation templates.</returns>
		public bool? IsDisambiguation
		{
			get
			{
				// Disambiguator is 1.21+, so we don't need to worry about the fact that page properties are 1.17+.
				if (this.Site.DisambiguatorAvailable && this.ModuleLoaded(PageModules.Properties))
				{
					return this.Properties.ContainsKey("disambiguation");
				}

				if (!this.ModuleLoaded(PageModules.Templates) ||
					this.Site.DisambiguationTemplates.Count == 0)
				{
					return null;
				}

				HashSet<Title> templates = new(this.Templates);
				templates.IntersectWith(this.Site.DisambiguationTemplates);

				return templates.Count > 0;
			}
		}

		/// <summary>Gets or sets a value indicating whether this <see cref="Page" /> is invalid.</summary>
		/// <value><see langword="true" /> if invalid; otherwise, <see langword="false" />.</value>
		public bool IsInvalid { get; protected set; }

		/// <summary>Gets a value indicating whether this <see cref="Page" /> has been loaded.</summary>
		/// <value><see langword="true" /> if loaded; otherwise, <see langword="false" />.</value>
		public bool IsLoaded { get; }

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

		/// <summary>Gets the information that was loaded for this page.</summary>
		/// <value>The load options.</value>
		public PageLoadOptions LoadOptions { get; } = PageLoadOptions.None;

		/// <summary>Gets or sets a value indicating whether this <see cref="Page"/> has previously been deleted.</summary>
		/// <value><see langword="true"/> if the page has previously been deleted; toherwise, <see langword="false"/>.</value>
		public bool PreviouslyDeleted { get; protected set; }

		/// <summary>Gets the page properties, if they were requested in the last load operation.</summary>
		/// <value>The list of page properties.</value>
		public IReadOnlyDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>Gets the protection entries for the page.</summary>
		/// <value>The protection entries.</value>
		public IReadOnlyDictionary<string, ProtectionEntry> Protections { get; } = new Dictionary<string, ProtectionEntry>(StringComparer.OrdinalIgnoreCase);

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
		public bool TextModified => !string.Equals(this.Text, this.CurrentRevision?.Text ?? string.Empty, StringComparison.Ordinal);
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new page from a <see cref="Title"/> object.</summary>
		/// <param name="title">The title to use.</param>
		/// <returns>A new page based on the title.</returns>
		public static Page FromTitle(Title title)
		{
			ArgumentNullException.ThrowIfNull(title);
			return title.Site.CreatePage(title);
		}

		/// <summary>Creates a new page from a <see cref="Title"/> object, filled with the supplied text.</summary>
		/// <param name="title">The title to use.</param>
		/// <param name="text">The text of the page.</param>
		/// <returns>A new page based on the title.</returns>
		public static Page FromTitle(Title title, string text)
		{
			ArgumentNullException.ThrowIfNull(title);
			return title.Site.CreatePage(title, text);
		}
		#endregion

		#region Public Methods

		/// <summary>Convenience method to determine if the page has a specific module loaded.</summary>
		/// <param name="module">The module to check.</param>
		/// <returns><see langword="true"/> if LoadOptions.Modules includes the specified module; otherwise, <see langword="false"/>.</returns>
		public bool ModuleLoaded(PageModules module) => this.LoadOptions.Modules.HasAnyFlag(module);

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

			PageTextChangeArgs changeArgs = new(this, nameof(this.Save), editSummary, isMinor, isBotEdit, recreateIfJustDeleted);

			return this.Site.PublishPageTextChange(changeArgs, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				// Modification status re-checked here because a subscriber may have reverted the page during PublishPageTextChage.
				if (!this.TextModified)
				{
					return ChangeStatus.NoEffect;
				}

				EditInput input = new(this.FullPageName, this.Text)
				{
					BaseTimestamp = this.CurrentRevision?.Timestamp,
					StartTimestamp = this.StartTimestamp,
					Bot = changeArgs.BotEdit,
					Minor = changeArgs.Minor ? Tristate.True : Tristate.False,
					Recreate = changeArgs.RecreateIfJustDeleted,
					Summary = changeArgs.EditSummary,
					RequireNewPage = createOnly,
				};

				var retval = this.Site.AbstractionLayer.Edit(input);
				return retval.Flags.HasAnyFlag(EditFlags.NoChange)
					? ChangeStatus.NoEffect
					: string.Equals(retval.Result, "Success", StringComparison.Ordinal)
						? ChangeStatus.Success
						: ChangeStatus.Failure;
			}
		}

		/// <summary>Saves the page text under a new name, overwriting any previous text.</summary>
		/// <param name="saveName">The name to save the page as.</param>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="createOnly">Whether the edit should only occur if it would create a new page (<see cref="Tristate.True"/>), if it would not create a new page (<see cref="Tristate.False"/>) or it doesn't matter (<see cref="Tristate.Unknown"/>).</param>
		/// <param name="recreateIfJustDeleted">Whether to recreate the page if it was deleted since being loaded.</param>
		/// <param name="isBotEdit">Whether the edit should be marked as a bot edit.</param>
		/// <returns>A value indicating the change status of the edit.</returns>
		public ChangeStatus SaveAs(string saveName, [Localizable(true)] string editSummary, bool isMinor, Tristate createOnly, bool recreateIfJustDeleted, bool isBotEdit)
		{
			PageTextChangeArgs changeArgs = new(this, nameof(this.SaveAs), editSummary, isMinor, isBotEdit, recreateIfJustDeleted);
			return this.Site.PublishPageTextChange(changeArgs, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				EditInput input = new(saveName, this.Text)
				{
					Bot = changeArgs.BotEdit,
					Minor = changeArgs.Minor ? Tristate.True : Tristate.False,
					Recreate = changeArgs.RecreateIfJustDeleted,
					Summary = changeArgs.EditSummary,
					RequireNewPage = createOnly,
				};

				var retval = this.Site.AbstractionLayer.Edit(input);
				return retval.Flags.HasAnyFlag(EditFlags.NoChange)
					? ChangeStatus.NoEffect
					: string.Equals(retval.Result, "Success", StringComparison.Ordinal)
						? ChangeStatus.Success
						: ChangeStatus.Failure;
			}
		}

		/// <summary>Sets <see cref="StartTimestamp"/> to 2000-01-01. This allows the Save function to detect if a page has ever been deleted.</summary>
		public void SetMinimalStartTimestamp() => this.StartTimestamp = new DateTime(2000, 1, 1);
		#endregion

		#region Protected Virtual Methods

		/// <summary>When overridden in a derived class, allows custom property inputs to be specified as necessary.</summary>
		protected virtual void BuildCustomPropertyInputs()
		{
		}
		#endregion
	}
}