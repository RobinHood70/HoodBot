namespace RobinHood70.Robby.Design
{
	using System;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class TitleParts : IFullTitle, ISimpleTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleParts"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public TitleParts(Site site, string fullPageName)
			: this(site, fullPageName, MediaWikiNamespaces.Main)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParts"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public TitleParts(Site site, string fullPageName, int defaultNamespace)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			var nameRemaining = HtmlUtilities.DecodeAndNormalize(fullPageName);
			if (nameRemaining.Length > 0 && nameRemaining[0] == ':')
			{
				this.LeadingColon = true;
				nameRemaining = nameRemaining.Substring(1).TrimStart();
			}

			if (nameRemaining.Length == 0)
			{
				throw new ArgumentException(CurrentCulture(Resources.TitleInvalid));
			}

			var split = nameRemaining.Split(TextArrays.Colon, 3);
			if (split.Length >= 2)
			{
				var key = split[0].TrimEnd();
				if (site.Namespaces.TryGetValue(key, out var ns))
				{
					this.Namespace = ns;
					this.OriginalNamespaceText = key;
					nameRemaining = split[1].TrimStart() + (split.Length == 3 ? ':' + split[2] : string.Empty);
				}
				else if (site.InterwikiMap.TryGetValue(key, out var iw))
				{
					this.Interwiki = iw;
					this.OriginalInterwikiText = key;
					key = split[1].Trim();
					if (iw.LocalWiki && site.Namespaces.TryGetValue(key, out ns))
					{
						this.Namespace = ns;
						this.OriginalNamespaceText = key;
						nameRemaining = split[2].TrimStart();
						if (nameRemaining.Length == 0)
						{
							this.OriginalPageNameText = string.Empty;
							this.PageName = site.MainPage?.PageName ?? "Main Page";
							return;
						}
					}
					else
					{
						nameRemaining = split[1].TrimStart() + (split.Length == 3 ? ':' + split[2] : string.Empty);
					}
				}
			}

			if (this.Namespace == null)
			{
				this.Namespace = site.Namespaces[defaultNamespace];
				this.OriginalNamespaceText = string.Empty;
			}

			split = nameRemaining.Split(TextArrays.Octothorp, 2);
			if (split.Length == 2)
			{
				this.PageName = split[0];
				this.OriginalPageNameText = split[0];
				this.Fragment = split[1];
			}
			else
			{
				this.PageName = nameRemaining;
				this.OriginalPageNameText = nameRemaining;
			}

			// Do not change page name if Namespace is null (meaning it's a non-local interwiki or there was a parsing failure).
			if (this.Namespace != null)
			{
				this.PageName = this.Namespace.CapitalizePageName(this.PageName);
			}
		}

		// Designed for data coming directly from MediaWiki. Assumes all values are appropriate and pre-trimmed - only does namespace parsing. interWiki and fragment may be null; fullPageName may not.
		internal TitleParts(Site site, string interWiki, string fullPageName, string fragment)
		{
			ThrowNull(fullPageName, nameof(fullPageName));
			if (interWiki != null)
			{
				this.Interwiki = site.InterwikiMap[interWiki];
				this.OriginalInterwikiText = interWiki;
			}

			var split = fullPageName.Split(TextArrays.Colon, 2);
			if (site.Namespaces.TryGetValue(split[0], out var ns))
			{
				this.Namespace = ns;
				this.OriginalNamespaceText = split[0];
				this.PageName = split[1];
			}
			else
			{
				this.Namespace = site.Namespaces[MediaWikiNamespaces.Main];
				this.OriginalNamespaceText = string.Empty;
				this.PageName = fullPageName;
			}

			if (fragment != null)
			{
				this.Fragment = fragment;
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title's fragment (the section or ID to scroll to).</summary>
		/// <value>The fragment.</value>
		public string Fragment { get; set; }

		/// <summary>Gets the full name of the page.</summary>
		/// <value>The full name of the page.</value>
		/// <remarks>This value is always constructed from the Namespace.DecoratedName property and the PageName property and can only be changed by changing those values.</remarks>
		public string FullPageName => this.Namespace?.DecoratedName + this.PageName;

		/// <summary>Gets or sets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public InterwikiEntry Interwiki { get; set; }

		/// <summary>Gets a value indicating whether this instance is identical to the local wiki.</summary>
		/// <value><c>true</c> if this instance is local wiki; otherwise, <c>false</c>.</value>
		public bool IsLocal => this.Interwiki == null || this.Interwiki.LocalWiki;

		/// <summary>Gets a value indicating whether the title had a leading colon.</summary>
		/// <value><see langword="true"/> if there was a leading colon; otherwise, <see langword="false"/>.</value>
		public bool LeadingColon { get; }

		/// <summary>Gets or sets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; set; }

		/// <summary>Gets the interwiki text passed to the constructor, after parsing.</summary>
		/// <value>The interwiki text.</value>
		/// <remarks>This value can be used to bypass any automatic formatting or name changes caused by using the default Interwiki values, such as case changes. Parsing removes hidden characters and changes unusual spaces to normal spaces. The value will also have been trimmed.</remarks>
		public string OriginalInterwikiText { get; }

		/// <summary>Gets the namespace text passed to the constructor, after parsing.</summary>
		/// <value>The namespace text.</value>
		/// <remarks>This value can be used to bypass any text changes caused by relying on the Namespace values, such as an alias having been used. Parsing removes hidden characters and changes unusual spaces to normal spaces. The value will also have been trimmed. This value will not change, even if the <see cref="Namespace"/> changes.</remarks>
		public string OriginalNamespaceText { get; }

		/// <summary>Gets the page name text passed to the constructor, after parsing.</summary>
		/// <value>The page name text.</value>
		/// <remarks>This value can be used to bypass any text changes caused by using the PageName value, such as first-letter casing. Parsing removes hidden characters and changes unusual spaces to normal spaces. The value will also have been trimmed.</remarks>
		public string OriginalPageNameText { get; }

		/// <summary>Gets or sets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; set; }

		/// <summary>Gets the site the title belongs to.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;
		#endregion

		#region Public Static Methods

		/// <summary>Returns a <see cref="TitleParts"/> for the given namespace and page name, allowing for the possibility that the page may already have the namespace prepended to it.</summary>
		/// <param name="ns">The namespace the page should belong to.</param>
		/// <param name="pageName">The name of the page, with or without the corresponding namespace prefix.</param>
		/// <returns>A TitleParts object with the given name in the given namespace.</returns>
		public static TitleParts ForcedNamespace(Namespace ns, string pageName)
		{
			ThrowNull(ns, nameof(ns));
			ThrowNull(pageName, nameof(pageName));
			var titleParts = new TitleParts(ns.Site, pageName);
			if (titleParts.Namespace != ns)
			{
				if (titleParts.Namespace == MediaWikiNamespaces.Main)
				{
					titleParts.Namespace = ns;
				}
				else
				{
					titleParts = new TitleParts(ns.Site, ns.DecoratedName + pageName);
				}
			}

			return titleParts;
		}

		/// <summary>Returns a <see cref="TitleParts"/> for the given namespace and page name, allowing for the possibility that the page may already have the namespace prepended to it.</summary>
		/// <param name="site">The Site the Title is from.</param>
		/// <param name="ns">The namespace ID the page should belong to.</param>
		/// <param name="pageName">The name of the page, with or without the corresponding namespace prefix.</param>
		/// <returns>A <see cref="TitleParts"/> object with the given name in the given namespace.</returns>
		public static TitleParts ForcedNamespace(Site site, int ns, string pageName) => ForcedNamespace(site?.Namespaces[ns], pageName);
		#endregion

		#region Public Methods

		/// <summary>Indicates whether the current title is equal to another title based on Interwiki, Namespace, PageName, and Fragment.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool FullEquals(IFullTitle other) =>
			other != null &&
			this.Interwiki == other.Interwiki &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName) &&
			this.Fragment == other.Fragment;

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equivalent to the local wiki and the title is equal to the <paramref name="other" /> parameter, ignoring the Fragment property; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool SimpleEquals(ISimpleTitle other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName);
		#endregion

		#region Public Overrides

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString()
		{
			var retval = this.LeadingColon ? ":" : string.Empty;
			if (this.Interwiki != null)
			{
				retval += this.Interwiki.Prefix + ':';
			}

			retval += this.FullPageName;
			if (this.Fragment != null)
			{
				retval += '#' + this.Fragment;
			}

			return retval;
		}
		#endregion
	}
}