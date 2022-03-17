namespace RobinHood70.Robby
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;

	/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
	public abstract class SimpleTitle
	{
		#region Fields
		private Title? subjectPage;
		private Title? talkPage;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SimpleTitle"/> class.</summary>
		/// <param name="title">The title to copy from.</param>
		protected SimpleTitle([NotNull, ValidatedNotNull] SimpleTitle title)
		{
			title.ThrowNull(nameof(title));
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
		}

		/// <summary>Initializes a new instance of the <see cref="SimpleTitle"/> class.</summary>
		/// <param name="ns">The namespace the title is in.</param>
		/// <param name="pageName">The page name.</param>
		protected SimpleTitle([NotNull, ValidatedNotNull] Namespace ns, [NotNull, ValidatedNotNull] string pageName)
		{
			this.Namespace = ns.NotNull(nameof(ns));
			this.PageName = pageName.NotNull(nameof(pageName));
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
		public SimpleTitle SubjectPage => this.subjectPage ??= Title.FromValidated(this.Namespace.SubjectSpace, this.PageName);

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
		public SimpleTitle? TalkPage => this.talkPage ??=
			this.Namespace.TalkSpace == null ? null :
			Title.FromValidated(this.Namespace.TalkSpace, this.PageName);
		#endregion

		#region Public Methods

		/// <summary>Gets the article path for the current page.</summary>
		/// <returns>A Uri to the index.php page.</returns>
		public Uri GetArticlePath() => this.Site.GetArticlePath(this.FullPageName);

		/// <summary>Compares two objects for <see cref="Namespace"/> and <see cref="PageName"/> equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public bool SimpleEquals(SimpleTitle? other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName, false);
		#endregion

		#region Public Virtual Methods

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

		/// <inheritdoc/>
		public override string ToString() => this.ToString(false);
		#endregion
	}
}
