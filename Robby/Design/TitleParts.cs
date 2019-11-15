﻿namespace RobinHood70.Robby.Design
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
		/// <param name="title">The ISimpleTitle to copy values from.</param>
		public TitleParts(ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			this.Site = title.Site;
			this.OriginalNamespaceText = title.Namespace.Name;
			this.OriginalPageNameText = title.PageName;
			this.LeadingColon = title.Namespace.IsForcedLinkSpace;
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParts"/> class.</summary>
		/// <param name="title">The ISimpleTitle to copy values from.</param>
		public TitleParts(IFullTitle title)
			: this(title as ISimpleTitle)
		{
			ThrowNull(title, nameof(title));
			this.Interwiki = title.Interwiki;
			this.Fragment = title.Fragment;
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParts"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public TitleParts(Site site, string fullPageName)
			: this(site, MediaWikiNamespaces.Main, fullPageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TitleParts"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		/// <exception cref="ArgumentException">Thrown when the page name is invalid.</exception>
		public TitleParts(Site site, int defaultNamespace, string fullPageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			this.Site = site;

			// Pipes are not allowed in page names, so if we find one, only parse the first part; the remainder is likely cruft from a category or file link.
			var nameRemaining = fullPageName.Split(TextArrays.Pipe, 2)[0].TrimEnd();
			nameRemaining = WikiTextUtilities.DecodeAndNormalize(nameRemaining);
			if (nameRemaining.Length > 0 && nameRemaining[0] == ':')
			{
				this.LeadingColon = true;
				nameRemaining = nameRemaining.Substring(1).TrimStart();
			}

			if (nameRemaining.Length == 0)
			{
				throw new ArgumentException(CurrentCulture(Resources.TitleInvalid));
			}

			Namespace? nsFinal = null;
			string? originalNs = null;
			var split = nameRemaining.Split(TextArrays.Colon, 3);
			if (split.Length >= 2)
			{
				var key = split[0].TrimEnd();
				if (site.Namespaces.ValueOrDefault(key) is Namespace ns)
				{
					nsFinal = ns;
					originalNs = key;
					nameRemaining = split[1].TrimStart() + (split.Length == 3 ? ':' + split[2] : string.Empty);
				}
				else if (site.InterwikiMap != null && site.InterwikiMap.TryGetValue(key, out var iw))
				{
					this.Interwiki = iw;
					this.OriginalInterwikiText = key;
					key = split[1].Trim();
					if (iw.LocalWiki && site.Namespaces.ValueOrDefault(key) is Namespace nsiw)
					{
						nsFinal = nsiw;
						originalNs = key;
						nameRemaining = split[2].TrimStart();
						if (nameRemaining.Length == 0)
						{
							this.PageName = site.MainPageName ?? "Main Page";
						}
					}
					else
					{
						nameRemaining = split[1].TrimStart() + (split.Length == 3 ? ':' + split[2] : string.Empty);
					}
				}
			}

			// If we have a leading colon, but no namespace, then this was meant to override any default namespace and force it to Main space.
			this.Namespace = nsFinal ?? site.Namespaces[this.LeadingColon ? MediaWikiNamespaces.Main : defaultNamespace];
			this.OriginalNamespaceText = originalNs ?? string.Empty;

			if (nameRemaining.Length == 0)
			{
				this.OriginalPageNameText = string.Empty;
			}
			else
			{
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
			}

			this.PageName = this.Namespace.CapitalizePageName(this.PageName);
		}

		// Designed for data coming directly from MediaWiki. Assumes all values are appropriate and pre-trimmed - only does namespace parsing. interWiki and fragment may be null; fullPageName may not.
		internal TitleParts(Site site, string? interWiki, string fullPageName, string? fragment)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			this.Site = site;
			if (interWiki != null)
			{
				this.Interwiki = site.InterwikiMap[interWiki];
				this.OriginalInterwikiText = interWiki;
			}

			var split = fullPageName.Split(TextArrays.Colon, 2);
			if (site.Namespaces.ValueOrDefault(split[0]) is Namespace ns)
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

			this.OriginalPageNameText = this.PageName;
			if (fragment != null)
			{
				this.Fragment = fragment;
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title's fragment (the section or ID to scroll to).</summary>
		/// <value>The fragment.</value>
		public string? Fragment { get; set; }

		/// <summary>Gets the full name of the page.</summary>
		/// <value>The full name of the page.</value>
		/// <remarks>This value is always constructed from the Namespace.DecoratedName property and the PageName property and can only be changed by changing those values.</remarks>
		public string FullPageName => this.Namespace?.DecoratedName + this.PageName;

		/// <summary>Gets or sets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public InterwikiEntry? Interwiki { get; set; }

		/// <summary>Gets a value indicating whether this instance is identical to the local wiki.</summary>
		/// <value><see langword="true"/> if this instance is local wiki; otherwise, <see langword="false"/>.</value>
		public bool IsLocal => this.Interwiki == null || this.Interwiki.LocalWiki;

		/// <summary>Gets or sets a value indicating whether the title had a leading colon.</summary>
		/// <value><see langword="true"/> if there was a leading colon; otherwise, <see langword="false"/>.</value>
		public bool LeadingColon { get; set; }

		/// <summary>Gets or sets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		/// <remarks>In the event that the title is a non-local interwiki title, this will be populated with the default namespace specified in the constructor (if applicable).</remarks>
		public Namespace Namespace { get; set; }

		/// <summary>Gets the interwiki text passed to the constructor, after parsing.</summary>
		/// <value>The interwiki text.</value>
		/// <remarks>This value can be used to bypass any automatic formatting or name changes caused by using the default Interwiki values, such as case changes. Parsing removes hidden characters and changes unusual spaces to normal spaces. The value will also have been trimmed.</remarks>
		public string? OriginalInterwikiText { get; }

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
		public Site Site { get; }
		#endregion

		#region Public Methods

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="leadingColon">The value returned by <see cref="LeadingColon"/>.</param>
		/// <param name="interwiki">The value returned by <see cref="Interwiki"/>.</param>
		/// <param name="ns">The value returned by <see cref="Namespace"/>.</param>
		/// <param name="pageName">The value returned by <see cref="PageName"/>.</param>
		/// <param name="fragment">The value returned by <see cref="Fragment"/>.</param>
		public void Deconstruct(out bool leadingColon, out InterwikiEntry? interwiki, out Namespace ns, out string pageName, out string? fragment)
		{
			leadingColon = this.LeadingColon;
			interwiki = this.Interwiki;
			ns = this.Namespace;
			pageName = this.PageName;
			fragment = this.Fragment;
		}

		/// <summary>Indicates whether the current title is equal to another title based on Interwiki, Namespace, PageName, and Fragment.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool FullEquals(IFullTitle other) =>
			other != null &&
			this.Interwiki == other.Interwiki &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName) &&
			this.Fragment == other.Fragment;

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equivalent to the local wiki and the title is equal to the <paramref name="other" /> parameter, ignoring the Fragment property; otherwise, <see langword="false"/>.</returns>
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