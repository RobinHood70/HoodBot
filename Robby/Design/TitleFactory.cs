namespace RobinHood70.Robby.Design
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>This class serves as a light-weight parser to split a wiki title into its constituent parts.</summary>
	public sealed class TitleFactory : ILinkTitle, IFullTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		private TitleFactory(Site site, int defaultNamespace, string pageName)
		{
			// This routine very roughly follows the logic of MediaWikiTitleCodec.splitTitleString() but skips much of the error checking and rarely encountered sanitization, which is left to the server.
			static (string Key, string PageName, bool Forced) SplitPageName(string pageName)
			{
				var forced = pageName.Length > 0 && pageName[0] == ':';
				if (forced)
				{
					pageName = pageName[1..].TrimStart();
				}

				var split = pageName.Split(TextArrays.Colon, 2);
				return split.Length == 2
					? (split[0].TrimEnd(), split[1].TrimStart(), forced)
					: (string.Empty, pageName, forced);
			}

			WikiTextUtilities.TrimCruft(pageName);
			var (key, remaining, forced) = SplitPageName(pageName);
			var isMainPage = false;
			if (!forced && key.Length == 0)
			{
				this.ForcedNamespaceLink = forced;
				pageName = remaining;
			}
			else if (site.Namespaces.ValueOrDefault(key) is Namespace ns)
			{
				this.Namespace = ns;
				this.ForcedNamespaceLink = forced;
				pageName = remaining;
			}
			else if (site.InterwikiMap != null && site.InterwikiMap.TryGetValue(key.ToLower(site.Culture), out var iw))
			{
				this.Interwiki = iw;
				this.ForcedInterwikiLink = forced;
				if (iw.LocalWiki)
				{
					if (remaining.Length == 0 && site.MainPage is FullTitle mp)
					{
						this.Interwiki = mp.Interwiki ?? iw;
						this.Namespace = mp.Namespace;
						pageName = mp.PageName;
						this.Fragment = mp.Fragment;
						isMainPage = true;
					}
					else
					{
						var before = remaining;
						(key, remaining, forced) = SplitPageName(remaining);
						if (forced)
						{
							// Second colon in a row. MediaWiki invalidates this, but for now, this is designed to always succeed, so return page name in main space with leading colon.
							this.Namespace = site[MediaWikiNamespaces.Main];
							this.ForcedNamespaceLink = false;
							pageName = before;
						}
						else if (site.Namespaces.ValueOrDefault(key) is Namespace ns2)
						{
							this.Namespace = ns2;
							pageName = remaining;
						}
					}
				}
				else
				{
					pageName = remaining;
				}
			}

			if (!isMainPage)
			{
				var split = pageName.Split(TextArrays.Octothorpe, 2);
				if (split.Length == 2)
				{
					this.Fragment = split[1];
					pageName = split[0].TrimEnd();
				}
			}

			if (this.Namespace == null)
			{
				if (this.IsLocal)
				{
					this.Namespace = site[defaultNamespace];
					this.Coerced = true;
				}
				else
				{
					this.Namespace = site[MediaWikiNamespaces.Main];
				}
			}

			this.PageName = isMainPage || !InterwikiEntry.IsLocal(this.Interwiki)
				? pageName
				: this.Namespace.CapitalizePageName(pageName);
		}

		private TitleFactory(Namespace ns, string pageName)
		{
			// Shortcut constructor for times when a pre-validated, local page is be provided.
			this.Namespace = ns;

			var split = pageName.Split(TextArrays.Octothorpe, 2);
			if (split.Length == 2)
			{
				this.Fragment = split[1];
				pageName = split[0].TrimEnd();
			}

			this.PageName = pageName;
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public bool Coerced { get; }

		/// <inheritdoc/>
		public bool ForcedInterwikiLink { get; }

		/// <inheritdoc/>
		public bool ForcedNamespaceLink { get; }

		/// <inheritdoc/>
		public string? Fragment { get; }

		/// <summary>Gets the full page name of a title.</summary>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		public string FullPageName => this.Namespace.DecoratedName + this.PageName;

		/// <summary>Gets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public InterwikiEntry? Interwiki { get; }

		/// <summary>Gets a value indicating whether this object represents the local wiki (either via being a direct link or local interwiki link.</summary>
		public bool IsLocal => this.Interwiki?.LocalWiki != false;

		/// <inheritdoc/>
		public Namespace Namespace { get; }

		/// <summary>Gets a value indicating whether the namespace is displayed as part of the name.</summary>
		/// <value><see langword="true"/> if no namespace is present; otherwise, <see langword="false"/>.</value>
		/// <remarks>This value will be false for Main space links without a leading colon, Template calls (unless they actually specify <c>Template:</c>), and any gallery links that don't specify <c>File:</c>.</remarks>
		public bool NamespaceVisible { get; }

		/// <inheritdoc/>
		public string PageName { get; }
		#endregion

		#region Public Implicit Operators

		/// <summary>Implicit conversion to <see cref="FullTitle"/>.</summary>
		/// <param name="factory">The value to convert.</param>
		public static implicit operator FullTitle(TitleFactory factory) => new((IFullTitle)factory);

		/// <summary>Implicit conversion to <see cref="FullTitle"/>.</summary>
		/// <param name="factory">The value to convert.</param>
		public static implicit operator SiteLink(TitleFactory factory) => new((ILinkTitle)factory);

		/// <summary>Implicit conversion to <see cref="Title"/>.</summary>
		/// <param name="factory">The value to convert.</param>
		public static implicit operator Title(TitleFactory factory) => new(factory);
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site the page is in.</param>
		/// <param name="nsId">The namespace the page should resolve to.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		public static TitleFactory CoValidate(Site site, int? nsId, string fullPageName)
		{
			if (nsId is null)
			{
				return FromUnvalidated(site, fullPageName);
			}

			TitleFactory retval = new(site.NotNull(), MediaWikiNamespaces.Main, fullPageName.NotNull());
			return retval.Namespace.Id == nsId
				? retval
				: throw new InvalidOperationException("Namespace validation failed.");
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="node">The <see cref="IBacklinkNode"/> to parse.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		public static TitleFactory FromBacklinkNode(Site site, IBacklinkNode node) => FromUnvalidated(site.NotNull()[MediaWikiNamespaces.Main], node.NotNull().GetTitleText());

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromValidated(Namespace ns, string pageName) => new(ns.NotNull(), pageName.NotNull());

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromUnvalidated(Site site, string pageName) => new(site.NotNull(), MediaWikiNamespaces.Main, pageName.NotNull());

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromUnvalidated(Namespace ns, string pageName) => new(ns.NotNull().Site, ns.Id, pageName.NotNull());
		#endregion

		#region Public Methods

		/// <summary>Compares two objects for <see cref="Namespace"/> and <see cref="PageName"/> equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public bool SimpleEquals(Title? other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName, false);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public override string ToString()
		{
			var iwColon = this.ForcedInterwikiLink ? ":" : string.Empty;
			var interwiki = this.Interwiki == null ? string.Empty : this.Interwiki.Prefix + ':';
			var nsColon = this.ForcedNamespaceLink ? ":" : string.Empty;
			var fragment = this.Fragment == null ? string.Empty : '#' + this.Fragment;

			return iwColon + interwiki + nsColon + this.FullPageName + fragment;
		}

		/// <summary>Converts the current title to a <see cref="FullTitle"/>.</summary>
		/// <returns>A new FullTitle with the relevant properties set.</returns>
		public FullTitle ToFullTitle() => new((IFullTitle)this);

		/// <summary>Converts the current title to a <see cref="SiteLink"/>.</summary>
		/// <returns>A new SiteLink with the relevant properties set.</returns>
		public SiteLink ToSiteLink() => new((ILinkTitle)this);

		/// <summary>Converts the current title to a <see cref="Title"/>.</summary>
		/// <returns>A new Title with the relevant properties set.</returns>
		public Title ToTitle() => new(this);
		#endregion
	}
}