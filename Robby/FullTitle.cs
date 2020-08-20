namespace RobinHood70.Robby.Design
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Review constructors for various title objects.

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class FullTitle : Title, IFullTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public FullTitle(Site site, string fullPageName)
			: this(new TitleParser(site, MediaWikiNamespaces.Main, fullPageName, false))
		{
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public FullTitle(Site site, int defaultNamespace, string pageName)
			: this(new TitleParser(site, defaultNamespace, pageName, true))
		{
		}

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
				this.Namespace = ns;
				this.PageName = split[1];
			}
			else
			{
				this.Namespace = site.Mainspace;
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
			this.Fragment = parser.Fragment;
			this.Interwiki = parser.Interwiki;
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

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <returns>A new <see cref="Title"/> with the namespace found in <paramref name="pageName"/>, if there is one, otherwise using <paramref name="defaultNamespace"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public static new FullTitle Coerce(Site site, int defaultNamespace, string pageName) => new FullTitle(new TitleParser(site, defaultNamespace, pageName, false));
		#endregion

		#region Public Methods

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="interwiki">The value returned by <see cref="Interwiki"/>.</param>
		/// <param name="ns">The value returned by <see cref="ISimpleTitle.Namespace"/>.</param>
		/// <param name="pageName">The value returned by <see cref="ISimpleTitle.PageName"/>.</param>
		/// <param name="fragment">The value returned by <see cref="Fragment"/>.</param>
		public void Deconstruct(out InterwikiEntry? interwiki, out Namespace ns, out string pageName, out string? fragment)
		{
			interwiki = this.Interwiki;
			ns = this.Namespace;
			pageName = this.PageName;
			fragment = this.Fragment;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File).</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public override string ToString(bool forceLink)
		{
			var colon = (forceLink && this.Namespace.IsForcedLinkSpace) ? ":" : string.Empty;
			var interwiki = this.Interwiki == null ? string.Empty : this.Interwiki.Prefix + ':';
			var fragment = this.Fragment == null ? string.Empty : '#' + this.Fragment;

			return colon + interwiki + this.FullPageName() + fragment;
		}
		#endregion
	}
}