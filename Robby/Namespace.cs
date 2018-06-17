﻿namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using WallE.Base;
	using static WikiCommon.Globals;

	/// <summary>Represents a MediaWiki namespace for a specific site.</summary>
	/// <seealso cref="System.IEquatable{T}" />
	public class Namespace : IEquatable<Namespace>, IEquatable<int>
	{
		#region Fields
		private readonly HashSet<string> allNames = new HashSet<string>();
		#endregion

		#region Constructors
		internal Namespace(Site site, NamespacesItem ns, List<string> aliases)
		{
			ThrowNull(site, nameof(site));

			this.Site = site;

			this.Id = ns.Id;
			this.SubjectSpaceId = ns.Id >= 0 ? ns.Id & 0x7ffffffe : ns.Id;
			this.TalkSpaceId = ns.Id >= 0 ? new int?(ns.Id | 1) : null;

			this.AllowsSubpages = ns.Flags.HasFlag(NamespaceFlags.Subpages);
			this.CaseSensitive = ns.Flags.HasFlag(NamespaceFlags.CaseSensitive);
			this.ContentSpace = ns.Flags.HasFlag(NamespaceFlags.ContentSpace);

			this.Name = ns.Name;
			this.CanonicalName = ns.CanonicalName;
			this.DecoratedName = ns.Id == 0 ? string.Empty : ns.Name + ':';

			this.AddName(ns.Name);
			this.AddName(ns.CanonicalName);
			if (aliases == null)
			{
				this.Aliases = new List<string>();
			}
			else
			{
				this.Aliases = aliases.AsReadOnly();
				foreach (var item in aliases)
				{
					this.AddName(item);
				}
			}

			this.allNames.TrimExcess();
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a list of aliases for the namespace (e.g., "WP" for "Wikipedia" space).</summary>
		/// <value>The aliases for the namespace, as defined by the specific MediaWiki installation.</value>
		public IReadOnlyList<string> Aliases { get; }

		/// <summary>Gets a collection of all names that can be used to refer to the namespace.</summary>
		/// <value>A collection of all names that can be used to refer to the namespace.</value>
		/// <remarks>This collection is case-sensitive and should normally include all valid case variants based on the first letter.</remarks>
		public IReadOnlyCollection<string> AllNames => this.allNames;

		/// <summary>Gets a value indicating whether the namespace allows subpages.</summary>
		/// <value><c>true</c> if the namespace allows subpages; otherwise, <c>false</c>.</value>
		public bool AllowsSubpages { get; }

		/// <summary>Gets the canonical name of the namespace.</summary>
		/// <value>The canonical name of the namespace. For built-in namespaces, this is the default English name of the namespace (e.g., File, Project talk, etc.).</value>
		public string CanonicalName { get; }

		/// <summary>Gets a value indicating whether the first letter of the namespace name is case-sensitive.</summary>
		/// <value><c>true</c> if the first letter of the namespace name is case-sensitive; otherwise, <c>false</c>.</value>
		public bool CaseSensitive { get; }

		/// <summary>Gets a value indicating whether this namespace is counted as content space.</summary>
		/// <value><c>true</c> if this namespace is counted as content space; otherwise, <c>false</c>.</value>
		public bool ContentSpace { get; }

		/// <summary>Gets the decorated name of the namespace.</summary>
		/// <value>The decorated name of the namespace, which is the primary name with a trailing colon; in Main space, this is an empty string.</value>
		public string DecoratedName { get; }

		/// <summary>Gets the MediaWiki ID for the namespace.</summary>
		/// <value>The MediaWiki ID for the namespace.</value>
		public int Id { get; }

		/// <summary>Gets the primary name of the namespace.</summary>
		/// <value>The primary name of the namespace.</value>
		public string Name { get; }

		/// <summary>Gets the site to which this namespace belongs.</summary>
		/// <value>The site.</value>
		public Site Site { get; }

		/// <summary>Gets the MediaWiki ID for the subject space.</summary>
		/// <value>The MediaWiki ID for the subject space.</value>
		public int SubjectSpaceId { get; }

		/// <summary>Gets the MediaWiki ID for the talk space, if applicable.</summary>
		/// <value>The MediaWiki ID for the talk space, if applicable; otherwise, <c>null</c>.</value>
		public int? TalkSpaceId { get; }
		#endregion

		#region Public Operators

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if string is equal to any of the names representing the namespace.</returns>
		public static bool operator ==(Namespace left, string right) => left?.allNames.Contains(right) ?? false;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if string is equal to any of the names representing the namespace.</returns>
		public static bool operator ==(string left, Namespace right) => right?.allNames.Contains(left) ?? false;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if string is not equal to any of the names representing the namespace.</returns>
		public static bool operator !=(Namespace left, string right) => !left?.allNames.Contains(right) ?? true;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><c>true</c> if string is not equal to any of the names representing the namespace.</returns>
		public static bool operator !=(string left, Namespace right) => right?.allNames.Contains(left) ?? true;

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

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns><see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
		public bool Equals(Namespace other) => other == null ? false : this.Site.Equals(other.Site) && this.Id.Equals(other.Id);

		/// <summary>Indicates whether the id of the current object is equal to the given integer.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns><see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
		public bool Equals(int other) => this.Id == other;
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

		#region Internal Methods
		internal string AddName(string name)
		{
			this.allNames.Add(name);
			if (!this.CaseSensitive && name.Length > 0)
			{
				var lowerName = this.Site.Culture.TextInfo.ToLower(name[0]) + (name.Length == 1 ? string.Empty : name.Substring(1));
				this.allNames.Add(lowerName);

				return lowerName;
			}

			return name;
		}
		#endregion
	}
}