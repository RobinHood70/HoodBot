namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	/// <summary>Read-only Namespace dictionary that can be referenced by ID and all valid names for the namespace.</summary>
	public class NamespaceCollection : IReadOnlyCollection<Namespace>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="NamespaceCollection" /> class.</summary>
		/// <param name="namespaces">An enumeration of all namespace objects.</param>
		/// <param name="comparer">The site's case-insensitive EqualityComparer.</param>
		internal NamespaceCollection(IEnumerable<Namespace> namespaces, IEqualityComparer<string> comparer)
		{
			// CONSIDER: Implementing namespace-specific case-sensitivity, which is supported by the wiki software, though rarely used.
			var names = new Dictionary<string, Namespace>(comparer);
			this.NamesDictionary = names;
			foreach (var ns in namespaces)
			{
				this.IdsDictionary.Add(ns.Id, ns);
				names[ns.Name] = ns;
				names[ns.CanonicalName] = ns;
				foreach (var name in ns.Aliases)
				{
					names[name] = ns;
				}
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the namespace collection as a collection of their IDs.</summary>
		/// <value>The namespace collection as a collection of their IDs.</value>
		public IEnumerable<int> AllIds => this.IdsDictionary.Keys;

		/// <summary>Gets the number of namespaces in the collection.</summary>
		/// <value>The number of namespaces in the collection.</value>
		public int Count => this.IdsDictionary.Count;

		/// <summary>Gets the namespace collection as a collection of their IDs, excluding the special namespaces.</summary>
		/// <value>The namespace collection as a collection of their IDs, excluding the special namespaces.</value>
		public IEnumerable<int> RegularIds
		{
			get
			{
				foreach (var id in this.IdsDictionary.Keys)
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
				foreach (var id in this.IdsDictionary.Keys)
				{
					if (id >= 0 && (id & 1) == 1)
					{
						yield return id;
					}
				}
			}
		}
		#endregion

		#region Protected Properties

		/// <summary>Gets the ID dictionary of the collection.</summary>
		/// <value>The ID dictionary.</value>
		protected IDictionary<int, Namespace> IdsDictionary { get; } = new SortedList<int, Namespace>();

		/// <summary>Gets the name dictionary of the collection.</summary>
		/// <value>The name dictionary.</value>
		protected IDictionary<string, Namespace> NamesDictionary { get; }
		#endregion

		#region Public Indexers

		/// <summary>Gets the element with the specified key.</summary>
		/// <param name="id">The ID of the namespace.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
		/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
		public Namespace this[int id] => this.IdsDictionary[id];

		/// <summary>Gets the element with the specified key.</summary>
		/// <param name="key">Any valid name for the namespace.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, it will be tried again after HTML decoding the key and normalizing space characters. If that also fails, an exception is thrown.</returns>
		/// <exception cref="ArgumentNullException">The key was null.</exception>
		/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
		public Namespace this[string key] => this.NamesDictionary.TryGetValue(key, out var retval)
			? retval
			: this.NamesDictionary[WikiTextUtilities.DecodeAndNormalize(key)];
		#endregion

		#region Public Methods

		/// <summary>This function can be used to register alternate names that may be useful for coding purposes. It will add the name to its own internal list of names, as well as the namespace's AllNames collection, but not the Aliases collection.</summary>
		/// <param name="name">The name to be added (e.g., "Main").</param>
		/// <param name="id">The ID of the namespace to associate the name with.</param>
		public void AddToNames(string name, int id) => this.AddToNames(name, this.IdsDictionary[id]);

		/// <summary>This function can be used to register alternate names that may be useful for coding purposes. It will add the name to its own internal list of names, as well as the namespace's AllNames collection, but not the Aliases collection.</summary>
		/// <param name="name">The name to be added (e.g., "Main").</param>
		/// <param name="ns">The namespace to associate the name with.</param>
		public void AddToNames(string name, Namespace ns)
		{
			this.NamesDictionary.Add(name.NotNull(nameof(name)), ns.NotNull(nameof(ns)));
			ns.AddName(name);
		}

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="id">The namespace ID to locate in the collection.</param>
		/// <returns>True if the collection contains the relevant namespace.</returns>
		public bool Contains(int id) => this.IdsDictionary.ContainsKey(id);

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to locate in the collection.</param>
		/// <returns>True if the collection contains the relevant namespace.</returns>
		/// <exception cref="ArgumentNullException">The name is null.</exception>
		public bool Contains(string name) =>
			this.NamesDictionary.ContainsKey(name) ||
			this.NamesDictionary.ContainsKey(WikiTextUtilities.DecodeAndNormalize(name));

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Namespace> GetEnumerator() => this.IdsDictionary.Values.GetEnumerator();

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.IdsDictionary.Values.GetEnumerator();

		/// <summary>Returns the namespace associated with the specified ID.</summary>
		/// <param name="id">The namespace ID to search for.</param>
		/// <param name="value">The namespace object, if found; otherwise, null.</param>
		/// <returns>True if the collection contains the desired namespace.</returns>
		public bool TryGetValue(int id, [MaybeNullWhen(false)] out Namespace value) => this.IdsDictionary.TryGetValue(id, out value!);

		/// <summary>Returns the namespace associated with the specified name.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to search for.</param>
		/// <param name="value">The namespace object, if found; otherwise, null.</param>
		/// <returns>True if the collection contains the desired namespace.</returns>
		/// <exception cref="ArgumentNullException">The name is null.</exception>
		public bool TryGetValue(string name, [MaybeNullWhen(false)] out Namespace value) =>
			this.NamesDictionary.TryGetValue(name, out value) ||
			this.NamesDictionary.TryGetValue(WikiTextUtilities.DecodeAndNormalize(name), out value);

		/// <summary>Returns the namespace associated with the specified ID.</summary>
		/// <param name="id">The namespace ID to search for.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public Namespace? ValueOrDefault(int id) => this.IdsDictionary.TryGetValue(id, out var value) ? value : default;

		/// <summary>Returns the namespace associated with the specified name.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to search for.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public Namespace? ValueOrDefault(string? name) => name != null && this.TryGetValue(name, out var value) ? value : default;
		#endregion
	}
}