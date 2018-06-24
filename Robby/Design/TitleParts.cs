namespace RobinHood70.Robby.Design
{
	using System;
	using System.Diagnostics;
	using System.Net;
	using System.Text.RegularExpressions;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class TitleParts : IFullTitle, ISimpleTitle
	{
		#region Static Fields
		private static Regex bidiText = new Regex(@"[\u200E\u200F\u202A\u202B\u202C\u202D\u202E]", RegexOptions.Compiled); // Taken from MediaWikiTitleCodec->splitTitleString, then converted to Unicode
		private static Regex spaceText = new Regex(@"[ _\xA0\u1680\u180E\u2000-\u200A\u2028\u2029\u202F\u205F\u3000]", RegexOptions.Compiled); // as above, but already Unicode in MW code
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleParts"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public TitleParts(Site site, string fullPageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			var nameRemaining = DecodeAndNormalize(fullPageName);
			if (nameRemaining.Length > 0 && nameRemaining[0] == ':')
			{
				// this.Namespace = site.Namespaces[MediaWikiNamespaces.Main];
				nameRemaining = nameRemaining.Substring(1).TrimStart();
			}

			if (nameRemaining.Length == 0)
			{
				throw new ArgumentException(CurrentCulture(TitleInvalid));
			}

			var split = nameRemaining.Split(new[] { ':' }, 3);
			if (split.Length >= 2)
			{
				var key = split[0].TrimEnd();
				if (site.Namespaces.TryGetValue(key, out var ns))
				{
					this.Namespace = ns;
					nameRemaining = split[1].TrimStart() + (split.Length == 3 ? ':' + split[2] : string.Empty);
				}
				else if (site.InterwikiMap.TryGetValue(key, out var iw))
				{
					this.Interwiki = iw;
					if (iw.LocalWiki && site.Namespaces.TryGetValue(split[1].Trim(), out ns))
					{
						this.Namespace = ns;
						nameRemaining = split[2].TrimStart();
						if (nameRemaining.Length == 0)
						{
							var mainPageName = site.MainPage ?? "Main Page";

							// Make sure we're not recursing with a horribly unlikely Main Page.
							if (mainPageName != fullPageName)
							{
								var mainPage = new TitleParts(site, site.MainPage ?? "Main Page");
								this.Interwiki = mainPage.Interwiki;
								this.Namespace = mainPage.Namespace;
								this.PageName = mainPage.PageName;
								this.Fragment = mainPage.Fragment;

								return;
							}
						}
					}
					else
					{
						this.Namespace = site.Namespaces[MediaWikiNamespaces.Main];
						nameRemaining = split[1].TrimStart() + ':' + split[2];
					}
				}
				else
				{
					this.Namespace = site.Namespaces[MediaWikiNamespaces.Main];
				}
			}
			else
			{
				this.Namespace = site.Namespaces[MediaWikiNamespaces.Main];
			}

			if (this.Namespace == MediaWikiNamespaces.Talk)
			{
				split = nameRemaining.TrimStart().Split(new[] { ':' }, 2);
				var nsTest = split[0].TrimEnd();
				if (split.Length == 2 && (site.Namespaces.Contains(nsTest) || site.InterwikiMap.Contains(nsTest)))
				{
					throw new ArgumentException(CurrentCulture(TitleDoubleNamespace));
				}
			}

			split = nameRemaining.Split(new[] { '#' }, 2);
			if (split.Length == 2)
			{
				this.PageName = split[0];
				this.Fragment = split[1];
			}
			else
			{
				this.PageName = nameRemaining;
			}

			// Do not change page name if Namespace is null (meaning it's a non-local interwiki or there was a parsing failure).
			if (!this.Namespace?.CaseSensitive ?? false)
			{
				this.PageName = this.PageName.UpperFirst(site.Culture);
			}

			Debug.Assert(this.Interwiki != null || this.Namespace != null, "Neither Interwiki nor Namespace were assigned.");
		}

		// Designed for data coming directly from MediaWiki. Assumes all values are appropriate and pre-trimmed - only does namespace parsing. interWiki and fragment may be null; fullPageName may not.
		internal TitleParts(Site site, string interWiki, string fullPageName, string fragment)
		{
			ThrowNull(fullPageName, nameof(fullPageName));
			if (interWiki != null)
			{
				this.Interwiki = site.InterwikiMap[interWiki];
			}

			var split = fullPageName.Split(new[] { ':' }, 2);
			if (site.Namespaces.TryGetValue(split[0], out var ns))
			{
				this.Namespace = ns;
				this.PageName = split[1];
			}
			else
			{
				this.Namespace = site.Namespaces[MediaWikiNamespaces.Main];
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
		public bool IsLocalWiki => this.Interwiki == null || this.Interwiki.LocalWiki;

		/// <summary>Gets or sets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; set; }

		/// <summary>Gets or sets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; set; }
		#endregion

		#region Public Static Methods

		/// <summary>HTML-decodes the specified text, removes bidirectional text markers, and replaces space-like characters with spaces.</summary>
		/// <param name="text">The text to decode and normalize.</param>
		/// <returns>The original text with bidirectional text markers removed and space-like characters converted to spaces.</returns>
		public static string DecodeAndNormalize(string text) => spaceText.Replace(bidiText.Replace(WebUtility.HtmlDecode(text), string.Empty), " ").Trim();
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
			this.IsLocalWiki &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName);
		#endregion

		#region Public Overrides

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString()
		{
			var retval = string.Empty;
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