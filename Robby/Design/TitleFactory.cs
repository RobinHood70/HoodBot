﻿namespace RobinHood70.Robby.Design;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

#region Public Enumerations

/// <summary>Enumeration of the different textual parts of a link.</summary>
[Flags]
public enum TitlePartType
{
	/// <summary>Do not include any parts of the original text (for future use).</summary>
	None = 0,

	/// <summary>The original interwiki text on the title.</summary>
	Interwiki = 1 << 0,

	/// <summary>The original namespace text of the title.</summary>
	Namespace = 1 << 1,

	/// <summary>The original page name text of the title.</summary>
	PageName = 1 << 2,

	/// <summary>The original fragment text of the title.</summary>
	Fragment = 1 << 3,

	/// <summary>Include all parts of the original text (for future use).</summary>
	All = Interwiki | Namespace | PageName | Fragment
}
#endregion

/// <summary>This class serves as a light-weight parser to split a wiki title into its constituent parts.</summary>
public sealed class TitleFactory : ILinkTitle, IFullTitle, ITitle
{
	#region Private Fields
	private readonly Dictionary<TitlePartType, string> originalParts = [];
	private Title? title;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class. Assumes title could include interwiki and/or fragment.</summary>
	/// <param name="site">The site.</param>
	/// <param name="defaultNamespace">The default namespace.</param>
	/// <param name="pageName">Name of the page.</param>
	private TitleFactory(Site site, int defaultNamespace, string pageName)
	{
		var (key, remaining, forced) = SplitPageName(pageName);
		if (!forced && key.Length == 0)
		{
			pageName = remaining;
		}
		else if (site.Namespaces.ValueOrDefault(key) is Namespace ns)
		{
			this.Namespace = ns;
			this.originalParts.Add(TitlePartType.Namespace, key);
			this.ForcedNamespaceLink = forced;
			pageName = remaining;
		}
		else if (site.InterwikiMap?.TryGetValue(key.ToLower(site.Culture), out var iw) == true)
		{
			this.originalParts.Add(TitlePartType.Interwiki, key);
			this.Interwiki = iw;
			this.ForcedInterwikiLink = forced;
			if (iw.LocalWiki)
			{
				if (remaining.Length == 0 && site.MainPage is IFullTitle mp)
				{
					// A link like [[:en:]] returns the title in MediaWiki:Mainpage and respects both interwiki and fragment, so we copy the full thing over.
					this.originalParts.Add(TitlePartType.PageName, string.Empty);
					this.Interwiki = mp.Interwiki;
					this.Namespace = mp.Title.Namespace;
					this.PageName = mp.Title.PageName;
					this.Fragment = mp.Fragment;
					return;
				}

				var before = remaining;
				(key, remaining, forced) = SplitPageName(remaining);
				if (forced)
				{
					// Second colon in a row. MediaWiki invalidates this, but for now, this is designed to always succeed, so return page name in main space with leading colon.
					this.Namespace = site[MediaWikiNamespaces.Main];
					this.originalParts.Add(TitlePartType.Namespace, string.Empty);
					this.ForcedNamespaceLink = false;
					pageName = before;
				}
				else if (site.Namespaces.ValueOrDefault(key) is Namespace ns2)
				{
					this.Namespace = ns2;
					this.originalParts.Add(TitlePartType.Namespace, key);
					pageName = remaining;
				}
			}
			else
			{
				pageName = remaining;
			}
		}

		var split = pageName.Split(TextArrays.Octothorpe, 2);
		if (split.Length == 2)
		{
			this.Fragment = split[1].Replace('_', ' ').TrimEnd();
			this.originalParts.Add(TitlePartType.Fragment, this.Fragment);
			pageName = split[0].TrimEnd();
		}

		var isLocal = this.IsLocal;
		if (this.Namespace is null)
		{
			var nsid = isLocal ? defaultNamespace : MediaWikiNamespaces.Main;
			this.Namespace = site[nsid];
			this.Coerced =
				isLocal &&
				defaultNamespace != MediaWikiNamespaces.Main &&
				nsid != MediaWikiNamespaces.Main;
		}

		this.PageName = isLocal
			? this.Namespace.CapitalizePageName(pageName)
			: pageName;
		this.originalParts.Add(TitlePartType.PageName, pageName);
	}

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class. Assumes title only contains namespace and page name.</summary>
	/// <param name="site">The site.</param>
	/// <param name="fullPageName">Name of the page.</param>
	private TitleFactory(Site site, string fullPageName)
	{
		var (key, remaining, forced) = SplitPageName(fullPageName);
		if (!forced && key.Length == 0)
		{
			fullPageName = remaining;
		}
		else if (site.Namespaces.ValueOrDefault(key) is Namespace ns)
		{
			this.Namespace = ns;
			this.originalParts.Add(TitlePartType.Namespace, key);
			this.ForcedNamespaceLink = forced;
			fullPageName = remaining;
		}

		if (this.Namespace is null)
		{
			this.Namespace = site[MediaWikiNamespaces.Main];
			this.Coerced = forced;
		}

		this.PageName = fullPageName;
		this.originalParts.Add(TitlePartType.PageName, fullPageName);
	}

