namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	/// <summary>Read-only Namespace dictionary that can be referenced by id and all valid names for the namespace.</summary>
	public class NamespaceCollection : IReadOnlyCollection<Namespace>, IEnumerable<int>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="NamespaceCollection"/> class.</summary>
		/// <param name="namespaces">An enumeration of all namespace objects.</param>
		public NamespaceCollection(IEnumerable<Namespace> namespaces)
		{
			ThrowNull(namespaces, nameof(namespaces));
			foreach (var ns in namespaces)
			{
				this.IdsDictionary.Add(ns.Id, ns);
				foreach (var name in ns.AllNames)
				{
					this.NamesDictionary.Add(name, ns);
				}
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the namespace collection as a collection of their IDs.</summary>
		public IEnumerable<int> AsIds => this.IdsDictionary.Keys;

		/// <summary>Gets the number of namespaces in the collection.</summary>
		public int Count => this.IdsDictionary.Count;
		#endregion

		#region Protected Properties

		/// <summary>Gets the id lookup dictionary of the collection.</summary>
		protected SortedList<int, Namespace> IdsDictionary { get; } = new SortedList<int, Namespace>();

		/// <summary>Gets the name lookup dictionary of the collection.</summary>
		protected Dictionary<string, Namespace> NamesDictionary { get; } = new Dictionary<string, Namespace>();
		#endregion

		#region Public Indexers

		/// <summary>Gets the element with the specified key.</summary>
		/// <param name="id">The id of the namespace.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
		/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
		public Namespace this[int id] => this.IdsDictionary[id];

		/// <summary>Gets the element with the specified key.</summary>
		/// <param name="key">Any valid name for the namespace.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
		/// <exception cref="ArgumentNullException">The key was null.</exception>
		/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
		public Namespace this[string key] => this.NamesDictionary[key];
		#endregion

		#region Public Methods

		/// <summary>This function can be used to register alternate names that may be useful for coding purposes. It will add the name to its own internal list of names, as well as the namespace's AllNames collection, but not the Aliases collection.</summary>
		/// <param name="name">The name to be added (e.g., "Main").</param>
		/// <param name="id">The id of the namespace to associate the name with.</param>
		public void AddToNames(string name, int id) => this.AddToNames(name, this.IdsDictionary[id]);

		/// <summary>This function can be used to register alternate names that may be useful for coding purposes. It will add the name to its own internal list of names, as well as the namespace's AllNames collection, but not the Aliases collection.</summary>
		/// <param name="name">The name to be added (e.g., "Main").</param>
		/// <param name="ns">The namespace to associate the name with.</param>
		public void AddToNames(string name, Namespace ns)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(ns, nameof(ns));
			this.NamesDictionary.Add(name, ns);
			var otherName = ns.AddName(name);
			if (otherName != null)
			{
				this.NamesDictionary.Add(otherName, ns);
			}
		}

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="id">The namespace ID to locate in the collection.</param>
		/// <returns>True if the collection contains the relevant namespace.</returns>
		public bool Contains(int id) => this.IdsDictionary.ContainsKey(id);

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to locate in the collection.</param>
		/// <returns>True if the collection contains the relevant namespace.</returns>
		/// <exception cref="ArgumentNullException">The name is null.</exception>
		public bool Contains(string name) => this.NamesDictionary.ContainsKey(name);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Namespace> GetEnumerator() => this.IdsDictionary.Values.GetEnumerator();

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.IdsDictionary.Values.GetEnumerator();

		/// <summary>Gets the namespace associated with the specified ID.</summary>
		/// <param name="id">The namespace ID to try to get.</param>
		/// <param name="value">The namespace object, if found; otherwise, null.</param>
		/// <returns>True if the collection contains the desired namespace.</returns>
		public bool TryGetValue(int id, out Namespace value) => this.IdsDictionary.TryGetValue(id, out value);

		/// <summary>Gets the namespace associated with the specified ID.</summary>
		/// <param name="name">Any of the names or aliases of the namespace to try to get.</param>
		/// <param name="value">The namespace object, if found; otherwise, null.</param>
		/// <returns>True if the collection contains the desired namespace.</returns>
		/// <exception cref="ArgumentNullException">The name is null.</exception>
		public bool TryGetValue(string name, out Namespace value) => this.NamesDictionary.TryGetValue(name, out value);

		IEnumerator<int> IEnumerable<int>.GetEnumerator()
		{
			foreach (var ns in this)
			{
				yield return ns.Id;
			}
		}
		#endregion
	}
}