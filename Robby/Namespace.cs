namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static WikiCommon.Globals;

	/// <summary>Represents a MediaWiki namespace for a specific site.</summary>
	public class Namespace
	{
		#region Fields
		private readonly HashSet<string> allNames;
		private readonly HashSet<string> addedNames;
		private readonly HashSet<string> baseNames;
		private readonly int subjectSpaceId;
		private readonly int? talkSpaceId;
		#endregion

		#region Constructors
		internal Namespace(Site site, NamespacesItem ns, IEnumerable<string> aliases)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
			this.Id = ns.Id;

			// We can't actually populate SubjectSpace and TalkSpace here because they may not both be present in Site.Namespaces at this time, so only populate the local variables.
			this.subjectSpaceId = ns.Id >= MediaWikiNamespaces.Main ? ns.Id & 0x7ffffffe : ns.Id;
			this.talkSpaceId = ns.Id >= MediaWikiNamespaces.Main ? new int?(ns.Id | 1) : null;

			this.AllowsSubpages = ns.Flags.HasFlag(NamespaceFlags.Subpages);
			this.CaseSensitive = ns.Flags.HasFlag(NamespaceFlags.CaseSensitive);
			this.IsContentSpace = ns.Flags.HasFlag(NamespaceFlags.ContentSpace);

			this.Name = ns.Name;
			this.CanonicalName = ns.CanonicalName;
			this.DecoratedName = this.Id == MediaWikiNamespaces.Main ? string.Empty : this.Name + ':';
			this.LinkName = (this.Id == MediaWikiNamespaces.File || this.Id == MediaWikiNamespaces.Category ? ":" : string.Empty) + this.DecoratedName;
			this.Aliases = aliases == null ? new List<string>() : new List<string>(aliases);

			this.addedNames = new HashSet<string>(site.EqualityComparerInsensitive);
			this.baseNames = new HashSet<string>(this.Aliases, site.EqualityComparerInsensitive)
			{
				ns.Name,
				ns.CanonicalName
			};
			this.baseNames.TrimExcess();
			this.allNames = new HashSet<string>(this.baseNames, site.EqualityComparerInsensitive);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a list of aliases for the namespace (e.g., "WP" for "Wikipedia" space).</summary>
		/// <value>The aliases for the namespace, as defined by the specific MediaWiki installation.</value>
		public IReadOnlyList<string> Aliases { get; }

		/// <summary>Gets a value indicating whether the namespace allows subpages.</summary>
		/// <value><c>true</c> if the namespace allows subpages; otherwise, <c>false</c>.</value>
		public bool AllowsSubpages { get; }

		/// <summary>Gets the canonical name of the namespace.</summary>
		/// <value>The canonical name of the namespace. For built-in namespaces, this is the default English name of the namespace (e.g., File, Project talk, etc.).</value>
		public string CanonicalName { get; }

		/// <summary>Gets a value indicating whether the first letter of the namespace name is case-sensitive.</summary>
		/// <value><c>true</c> if the first letter of the namespace name is case-sensitive; otherwise, <c>false</c>.</value>
		public bool CaseSensitive { get; }

		/// <summary>Gets or sets the decorated name of the namespace.</summary>
		/// <value>The decorated name of the namespace.</value>
		/// <remarks>By default, this is the primary name with a trailing colon for most namespaces; in Main space, however, it's an empty string. It is publicly settable for special cases.</remarks>
		public string DecoratedName { get; set; }

		/// <summary>Gets the MediaWiki ID for the namespace.</summary>
		/// <value>The MediaWiki ID for the namespace.</value>
		public int Id { get; }

		/// <summary>Gets a value indicating whether this namespace is counted as content space.</summary>
		/// <value><c>true</c> if this namespace is counted as content space; otherwise, <c>false</c>.</value>
		public bool IsContentSpace { get; }

		/// <summary>Gets a value indicating whether this instance is subject space.</summary>
		/// <value><c>true</c> if this instance is a subject namespace; otherwise, <c>false</c>.</value>
		public bool IsSubjectSpace => this.Id == this.subjectSpaceId;

		/// <summary>Gets a value indicating whether this instance is talk space.</summary>
		/// <value><c>true</c> if this instance is talk namespace; otherwise, <c>false</c>.</value>
		public bool IsTalkSpace => this.Id == this.talkSpaceId;

		/// <summary>Gets or sets the name to be used in links.</summary>
		/// <value>The name of the namespace as used in a link.</value>
		/// <remarks>By default, in Category and File space, a colon will automatically be prepended to skip the magic linking and use normal linking instead. It is left publicly settable for special cases.</remarks>
		public string LinkName { get; set; }

		/// <summary>Gets the primary name of the namespace.</summary>
		/// <value>The primary name of the namespace.</value>
		public string Name { get; }

		/// <summary>Gets the site to which this namespace belongs.</summary>
		/// <value>The site.</value>
		public Site Site { get; }

		/// <summary>Gets the MediaWiki ID for the subject space.</summary>
		/// <value>The MediaWiki ID for the subject space.</value>
		public Namespace SubjectSpace => this.Site.Namespaces[this.subjectSpaceId];

		/// <summary>Gets the MediaWiki ID for the talk space, if applicable.</summary>
		/// <value>The MediaWiki ID for the talk space, if applicable; otherwise, <c>null</c>.</value>
		public Namespace TalkSpace => this.talkSpaceId == null ? null : this.Site.Namespaces[this.talkSpaceId.Value];
		#endregion

		#region Public Operators

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if string is equal to any of the names representing the namespace.</returns>
		public static bool operator ==(Namespace left, Namespace right) => left?.Site == right?.Site && left?.Id == right?.Id;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if the namespace Site or Id are not equal.</returns>
		public static bool operator !=(Namespace left, Namespace right) => !(left == right);

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if the integer provided equals the namespace ID.</returns>
		public static bool operator ==(Namespace left, int right) => left?.Id == right;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if the integer provided equals the namespace ID.</returns>
		public static bool operator ==(int left, Namespace right) => left == right?.Id;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if the integer provided does not equal the namespace ID.</returns>
		public static bool operator !=(Namespace left, int right) => left?.Id != right;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if the integer provided does not equal the namespace ID.</returns>
		public static bool operator !=(int left, Namespace right) => left != right?.Id;
		#endregion

		#region Public Methods

		/// <summary>Adds a name to the lookup list.</summary>
		/// <param name="name">The name.</param>
		public void AddName(string name)
		{
			if (!this.allNames.Contains(name))
			{
				this.addedNames.Add(name);
				this.allNames.Add(name);
			}
		}

		/// <summary>Capitalizes the page name based on the namespace's case-sensitivity.</summary>
		/// <param name="pageName">Name of the page.</param>
		/// <returns>If the namespace isn't case sensitive, and the page name begins with a lower-case letter, returns the capitalized version of the page name; otherwise, the page name is returned unaltered.</returns>
		public string CapitalizePageName(string pageName)
		{
			ThrowNull(pageName, nameof(pageName));
			if (this.CaseSensitive || pageName.Length == 0)
			{
				return pageName;
			}

			if (char.IsLower(pageName[0]))
			{
				return pageName.Length == 1 ? char.ToUpper(pageName[0], this.Site.Culture).ToString() : char.ToUpper(pageName[0], this.Site.Culture) + pageName.Substring(1);
			}

			return pageName;
		}

		/// <summary>Determines whether the name specified is in the list of names for this namespace.</summary>
		/// <param name="name">The name to locate.</param>
		/// <returns><c>true</c> if the name list for the namespace contains the specified name; otherwise, <c>false</c>.</returns>
		public bool Contains(string name) => this.allNames.Contains(name);

		/// <summary>Checks if two page names are the same, based on the case-sensitivity for the namespace.</summary>
		/// <param name="pageName1">The page name to check.</param>
		/// <param name="pageName2">The page name to compare to.</param>
		/// <returns><see langword="true" /> if the two string are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string pageName1, string pageName2)
		{
			ThrowNull(pageName1, nameof(pageName1));
			ThrowNull(pageName2, nameof(pageName2));
			if (pageName1.Length != pageName2.Length)
			{
				// Quick check to rule out most cases before we do string building.
				return false;
			}

			var siteCulture = this.Site.Culture;
			return this.CaseSensitive
				? pageName1 == pageName2
				: pageName1.UpperFirst(siteCulture) == pageName2.UpperFirst(siteCulture);
		}

		/// <summary>Removes a name from the lookup list. Only names that have been previously added can be removed.</summary>
		/// <param name="name">The name.</param>
		public void RemoveName(string name)
		{
			if (this.addedNames.Remove(name))
			{
				this.allNames.Remove(name);
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Determines whether the specified <see cref="object" />, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj)
		{
			var other = obj as Namespace;
			return other == null ? false : this.Site == other.Site && this.Id == other.Id;
		}

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. </returns>
		public override int GetHashCode() => CompositeHashCode(this.Site.GetHashCode(), this.Id.GetHashCode());

		/// <summary>Returns a <see cref="string" /> that represents this instance using the primary name of the namespace.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name;
		#endregion
	}
}