	private TitleFactory(Namespace ns, string pageName)
	{
		// Shortcut constructor for times when a local page is known to be in the provided namespace. Capitalization is handled for semi-validated cases, such as modifications to an existing pagename or other known-good cases.
		this.Namespace = ns;
		this.originalParts.Add(TitlePartType.Namespace, ns.CanonicalName);
		var split = pageName.Split(TextArrays.Octothorpe, 2);
		if (split.Length == 2)
		{
			this.Fragment = split[1];
			pageName = split[0].TrimEnd();
		}

		this.originalParts.Add(TitlePartType.PageName, pageName);
		this.PageName = ns.CapitalizePageName(pageName);
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

	/// <summary>Gets the interwiki prefix.</summary>
	/// <value>The interwiki prefix.</value>
	public InterwikiEntry? Interwiki { get; }

	/// <summary>Gets a value indicating whether this object represents the local wiki (either via being a direct link or local interwiki link.</summary>
	public bool IsLocal => this.Interwiki is null || this.Interwiki.LocalWiki;

	/// <summary>Gets the namespace object for the title.</summary>
	/// <value>The namespace.</value>
	public Namespace Namespace { get; }

	/// <summary>Gets the original namespace for the title.</summary>
	/// <value>The namespace as originally specified. This could be an alias or case variant.</value>
	public IReadOnlyDictionary<TitlePartType, string> OriginalParts => this.originalParts;

	/// <summary>Gets a value indicating whether the namespace is displayed as part of the name.</summary>
	/// <value><see langword="true"/> if no namespace is present; otherwise, <see langword="false"/>.</value>
	/// <remarks>This value will be false for Main space links without a leading colon, Template calls (unless they actually specify <c>Template:</c>), and any gallery links that don't specify <c>File:</c>.</remarks>
	public bool NamespaceVisible { get; }

	/// <summary>Gets the name of the page without the namespace.</summary>
	/// <value>The name of the page without the namespace.</value>
	public string PageName { get; }

	/// <inheritdoc/>
	public Title Title => this.title ??= new Title(this.Namespace, this.PageName);
	#endregion

	#region Public Implicit Operators

	/// <summary>Implicit conversion to <see cref="FullTitle"/>.</summary>
	/// <param name="factory">The value to convert.</param>
	public static implicit operator FullTitle(TitleFactory factory) => new((IFullTitle)factory);

	/// <summary>Implicit conversion to <see cref="Robby.Title"/>.</summary>
	/// <param name="factory">The value to convert.</param>
	public static implicit operator Title(TitleFactory factory)
	{
		ArgumentNullException.ThrowIfNull(factory);
		return new(factory.Namespace, factory.PageName);
	}

	/// <summary>Implicit conversion to <see cref="FullTitle"/>.</summary>
	/// <param name="factory">The value to convert.</param>
	public static implicit operator SiteLink(TitleFactory factory) => new((ILinkTitle)factory);
	#endregion

	#region Public Static Methods

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class from the site, namespace ID, and <b>validated</b> full page name.</summary>
	/// <param name="site">The site the page is on.</param>
	/// <param name="nsId">The namespace the page should resolve to. If the resolved namespace doesn't match, this will throw an error.</param>
	/// <param name="fullPageName">Full name of the page.</param>
	public static TitleFactory CoValidate(Site site, int? nsId, string fullPageName)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(fullPageName);
		if (nsId is null)
		{
			return FromValidated(site, fullPageName);
		}

		TitleFactory retval = new(site, MediaWikiNamespaces.Main, fullPageName);
		return retval.Namespace.Id == nsId
			? retval
			: throw new InvalidOperationException("Namespace validation failed.");
	}

