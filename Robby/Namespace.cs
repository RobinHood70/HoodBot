namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Properties;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

/// <summary>Represents a MediaWiki namespace for a specific site.</summary>
public sealed class Namespace : IEquatable<Namespace>
{
	#region Fields
	private readonly HashSet<string> allNames;
	private readonly int hashCode;
	private readonly int subjectSpaceId;
	private readonly int? talkSpaceId;
	#endregion

	#region Constructors
	internal Namespace(Site site, SiteInfoNamespace ns, HashSet<string> allNames)
	{
		ArgumentNullException.ThrowIfNull(site);
		this.Site = site;
		this.Id = ns.Id;
		this.hashCode = HashCode.Combine(site, ns.Id);
		this.Flags = ns.Flags;
		this.Name = ns.Name;
		this.CanonicalName = ns.CanonicalName;
		this.allNames = allNames;

		// We can't actually populate SubjectSpace and TalkSpace here because they may not both be present in Site.Namespaces at this time, so only populate the local variables.
		this.subjectSpaceId = this.CanTalk ? ns.Id & 0x7ffffffe : ns.Id;
		this.talkSpaceId = this.CanTalk ? ns.Id | 1 : null;
	}
	#endregion

	#region Public Static Properties

	/// <summary>Gets a <see cref="Comparison{T}"/> intended for sorting purposes only.</summary>
	/// <remarks>This is defined as a Comparison rather than making the class IComparable since less-than/greater-than semantics don't really make sense outside of sorting.</remarks>
	public static Comparison<Namespace> SortComparer => new(Compare);
	#endregion

	#region Public Properties

	/// <summary>Gets all valid names for the namespace, including the <see cref="CanonicalName"/>, <see cref="Name"/>, and any aliases defined for the namespace.</summary>
	public IReadOnlySet<string> AllNames => this.allNames;

	/// <summary>Gets a value indicating whether the namespace allows subpages.</summary>
	/// <value><see langword="true"/> if the namespace allows subpages; otherwise, <see langword="false"/>.</value>
	public bool AllowsSubpages => this.Flags.HasAnyFlag(NamespaceFlags.AllowsSubpages);

	/// <summary>Gets the canonical name of the namespace.</summary>
	/// <value>The canonical name of the namespace. For built-in namespaces, this is the default English name of the namespace (e.g., File, Project talk, etc.).</value>
	public string CanonicalName { get; }

	/// <summary>Gets a value indicating whether this page can have editable content.</summary>
	/// <value><see langword="true"/> if the namespace can have editable content; otherwise, <see langword="false"/> (e.g., for Special and Media spaces).</value>
	public bool CanTalk => this.Flags.HasAnyFlag(NamespaceFlags.CanTalk);

	/// <summary>Gets a value indicating whether the first letter of the namespace name is case-sensitive.</summary>
	/// <value><see langword="true"/> if the first letter of the namespace name is case-sensitive; otherwise, <see langword="false"/>.</value>
	public bool CaseSensitive => this.Flags.HasAnyFlag(NamespaceFlags.CaseSensitive);

	/// <summary>Gets the behavioural properties of the namespace.</summary>
	/// <value>The various boolean flags indicating which behaviours are permitted.</value>
	public NamespaceFlags Flags { get; }

	/// <summary>Gets the MediaWiki ID for the namespace.</summary>
	/// <value>The MediaWiki ID for the namespace.</value>
	public int Id { get; }

	/// <summary>Gets a value indicating whether this namespace is counted as content space.</summary>
	/// <value><see langword="true"/> if this namespace is counted as content space; otherwise, <see langword="false"/>.</value>
	public bool IsContentSpace => this.Flags.HasAnyFlag(NamespaceFlags.ContentSpace);

	/// <summary>Gets a value indicating whether this namespace requires a colon to be prepended in order to create a link.</summary>
	public bool IsForcedLinkSpace => this.Flags.HasAnyFlag(NamespaceFlags.ForcedLinkSpace);

	/// <summary>Gets a value indicating whether this instance is subject space.</summary>
	/// <value><see langword="true"/> if this instance is a subject namespace; otherwise, <see langword="false"/>.</value>
	public bool IsSubjectSpace => this.Id == this.subjectSpaceId;

	/// <summary>Gets a value indicating whether this instance is talk space.</summary>
	/// <value><see langword="true"/> if this instance is talk namespace; otherwise, <see langword="false"/>.</value>
	public bool IsTalkSpace => this.Id == this.talkSpaceId;

