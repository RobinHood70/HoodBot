namespace RobinHood70.Robby.Design
{
	using System;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class FullTitle : Title, IFullTitle, ISimpleTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="title">The ISimpleTitle to copy values from.</param>
		public FullTitle(ISimpleTitle title)
			: base(title)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="title">The ISimpleTitle to copy values from.</param>
		public FullTitle(IFullTitle title)
			: this(title as ISimpleTitle)
		{
			ThrowNull(title, nameof(title));
			this.Interwiki = title.Interwiki;
			this.Fragment = title.Fragment;
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public FullTitle(Site site, string fullPageName)
			: this(site, MediaWikiNamespaces.Main, fullPageName, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <param name="forceNamespace">If <see langword="true"/>, the namespace specified will always be used, even if the pageName begins with what looks like a namespace or interwiki prefix.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public FullTitle(Site site, int defaultNamespace, string pageName, bool forceNamespace)
			: this(new TitleParser(site, defaultNamespace, pageName, forceNamespace))
		{
		}

		// Designed for data coming directly from MediaWiki. Assumes all values are appropriate and pre-trimmed - only does namespace parsing. interWiki and fragment may be null; fullPageName may not.
		internal FullTitle(Site site, string? interWiki, string fullPageName, string? fragment)
			: this(site, fullPageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			if (interWiki != null)
			{
				this.Interwiki = site.InterwikiMap[interWiki];
			}

			var split = fullPageName.Split(TextArrays.Colon, 2);
			if ((this.Interwiki == null || this.Interwiki.LocalWiki) && site.Namespaces.ValueOrDefault(split[0]) is Namespace ns)
			{
				this.NamespaceId = ns.Id;
				this.PageName = split[1];
			}
			else
			{
				this.NamespaceId = MediaWikiNamespaces.Main;
				this.PageName = fullPageName;
			}

			if (fragment != null)
			{
				this.Fragment = fragment;
			}
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="parser">The <see cref="TitleParser"/> with the desired information.</param>
		protected FullTitle(TitleParser parser)
			: base(parser)
		{
			this.Interwiki = parser.Interwiki;
			this.Fragment = parser.Fragment;
			this.Coerced = parser.Coerced;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title's fragment (the section or ID to scroll to).</summary>
		/// <value>The fragment.</value>
		public string? Fragment { get; set; }

		/// <summary>Gets or sets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public InterwikiEntry? Interwiki { get; set; }

		/// <summary>Gets a value indicating whether this instance is identical to the local wiki.</summary>
		/// <value><see langword="true"/> if this instance is local wiki; otherwise, <see langword="false"/>.</value>
		public bool IsLocal => this.Interwiki == null || this.Interwiki.LocalWiki;
		#endregion

		#region Public Override Properties

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <value>The name of the label.</value>
		/// <remarks>Unlike the regular pipe trick, this will take a non-empty fragment name in preference to the page name.</remarks>
		public override string LabelName => string.IsNullOrWhiteSpace(this.Fragment) ? PipeTrick(this.PageName) : this.Fragment.Trim();
		#endregion

		#region Public Methods

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="site">The value returned by <see cref="Site"/>.</param>
		/// <param name="leadingColon">The value returned by <see cref="Title.LeadingColon"/>.</param>
		/// <param name="interwiki">The value returned by <see cref="Interwiki"/>.</param>
		/// <param name="ns">The value returned by <see cref="Title.NamespaceId"/>.</param>
		/// <param name="pageName">The value returned by <see cref="Title.PageName"/>.</param>
		/// <param name="fragment">The value returned by <see cref="Fragment"/>.</param>
		public void Deconstruct(out Site site, out bool leadingColon, out InterwikiEntry? interwiki, out int ns, out string pageName, out string? fragment)
		{
			site = this.Site;
			leadingColon = this.LeadingColon;
			interwiki = this.Interwiki;
			ns = this.NamespaceId;
			pageName = this.PageName;
			fragment = this.Fragment;
		}

		/// <summary>Indicates whether the current title is equal to another title based on Interwiki, Namespace, PageName, and Fragment.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool FullEquals(IFullTitle other) =>
			other != null &&
			this.Interwiki == other.Interwiki &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName) &&
			this.Fragment == other.Fragment;
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File), regardless of the value of LeadingColon.</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public override string ToString(bool forceLink)
		{
			var baseText = base.ToString(forceLink);
			var interwiki = this.Interwiki == null ? string.Empty : this.Interwiki.Prefix + ':';
			var fragment = this.Fragment == null ? string.Empty : '#' + this.Fragment;

			return this.LeadingColon
				? ':' + interwiki + baseText.Substring(2) + fragment
				: interwiki + baseText + fragment;
		}
		#endregion
	}
}