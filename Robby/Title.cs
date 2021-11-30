namespace RobinHood70.Robby
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	#region Public Enumerations

	/// <summary>The possible protection levels on a standard wiki.</summary>
	public enum ProtectionLevel
	{
		/// <summary>Do not make any changes to the protection.</summary>
		NoChange,

		/// <summary>Remove the protection.</summary>
		Remove,

		/// <summary>Change to semi-protection.</summary>
		Semi,

		/// <summary>Change to full-protection.</summary>
		Full,
	}
	#endregion

	// TODO: Convert this to a Record and rewrite all classes deriving from it (possibly using interfaces with a composite structure, i.e. has a Title rather than is a Title). Consider splitting into a simple NS/Name vs. full Title design, or something similar, so things like SubjectPage/TalkPage can be created without special considerations for Titles within Titles.

	/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
	public class Title : IMessageSource, ISimpleTitle
	{
		#region Fields
		private Title? subjectPage;
		private Title? talkPage;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> to copy values from.</param>
		public Title([NotNull, ValidatedNotNull] ISimpleTitle title)
		{
			title.ThrowNull(nameof(title));
			this.Namespace = title.Namespace.NotNull(nameof(title), nameof(title.Namespace));
			this.PageName = title.PageName.NotNull(nameof(title), nameof(title.PageName));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string BasePageName => this.Namespace.AllowsSubpages && this.PageName.LastIndexOf('/') is var subPageLoc && subPageLoc > 0
				? this.PageName[..subPageLoc]
				: this.PageName;

		/// <summary>Gets the full page name of a title.</summary>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		public string FullPageName => this.Namespace.DecoratedName + this.PageName;

		/// <summary>Gets the namespace object for the title.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; }

		/// <summary>Gets the value corresponding to {{ROOTPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string RootPageName =>
			this.Namespace.AllowsSubpages &&
			this.PageName.IndexOf('/', StringComparison.Ordinal) is var subPageLoc &&
			subPageLoc >= 0
				? this.PageName[..subPageLoc]
				: this.PageName;

		/// <summary>Gets the site to which this title belongs.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The subject page.</returns>
		/// <remarks>If title Title is a subject page, returns itself.</remarks>
		public Title SubjectPage => this.subjectPage ??= this.Namespace.IsSubjectSpace
			? this
			: TitleFactory.DirectNormalized(this.Namespace.SubjectSpace, this.PageName).ToTitle();

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <returns>The name of the subpage.</returns>
		public string SubPageName =>
			this.Namespace.AllowsSubpages &&
			(this.PageName.LastIndexOf('/') + 1) is var subPageLoc &&
			subPageLoc > 0
				? this.PageName[subPageLoc..]
				: this.PageName;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The talk page.</returns>
		/// <remarks>If this object represents a talk page, returns a self-reference.</remarks>
		public Title? TalkPage => this.talkPage ??=
			this.Namespace.TalkSpace == null ? null :
			this.Namespace.IsTalkSpace ? this :
			TitleFactory.DirectNormalized(this.Namespace.TalkSpace, this.PageName).ToTitle();
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new instance of the <see cref="Title"/> class from the page name, placing it in the default namespace if no other namespace is present in the name.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <returns>A new <see cref="Title"/> with the namespace found in <paramref name="pageName"/>, if there is one, otherwise using <paramref name="defaultNamespace"/>.</returns>
		public static Title Coerce(Site site, int defaultNamespace, string pageName) => TitleFactory.FromName(site.NotNull(nameof(site)), defaultNamespace, pageName.NotNull(nameof(pageName))).ToTitle();

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="node">The <see cref="IBacklinkNode"/> to parse.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		public static Title FromBacklinkNode(Site site, IBacklinkNode node)
		{
			var title = node.NotNull(nameof(node)).GetTitleText();
			return TitleFactory.FromName(site.NotNull(nameof(site)), title).ToTitle();
		}
		#endregion

		#region Public Methods

		/// <summary>Returns a value indicating if the page exists. This will trigger a Load operation.</summary>
		/// <returns><see langword="true" /> if the page exists; otherwise <see langword="false" />.</returns>
		public bool PageExists() => this.Load(PageModules.None, false).Exists;

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.SimpleEquals(obj as Title);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.Namespace, this.PageName);

		/// <summary>Gets the article path for the current page.</summary>
		/// <returns>A Uri to the index.php page.</returns>
		public Uri GetArticlePath() => this.Site.GetArticlePath(this.FullPageName);

		/// <summary>Loads the page found at this title.</summary>
		/// <returns>A page for this title. Can be null if the title is a Special or Media page.</returns>
		public Page Load() => this.Load(PageLoadOptions.Default);

		/// <summary>Loads the page found at this title.</summary>
		/// <param name="modules">The modules to load.</param>
		/// <param name="followRedirects">Indicates whether redirects should be followed when loading.</param>
		/// <returns>A page for this title. Can be null if the title is a Special or Media page.</returns>
		public Page Load(PageModules modules, bool followRedirects) => this.Load(new PageLoadOptions(modules, followRedirects));

		/// <summary>Loads the page found at this title.</summary>
		/// <param name="options">The page load options.</param>
		/// <returns>A page for this title.</returns>
		public Page Load(PageLoadOptions options)
		{
			if (this.Namespace.CanTalk)
			{
				PageCollection? pages = PageCollection.Unlimited(this.Site, options);
				pages.GetTitles(this);
				if (pages.Count == 1)
				{
					return pages[0];
				}
			}

			throw new InvalidOperationException(Globals.CurrentCulture(Resources.PageCouldNotBeLoaded, this.FullPageName));
		}

		/// <summary>Checks if the provided page name is equal to the title's page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="other">The page name to compare to.</param>
		/// <returns><see langword="true" /> if the two page names are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string other) => this.PageNameEquals(other, true);

		/// <summary>Checks if the provided page name is equal to the title's page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="other">The page name to compare to.</param>
		/// <param name="normalize">Inidicates whether the page names should be normalized before comparison.</param>
		/// <returns><see langword="true" /> if the two page names are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string other, bool normalize)
		{
			if (normalize)
			{
				other = WikiTextUtilities.DecodeAndNormalize(other).Trim();
			}

			return this.Namespace.PageNameEquals(this.PageName, other, false);
		}

		/// <summary>Compares two objects for <see cref="Namespace"/> and <see cref="PageName"/> equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public bool SimpleEquals(ISimpleTitle? other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName, false);

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File), regardless of the value of LeadingColon.</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public virtual string ToString(bool forceLink)
		{
			var colon = (forceLink && this.Namespace.IsForcedLinkSpace) ? ":" : string.Empty;
			return colon + this.FullPageName;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a string that represents the current Title.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString() => this.ToString(false);
		#endregion
	}
}
