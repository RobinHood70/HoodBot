namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text.RegularExpressions;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class TitleParts : IWikiTitle
	{
		#region Static Fields
		private static Regex bidiText = new Regex(@"[\u200E\u200F\u202A\u202B\u202C\u202D\u202E]", RegexOptions.Compiled); // Taken from MediaWikIWikiTitleCodec->splitTitleString, then converted to Unicode
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
			this.Key = fullPageName;
			var nameRemaining = Normalize(Decode(fullPageName));
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
					nameRemaining = split[1].TrimStart() + (split.Length == 3 ? split[2] : string.Empty);
				}
				else if (site.InterwikiMap.TryGetItem(key, out var iw))
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
							}
						}
					}
					else
					{
						nameRemaining = split[1].TrimStart() + split[2];
					}
				}
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
				this.PageName = this.Namespace.CaseSensitive ? split[0] : split[0].UpperFirst();
				this.Fragment = split[1];
			}
			else
			{
				this.PageName = this.Namespace.CaseSensitive ? nameRemaining : nameRemaining.UpperFirst();
			}
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

		/// <summary>Gets or sets the key to use in dictionary lookups. By default, this is the full page name provided when the object was constructed.</summary>
		/// <value>The key.</value>
		public string Key { get; set; }

		/// <summary>Gets or sets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; set; }

		/// <summary>Gets or sets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; set; }
		#endregion

		#region Public Static Methods

		/// <summary>HTML-decodes the specified text.</summary>
		/// <param name="text">The text.</param>
		/// <returns>The HTML-decoded text.</returns>
		/// <remarks>This is provided as a separate function for future-proofing, since Microsoft has historically provided several (often flawed) methods of URL and HTML encoding/decoding. This way, calls to it remain consistent even if the underlying method changes or bug fixes need to be added.</remarks>
		public static string Decode(string text) => WebUtility.HtmlDecode(text);

		/// <summary>Removes bidirectional text markers and replaces space-like characters with spaces.</summary>
		/// <param name="text">The text to normalize.</param>
		/// <returns>The original text with bidirectional text markers removed and space-like characters converted to spaces.</returns>
		public static string Normalize(string text) => spaceText.Replace(bidiText.Replace(text, string.Empty), " ").Trim();
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