	/// <summary>Gets the primary name of the namespace.</summary>
	/// <value>The primary name of the namespace.</value>
	public string Name { get; }

	/// <summary>Gets the site to which this namespace belongs.</summary>
	/// <value>The site.</value>
	public Site Site { get; }

	/// <summary>Gets the subject space.</summary>
	/// <value>The subject space.</value>
	public Namespace SubjectSpace => this.Site[this.subjectSpaceId];

	/// <summary>Gets the talk space, if applicable.</summary>
	/// <value>The talk space, if applicable; otherwise, <see langword="null"/>.</value>
	/// <remarks>This will only be <see langword="null"/> for namespaces that don't support talk pages, like Media and Special.</remarks>
	public Namespace? TalkSpace => this.talkSpaceId == null ? null : this.Site[this.talkSpaceId.Value];
	#endregion

	#region Public Operators

	/// <summary>Implements the operator ==.</summary>
	/// <param name="left">The left-hand side of the comparison.</param>
	/// <param name="right">The right-hand side of the comparison.</param>
	/// <returns><see langword="true"/> if string is equal to any of the names representing the namespace.</returns>
	public static bool operator ==(Namespace? left, Namespace? right) => left is null ? right is null : left.Equals(right);

	/// <summary>Implements the operator !=.</summary>
	/// <param name="left">The left-hand side of the comparison.</param>
	/// <param name="right">The right-hand side of the comparison.</param>
	/// <returns><see langword="true"/> if the namespace Site or Id are not equal.</returns>
	public static bool operator !=(Namespace? left, Namespace? right) => !(left == right);

	/// <summary>Implements the operator !=.</summary>
	/// <param name="left">The left-hand side of the comparison.</param>
	/// <param name="right">The right-hand side of the comparison.</param>
	/// <returns><see langword="true"/> if string is equal to any of the names representing the namespace.</returns>
	public static bool operator ==(Namespace? left, int right) => left is not null && left.Id == right;

	/// <summary>Implements the operator !=.</summary>
	/// <param name="left">The left-hand side of the comparison.</param>
	/// <param name="right">The right-hand side of the comparison.</param>
	/// <returns><see langword="true"/> if string is equal to any of the names representing the namespace.</returns>
	public static bool operator ==(int left, Namespace? right) => right is not null && left == right.Id;

	/// <summary>Implements the operator !=.</summary>
	/// <param name="left">The left-hand side of the comparison.</param>
	/// <param name="right">The right-hand side of the comparison.</param>
	/// <returns><see langword="true"/> if the namespace Site or Id are not equal.</returns>
	public static bool operator !=(Namespace? left, int right) => !(left == right);

	/// <summary>Implements the operator !=.</summary>
	/// <param name="left">The left-hand side of the comparison.</param>
	/// <param name="right">The right-hand side of the comparison.</param>
	/// <returns><see langword="true"/> if the namespace Site or Id are not equal.</returns>
	public static bool operator !=(int left, Namespace? right) => !(left == right);
	#endregion

	#region Public Static Methods

	/// <summary>Compares two <see cref="Namespace"/>s and returns an integer indicating the sort position of the first relative to the second.</summary>
	/// <param name="x">The first Namespace.</param>
	/// <param name="y">The second Namespace.</param>
	/// <returns>An integer indicating whether the first Namespace is less than (-1), equal to (0), or greater than (1) the second Namespace.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the Site values don't match.</exception>
	/// <remarks>This is not implemented as an IComparer because less-than/greater-than semantics only really apply in the context of sorting. The Comparer is made public primarily for the convenience of other sorting methods.</remarks>
	public static int Compare(Namespace? x, Namespace? y) => x is null
		? y is null ? 0 : -1
		: y is null ? 1 : x.Site == y.Site
			? x.Id.CompareTo(y.Id)
			: throw new InvalidOperationException(Resources.SiteMismatch);
	#endregion

	#region Public Methods

	/// <summary>Gets a name that's suitable for cases when a namespace is assumed, such as template calls.</summary>
	/// <param name="ns">The namespace ID.</param>
	/// <returns>An empty string if the namespace ID provided matches the current namespace ID; otherwise, returns the canonical namespace name with a colon.</returns>
	public string AssumedName(int ns) => this.Id == ns ? string.Empty : (this.Name + ':');

