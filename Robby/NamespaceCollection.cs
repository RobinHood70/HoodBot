namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	// Although this acts like both a string- and int-keyed dictionary, implementing IReadOnlyDictionary would add nothing useful to this that I can see.

	/// <summary>Read-only Namespace dictionary that can be referenced by ID and all valid names for the namespace.</summary>
	public sealed class NamespaceCollection : IReadOnlyCollection<Namespace>
	{
		private readonly IDictionary<int, Namespace> idsDictionary;
		private readonly IDictionary<string, Namespace> namesDictionary;
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="NamespaceCollection" /> class.</summary>
		/// <param name="site">The site the namespaces belong to.</param>
		/// <param name="namespaces">All namespace objects.</param>
		/// <param name="namespaceAliases">All namespace aliases.</param>
		internal NamespaceCollection(Site site, IReadOnlyList<SiteInfoNamespace> namespaces, IReadOnlyList<SiteInfoNamespaceAlias> namespaceAliases)
		{
			// From Language->getNsIndeix(), creates a case-insensitive comparer for the wiki's culture.
			StringComparer comparer = StringComparer.Create(site.NotNull(nameof(site)).Culture, true);

			// NamespaceAliases
			Dictionary<int, HashSet<string>> aliasesById = new();
			foreach (var ns in namespaces)
			{
				aliasesById.Add(ns.Id, new HashSet<string>(new[] { ns.CanonicalName, ns.Name }, comparer));
			}

			foreach (var item in namespaceAliases)
			{
				aliasesById[item.Id].Add(item.Alias);
			}

			this.idsDictionary = new SortedList<int, Namespace>(namespaces.Count);
			Dictionary<string, Namespace> names = new(namespaces.Count * 2 + namespaceAliases.Count, comparer);
			foreach (var item in namespaces)
			{
				var allNames = aliasesById[item.Id];
				allNames.TrimExcess();
				Namespace ns = new(site, item, allNames);
				this.idsDictionary.Add(ns.Id, ns);
				foreach (var name in allNames)
				{
					names.Add(name, ns);
				}
			}

			// Names are trimmed because CanonicalName and Name might be the same.
			names.TrimExcess();
			this.namesDictionary = names;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the namespace collection as a collection of their IDs.</summary>
		/// <value>The namespace collection as a collection of their IDs.</value>
		public IEnumerable<int> AllIds => this.idsDictionary.Keys;

		/// <summary>Gets the number of namespaces in the collection.</summary>
		/// <value>The number of namespaces in the collection.</value>
		public int Count => this.idsDictionary.Count;

		/// <summary>Gets the namespace collection as a collection of their IDs, excluding the special namespaces.</summary>
		/// <value>The namespace collection as a collection of their IDs, excluding the special namespaces.</value>
		public IEnumerable<int> RegularIds
		{
			get
			{
				foreach (var id in this.idsDictionary.Keys)
				{
					if (id >= 0)
					{
						yield return id;
					}
				}
			}
		}

		/// <summary>Gets the talk namespaces in the collection as a collection of their IDs, excluding the special namespaces.</summary>
		/// <value>The talk namespaces in the collection as a collection of their IDs, excluding the special namespaces.</value>
		public IEnumerable<int> TalkIds
		{
			get
			{
				foreach (var id in this.idsDictionary.Keys)
				{
					if (id >= 0 && (id & 1) == 1)
					{
						yield return id;
					}
				}
			}
		}
		#endregion

		#region Public Indexers

		/// <summary>Gets the element with the specified key.</summary>
		/// <param name="id">The ID of the namespace.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
		/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
		public Namespace this[int id] => this.idsDictionary[id];

		/// <summary>Gets the element with the specified key.</summary>
		/// <param name="key">Any valid name for the namespace.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, it will be tried again after HTML decoding the key and normalizing space characters. If that also fails, an exception is thrown.</returns>
		/// <exception cref="ArgumentNullException">The key was null.</exception>
		/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
		public Namespace this[string key] => this.namesDictionary.TryGetValue(key, out var retval)
			? retval
			: this.namesDictionary[WikiTextUtilities.DecodeAndNormalize(key)];
		#endregion

		#region Public Methods

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="id">The namespace ID to locate in the collection.</param>
		/// <returns>True if the collection contains the relevant namespace.</returns>
		public bool Contains(int id) => this.idsDictionary.ContainsKey(id);

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to locate in the collection.</param>
		/// <returns>True if the collection contains the relevant namespace.</returns>
		/// <exception cref="ArgumentNullException">The name is null.</exception>
		public bool Contains(string name) =>
			this.namesDictionary.ContainsKey(name) ||
			this.namesDictionary.ContainsKey(WikiTextUtilities.DecodeAndNormalize(name));

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Namespace> GetEnumerator() => this.idsDictionary.Values.GetEnumerator();

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.idsDictionary.Values.GetEnumerator();

		/// <summary>Returns the namespace associated with the specified ID.</summary>
		/// <param name="id">The namespace ID to search for.</param>
		/// <param name="value">The namespace object, if found; otherwise, null.</param>
		/// <returns>True if the collection contains the desired namespace.</returns>
		public bool TryGetValue(int id, [MaybeNullWhen(false)] out Namespace value) => this.idsDictionary.TryGetValue(id, out value!);

		/// <summary>Returns the namespace associated with the specified name.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to search for.</param>
		/// <param name="value">The namespace object, if found; otherwise, null.</param>
		/// <returns>True if the collection contains the desired namespace.</returns>
		/// <exception cref="ArgumentNullException">The name is null.</exception>
		public bool TryGetValue(string name, [MaybeNullWhen(false)] out Namespace value) =>
			this.namesDictionary.TryGetValue(name, out value) ||
			this.namesDictionary.TryGetValue(WikiTextUtilities.DecodeAndNormalize(name), out value);

		/// <summary>Returns the namespace associated with the specified ID.</summary>
		/// <param name="id">The namespace ID to search for.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public Namespace? ValueOrDefault(int id) => this.idsDictionary.TryGetValue(id, out var value) ? value : default;

		/// <summary>Returns the namespace associated with the specified name.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to search for.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public Namespace? ValueOrDefault(string? name) => name != null && this.TryGetValue(name, out var value) ? value : default;
		#endregion
	}
}