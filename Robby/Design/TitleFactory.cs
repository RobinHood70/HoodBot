namespace RobinHood70.Robby.Design
{
	using System.Diagnostics.CodeAnalysis;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	/// <summary>This class serves as a light-weight parser to split a wiki title into its constituent parts.</summary>
	public sealed class TitleFactory : ILinkTitle, IFullTitle
	{
		#region Constants
		// The following is taken from DefaultSettings::$wgLegalTitleChars and always assumes the default setting. I believe this is emitted as part of API:Siteinfo, but I wouldn't trust any kind of automated conversion, so better to just leave it as default, which is what 99.99% of wikis will probably use.
		private const string TitleChars = @"[ %!\""$&'()*,\-.\/0-9:;=?@A-Z\\^_`a-z~+\P{IsBasicLatin}-[()（）]]";
		#endregion

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

		private TitleFactory(Namespace ns, string pageName)
		{
			// Shortcut constructor for times when a pre-validated, local page is be provided.
			this.Namespace = ns;

			var split = pageName.Split(TextArrays.Octothorp, 2);
			if (split.Length == 2)
			{
				this.Fragment = split[1];
				pageName = split[0].TrimEnd();
			}

			this.PageName = pageName;
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets a regular expression matching all comma-like characters in a stirng.</summary>
		public static Regex LabelCommaRemover { get; } = new(@"\ *([,，]" + TitleChars + @"*?)\Z", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

		/// <summary>Gets a regular expression matching all parenthetical text in a stirng.</summary>
		public static Regex LabelParenthesesRemover { get; } = new(@"\ *(\(" + TitleChars + @"*?\)|（" + TitleChars + @"*?）)\Z", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
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

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory Create(Site site, int defaultNamespace, string pageName) => new(site.NotNull(nameof(site)), defaultNamespace, WikiTextUtilities.TrimToTitle(pageName.NotNull(nameof(pageName))));

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory CreateFromValidated(Namespace ns, string pageName) => new(ns.NotNull(nameof(ns)), pageName.NotNull(nameof(pageName)));

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="defaultNamespace">The default namespace.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromName(Site site, int defaultNamespace, string pageName) => new(site.NotNull(nameof(site)), defaultNamespace, WikiTextUtilities.TrimToTitle(pageName.NotNull(nameof(pageName))));

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="pageName">Name of the page.</param>
		public static TitleFactory FromNormalizedName(Site site, string pageName) => new(site.NotNull(nameof(site)), MediaWikiNamespaces.Main, pageName.NotNull(nameof(pageName)));

		/// <summary>Trims the disambiguator off of a title (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="title">The title to get the label name for.</param>
		/// <returns>The text with the final paranthetical text removed.</returns>
		public static string LabelName(string title) => LabelParenthesesRemover.Replace(title.NotNull(nameof(title)), string.Empty, 1, 1);

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="title">The title to modify.</param>
		/// <remarks>This doesn't precisely match the MediaWiki pipe trick logic. The two differ in their handling of edge cases. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text".</returns>
		[return: NotNullIfNotNull("title")]
		public static string PipeTrick(string title)
		{
			title.ThrowNull(nameof(title));
			var pageName = LabelCommaRemover.Replace(title, string.Empty, 1, 1);
			return LabelParenthesesRemover.Replace(pageName, string.Empty, 1, 1);
		}
		#endregion

		#region Public Methods

		/// <summary>Creates a new LinkTarget from the parsed text.</summary>
		/// <returns>A new <see cref="FullTitle"/>.</returns>
		public FullTitle ToFullTitle() => new(this);

		/// <summary>Creates a new SiteLink from the parsed text.</summary>
		/// <returns>A new <see cref="SiteLink"/>.</returns>
		public SiteLink ToSiteLink() => new(this);
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