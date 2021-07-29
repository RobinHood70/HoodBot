namespace RobinHood70.Robby.Design
{
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	/// <summary>This class serves as a light-weight parser to split a wiki title into its constituent parts.</summary>
	public class TitleFactory : ILinkTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		private TitleFactory(Site site, int defaultNamespace, string pageName)
		{
			// This routine very roughly follows the logic of MediaWikiTitleCodec.splitTitleString() but skips much of the error checking and rarely encountered sanitization, which is left to the server.
			site.ThrowNull(nameof(site));
			pageName.ThrowNull(nameof(pageName));
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
				var split = pageName.Split(TextArrays.Octothorp, 2);
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

		/// <inheritdoc/>
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

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromName(Site site, string pageName) => new(site.NotNull(nameof(site)), MediaWikiNamespaces.Main, Normalize(pageName.NotNull(nameof(pageName))));

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromName(Site site, int defaultNamespace, string pageName) => new(site.NotNull(nameof(site)), defaultNamespace, Normalize(pageName.NotNull(nameof(pageName))));

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromNormalizedName(Site site, string pageName) => new(site.NotNull(nameof(site)), MediaWikiNamespaces.Main, pageName.NotNull(nameof(pageName)));

		/// <summary>Normalizes a page name text for parsing. Page names coming directly from the API are already normalized.</summary>
		/// <param name="text">The page name to normalize.</param>
		/// <remarks>The following changes are applied:
		/// <list type="bullet">
		/// <item><description>All text after the first pipe (if any) is removed.</description></item>
		/// <item><description>HTML- and URL-encoded characters are decoded.</description></item>
		/// <item><description>Variants of the space character—such as hard spaces, em spaces, and underscores—are all converted to normal spaces.</description></item>
		/// </list></remarks>
		/// <returns>The normalized text, ready to be parsed.</returns>
		public static string Normalize(string text)
		{
			var retval = text.NotNull(nameof(text)).Split(TextArrays.Pipe, 2)[0];
			return WikiTextUtilities.DecodeAndNormalize(retval).Trim();
		}
		#endregion

		#region Public Methods

		/// <summary>Creates a new LinkTarget from the parsed text.</summary>
		/// <returns>A new <see cref="FullTitle"/>.</returns>
		public FullTitle ToLinkTarget() => new(this);

		/// <summary>Creates a new SiteLink from the parsed text.</summary>
		/// <returns>A new <see cref="FullTitle"/>.</returns>
		public SiteLink ToSiteLink() => new(this);

		/// <summary>Creates a new title from the parsed text.</summary>
		/// <returns>A new <see cref="Title"/>.</returns>
		public Title ToTitle() => new(this);
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
		#endregion
	}
}