namespace RobinHood70.Robby.Design
{
	using System;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Review constructors for various title objects.

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class FullTitle : Title, IFullTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FullTitle" /> class using the site and full page name.</summary>
		/// <param name="ns">The namespace of the title.</param>
		/// <param name="pageName">The page name (without leading namespace).</param>
		public FullTitle(Namespace ns, string pageName)
			: base(ns, pageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle" /> class using the site and full page name.</summary>
		/// <param name="iw">The interwiki of the title.</param>
		/// <param name="ns">The namespace of the title.</param>
		/// <param name="pageName">The page name (without leading namespace).</param>
		/// <param name="fragment">The fragment of the title.</param>
		public FullTitle(InterwikiEntry? iw, Namespace ns, string pageName, string? fragment)
			: base(ns, pageName)
		{
			this.Interwiki = iw;
			this.Fragment = fragment;
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> with the desired information.</param>
		public FullTitle(ISimpleTitle title)
			: base(title)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="title">The <see cref="IFullTitle"/> with the desired information.</param>
		public FullTitle(IFullTitle title)
			: base(title)
		{
			this.Fragment = title.Fragment;
			this.Interwiki = title.Interwiki;
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
		public bool IsLocal => this.Interwiki?.LocalWiki != false;
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <returns>A new LinkTitle with the namespace found in <paramref name="pageName"/>, if there is one, otherwise using <paramref name="defaultNamespace"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public static new FullTitle Coerce(Site site, int defaultNamespace, string pageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			var parser = new TitleParser(site, defaultNamespace, pageName);
			return new FullTitle(parser);
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="node">The <see cref="IBacklinkNode"/> to parse.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		public static new FullTitle FromBacklinkNode(Site site, IBacklinkNode node)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(node, nameof(node));
			return FromName(site, node.GetTitleText());
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public static new FullTitle FromName(Site site, string fullPageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			var parser = new TitleParser(site, fullPageName);
			return new FullTitle(parser);
		}
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

			return colon + interwiki + this.FullPageName + fragment;
		}
		#endregion

		#region Internal Methods

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class from the site and wiki text (which must already be in standard format).</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		internal static new FullTitle FromWikiTitle(Site site, string fullPageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			var parser = new TitleParser(site, MediaWikiNamespaces.Main, fullPageName, false);
			return new FullTitle(parser);
		}
		#endregion
	}
}