namespace RobinHood70.Robby.Design
{
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>This class serves as a light-weight parser to split a wiki title into its constituent parts.</summary>
	public class TitleParser : ILinkTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleParser"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="fullPageName">Full page name, with namespace.</param>
		public TitleParser(Site site, string fullPageName)
			: this(site, MediaWikiNamespaces.Main, fullPageName, true)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParser"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		public TitleParser(Site site, int defaultNamespace, string pageName)
			: this(site, defaultNamespace, pageName, true)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParser"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		/// <param name="fullParsing">If <see langword="true"/>, <paramref name="pageName"/> will be checked for pipes, HTML/URL codes, and alternate spaces.</param>
		public TitleParser(Site site, int defaultNamespace, string pageName, bool fullParsing)
		{
			// This routine very roughly follows the logic of MediaWikiTitleCodec.splitTitleString() but skips much of the error checking and rarely encountered sanitization, which is left to the server.
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
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

			if (fullParsing)
			{
				// Pipes are not allowed in page names, so if we find one, only parse the first part; the remainder is likely cruft from a category or file link.
				pageName = pageName.Split(TextArrays.Pipe, 2)[0];
				pageName = WikiTextUtilities.DecodeAndNormalize(pageName).Trim();
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
		public bool IsLocal => this.Interwiki == null || this.Interwiki.LocalWiki;

		/// <inheritdoc/>
		public Namespace Namespace { get; }

		/// <summary>Gets a value indicating whether the namespace is displayed as part of the name.</summary>
		/// <value><see langword="true"/> if no namespace is present; otherwise, <see langword="false"/>.</value>
		/// <remarks>This value will be false for Main space links without a leading colon, Template calls (unless they actually specify <c>Template:</c>), and any gallery links that don't specify <c>File:</c>.</remarks>
		public bool NamespaceVisible { get; }

		/// <inheritdoc/>
		public string PageName { get; }
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