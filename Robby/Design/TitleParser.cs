namespace RobinHood70.Robby.Design
{
	using System.Diagnostics;
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
			: this(site, MediaWikiNamespaces.Main, fullPageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParser"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		public TitleParser(Site site, int defaultNamespace, string pageName)
		{
			// This routine very roughly follows the logic of MediaWikiTitleCodec.splitTitleString() but skips much of the error checking and rarely encountered sanitization, which is left to the server.
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));

			static (string? Key, string PageName, bool Forced) SplitPageName(string pageName)
			{
				var forced = false;
				if (pageName.Length > 0 && pageName[0] == ':')
				{
					forced = true;
					pageName = pageName.Substring(1).TrimStart();
				}

				var split = pageName.Split(TextArrays.Colon, 2);
				return split.Length == 2 ? (split[0].TrimEnd(), split[1].TrimStart(), forced) : (null, pageName, forced);
			}

			// Pipes are not allowed in page names, so if we find one, only parse the first part; the remainder is likely cruft from a category or file link.
			pageName = pageName.Split(TextArrays.Pipe, 2)[0];
			pageName = WikiTextUtilities.DecodeAndNormalize(pageName).Trim();
			this.Namespace = site[defaultNamespace];

			var (key, remaining, forced) = SplitPageName(pageName);
			if (forced)
			{
				this.Namespace = site[MediaWikiNamespaces.Main];
				this.ForcedNamespaceLink = true;
			}

			pageName = remaining;
			var isMainPage = false;
			if (key != null)
			{
				if (site.Namespaces.ValueOrDefault(key) is Namespace ns)
				{
					this.Namespace = ns;
				}
				else if (site.InterwikiMap != null && site.InterwikiMap.TryGetValue(key, out var iw))
				{
					this.Interwiki = iw;
					this.ForcedNamespaceLink = false;
					this.ForcedInterwikiLink = forced;
					if (iw.LocalWiki)
					{
						if (pageName.Length == 0 && site.MainPage is FullTitle mp)
						{
							this.Interwiki = mp.Interwiki ?? iw;
							this.Namespace = mp.Namespace;
							pageName = mp.PageName;
							this.Fragment = mp.Fragment;
							isMainPage = true;
						}
						else
						{
							(key, remaining, forced) = SplitPageName(pageName);
							Debug.WriteLine($"{pageName} => {key}, {remaining}, {forced}");
							if (forced)
							{
								this.Namespace = site[MediaWikiNamespaces.Main];
								this.ForcedNamespaceLink = true;
								pageName = pageName.Substring(1);
							}
							else if (site.Namespaces.ValueOrDefault(key) is Namespace ns2)
							{
								this.Namespace = ns2;
								pageName = remaining;
								Debug.WriteLine($"{this.Namespace}, {pageName}");
							}
						}
					}
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

			this.Coerced = this.IsLocal && this.Namespace != defaultNamespace;
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
	}
}