	/// <summary>Capitalizes the page name based on the namespace's case-sensitivity.</summary>
	/// <param name="pageName">Name of the page.</param>
	/// <returns>If the namespace isn't case sensitive, and the page name begins with a lower-case letter, returns the capitalized version of the page name; otherwise, the page name is returned unaltered.</returns>
	public string CapitalizePageName(string pageName)
	{
		ArgumentNullException.ThrowIfNull(pageName);
		return this.CaseSensitive || pageName.Length == 0
			? pageName
			: pageName.UpperFirst(this.Site.Culture);
	}

	/// <summary>Compares two page names based on the site culture and first-letter policy for the namespace.</summary>
	/// <param name="x">This first page name to compare.</param>
	/// <param name="y">The second page name to compare.</param>
	/// <returns>An integer that indicates whether the first page name is less than the second (-1), equal to the second (0), or greater than the second (1).</returns>
	public int ComparePageNames(string? x, string? y)
	{
		var comparer = this.Site.GetPageNameComparer(this.CaseSensitive);
		return comparer.Compare(x, y);
	}

	/// <summary>Determines whether the name specified is in the list of names for this namespace.</summary>
	/// <param name="name">The name to locate.</param>
	/// <returns><see langword="true"/> if the name list for the namespace contains the specified name; otherwise, <see langword="false"/>.</returns>
	public bool Contains(string name) => this.allNames.Contains(name);

	/// <summary>Gets the decorated name of the namespace.</summary>
	/// <value>The decorated name of the namespace.</value>
	public string DecoratedName() => this.Id == MediaWikiNamespaces.Main ? string.Empty : this.Name + ':';

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns><see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
	public bool Equals(Namespace? other) =>
		other is not null &&
		this.GetHashCode() == other.GetHashCode() &&
		this.Id == other.Id &&
		this.Site == other.Site;

	/// <summary>Gets the name to be used in links.</summary>
	/// <value>The name of the namespace as used in a link.</value>
	/// <remarks>By default, in Category and File space, a colon will automatically be prepended to skip the magic linking and use normal linking instead.</remarks>
	public string LinkName() => this.IsForcedLinkSpace ? ":" + this.DecoratedName() : this.DecoratedName();

	/// <summary>Checks if two page names are the same, based on the case-sensitivity for the namespace.</summary>
	/// <param name="pageName1">The page name to check.</param>
	/// <param name="pageName2">The page name to compare to.</param>
	/// <returns><see langword="true" /> if the two page names are considered the same; otherwise <see langword="false" />.</returns>
	/// <remarks>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
	public bool PageNameEquals(string pageName1, string pageName2) => this.PageNameEquals(pageName1, pageName2, true);

	/// <summary>Checks if two page names are the same, based on the case-sensitivity for the namespace.</summary>
	/// <param name="pageName1">The page name to check.</param>
	/// <param name="pageName2">The page name to compare to.</param>
	/// <param name="normalize">Inidicates whether the page names should be normalized before comparison.</param>
	/// <returns><see langword="true" /> if the two page names are considered the same; otherwise <see langword="false" />.</returns>
	/// <remarks>
	/// <para>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</para>
	/// <para>If both pages come from Title-based objects or are otherwise guaranteed to be normalized already, set <paramref name="normalize"/> to <see langword="false"/> for faster comparison; user-provided titles should be normalized to ensure correct matching.</para></remarks>
	public bool PageNameEquals(string pageName1, string pageName2, bool normalize)
	{
		if (normalize)
		{
			pageName1 = WikiTextUtilities.DecodeAndNormalize(pageName1);
			pageName2 = WikiTextUtilities.DecodeAndNormalize(pageName2);
		}

		var comparer = this.Site.GetPageNameComparer(this.CaseSensitive);
		return comparer.Compare(pageName1, pageName2) == 0;
	}
	#endregion

	#region Public Override Methods

	/// <summary>Determines whether the specified <see cref="object" />, is equal to this instance.</summary>
	/// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
	/// <returns><see langword="true"/> if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
	public override bool Equals(object? obj) => this.Equals(obj as Namespace);

	/// <summary>Returns a hash code for this instance.</summary>
	/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. </returns>
	public override int GetHashCode() => this.hashCode;

	/// <summary>Returns a <see cref="string" /> that represents this instance using the primary name of the namespace.</summary>
	/// <returns>A <see cref="string" /> that represents this instance.</returns>
	public override string ToString() => this.Name;
	#endregion
}