	/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
	/// <param name="site">The site the title is from.</param>
	/// <param name="node">The <see cref="IBacklinkNode"/> to parse.</param>
	/// <returns>A new FullTitle based on the provided values.</returns>
	public static TitleFactory FromBacklinkNode(Site site, IBacklinkNode node)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(node);
		return FromUnvalidated(site[MediaWikiNamespaces.Main], node.GetTitleText());
	}

	/// <summary>Shortcut method to create a <see cref="Title"/> from a template name.</summary>
	/// <param name="site">The site the template is from.</param>
	/// <param name="templateName">The template name.</param>
	/// <returns>A <see cref="Title"/> that defaults to being in Template namespace unless <paramref name="templateName"/> dictates otherwise.</returns>
	public static Title FromTemplate(Site site, string templateName)
	{
		ArgumentNullException.ThrowIfNull(site);
		return FromUnvalidated(site[MediaWikiNamespaces.Template], templateName);
	}

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
	/// <param name="site">The namespace the page is in.</param>
	/// <param name="fullPageName">Name of the page.</param>
	public static TitleFactory FromUnvalidated(Site site, string fullPageName) =>
		FromUnvalidated(site, MediaWikiNamespaces.Main, fullPageName);

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
	/// <param name="site">The namespace the page is in.</param>
	/// <param name="defaultNamespace">The default namespace for the page; if pageName starts with a namespace name, this parameter will be ignored.</param>
	/// <param name="pageName">Name of the page.</param>
	public static TitleFactory FromUnvalidated(Site site, int defaultNamespace, string pageName)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(pageName);
		pageName = WikiTextUtilities.TrimCruft(pageName);
		return new TitleFactory(site, defaultNamespace, pageName);
	}

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
	/// <param name="defaultNamespace">The default namespace for the page; if pageName starts with a namespace name, this parameter will be ignored.</param>
	/// <param name="pageName">Name of the page.</param>
	public static TitleFactory FromUnvalidated(Namespace defaultNamespace, string pageName)
	{
		ArgumentNullException.ThrowIfNull(defaultNamespace);
		ArgumentNullException.ThrowIfNull(pageName);
		pageName = WikiTextUtilities.TrimCruft(pageName);
		return new TitleFactory(defaultNamespace.Site, defaultNamespace.Id, pageName);
	}

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class from a known namespace and page name.</summary>
	/// <param name="ns">The namespace the page is in.</param>
	/// <param name="pageName">Name of the page.</param>
	/// <remarks>This method bypasses much of the parsing that the unvalidated method uses. The page name will always be in the namespace provided, even if it looks like it should be elsewhere (e.g., if ns is "Template" and pageName is "File:BadIdea", the title will be "Template:File:BadIdea".</remarks>
	public static TitleFactory FromValidated(Namespace ns, string pageName)
	{
		ArgumentNullException.ThrowIfNull(ns);
		ArgumentNullException.ThrowIfNull(pageName);
		return new(ns, pageName);
	}

	/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
	/// <param name="site">The site the page is on.</param>
	/// <param name="fullPageName">The full name of the page.</param>
	/// <remarks>This method bypasses much of the parsing that the unvalidated method uses and assumes that fullPageName represents only a local namespace and page name with no interwiki or anchor components.</remarks>
	public static TitleFactory FromValidated(Site site, string fullPageName)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(fullPageName);
		return new TitleFactory(site, fullPageName);
	}

	/// <summary>Removes invalid characters from a page name and replaces quote-like characters with straight quotes.</summary>
	/// <param name="pageName">The page name to sanitize.</param>
	/// <param name="extended">If <see langword="false"/>, only the characters <c>&lt;&gt;[]|{}</c> are stripped out. If <see langword="true"/>, the previous replacements will occur along with converting all quote-like characters to straight quotes and reducing multiple spaces to just one.</param>
	/// <returns>The original title with special characters replaced or removed as appropriate.</returns>
	/// <remarks>Although illegal as part of a page name, <c>#</c> symbols are not removed under the assumption that they indicate a fragment.</remarks>
	public static string SanitizePageName(string pageName, bool extended)
	{
		pageName = Regex.Replace(pageName, @"[<>\[\]\|{}]", string.Empty, RegexOptions.None, Globals.DefaultRegexTimeout);
		if (extended)
		{
			pageName = Regex.Replace(pageName, "[`´’ʻʾʿ᾿῾‘’]", "'", RegexOptions.None, Globals.DefaultRegexTimeout);
			pageName = Regex.Replace(pageName, "[“”„“«»]", "\"", RegexOptions.None, Globals.DefaultRegexTimeout);
			pageName = RegexLibrary.PruneExcessSpaces(pageName);
		}

		return pageName;
	}
	#endregion

	#region Public Methods

	/// <summary>Gets the full page name of a title.</summary>
	/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
	public string FullPageName() => this.Namespace.DecoratedName() + this.PageName;

	/// <summary>Checks that the title is valid.</summary>
	/// <param name="allowRelative">If <see langword="true"/>, zero-length page names will not trigger an error.</param>
	/// <value><see langword="true"/> if the page name contains no illegal characters; otherwise, <see langword="false"/>.</value>
	/// <remarks>Currently, the only requirements for validity are that the <see cref="PageName"/> property be of a suitable length (namespace dependent) and not contain any illegal characters. Legality is roughly based on $wgLegalTitleChars, but is not precisely identical, so bizarre use of Unicode characters could slip through, where they wouldn't do so on MediaWiki itself. Titles in File space have the additional characters from $wgIllegalFileChars checked.</remarks>
	public bool IsValid(bool allowRelative)
	{
		var pageName = this.PageName;
		if (!allowRelative && pageName.Length == 0)
		{
			return this.Interwiki is not null || this.Namespace == MediaWikiNamespaces.Main;
		}

		if (pageName.Length > (this.Namespace == MediaWikiNamespaces.Special ? 512 : 255))
		{
			return false;
		}

		if (pageName.Contains('.', StringComparison.Ordinal) && (
			pageName.Equals(".", StringComparison.Ordinal) ||
			pageName.Equals("..", StringComparison.Ordinal) ||
			pageName.StartsWith("./", StringComparison.Ordinal) ||
			pageName.StartsWith("../", StringComparison.Ordinal) ||
			pageName.Contains("/./", StringComparison.Ordinal) ||
			pageName.Contains("/../", StringComparison.Ordinal) ||
			pageName.EndsWith("/.", StringComparison.Ordinal) ||
			pageName.EndsWith("/..", StringComparison.Ordinal)))
		{
			return false;
		}

		if (pageName.Contains("~~~", StringComparison.Ordinal))
		{
			return false;
		}

		var version = this.Namespace.Site.Version;
		var illegalChars = @"\x00-\x1F\x7F#<>\[\]\|{}";
		if (this.Namespace == MediaWikiNamespaces.File)
		{
			illegalChars += version >= new Version(1, 28)
				? @":/\\"
				: ":";
		}
		else if (this.Namespace == MediaWikiNamespaces.User)
		{
			if (version >= new Version(1, 40))
			{
				illegalChars += "@:>=";
			}
			else if (version >= new Version(1, 39))
			{
				illegalChars += "@:>";
			}
			else if (version >= new Version(1, 26))
			{
				illegalChars += "@:";
			}
			else if (version >= new Version(1, 15))
			{
				illegalChars += '@';
			}
		}

		var illegalRegex = new Regex('[' + illegalChars + ']', RegexOptions.None, Globals.DefaultRegexTimeout);
		return !illegalRegex.Match(pageName).Success;
	}
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

		return iwColon + interwiki + nsColon + this.FullPageName() + fragment;
	}

	/// <summary>Converts the current title to a <see cref="FullTitle"/>.</summary>
	/// <returns>A new FullTitle with the relevant properties set.</returns>
	public FullTitle ToFullTitle() => (FullTitle)this;

	/// <summary>Converts the current title to a <see cref="SiteLink"/>.</summary>
	/// <returns>A new SiteLink with the relevant properties set.</returns>
	public SiteLink ToSiteLink() => (SiteLink)this;

	/// <summary>Converts the current title to a <see cref="Robby.Title"/>.</summary>
	/// <returns>A new SiteLink with the relevant properties set.</returns>
	public Title ToTitle() => (Title)this;
	#endregion

	#region Private Static Methods
	private static (string Key, string PageName, bool Forced) SplitPageName(string pageName)
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
	#endregion
}