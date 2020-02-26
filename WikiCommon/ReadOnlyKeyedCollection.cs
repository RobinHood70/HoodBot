namespace RobinHood70.WikiCommon
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>A read-only version of the KeyedCollection class.</summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TItem">The type of the item.</typeparam>
	/// <seealso cref="IEnumerable{TItem}" />
	/// <seealso cref="IReadOnlyList{TItem}" />
	/// <seealso cref="IReadOnlyCollection{TItem}" />
	public class ReadOnlyKeyedCollection<TKey, TItem> : IReadOnlyDictionary<TKey, TItem>, IReadOnlyList<TItem>
		where TKey : notnull
	{
		#region Fields
		private readonly List<TItem> items;
		private readonly Func<TItem, TKey>? keyFunc;
		private Dictionary<TKey, TItem>? dictionary;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ReadOnlyKeyedCollection{TKey, TItem}" /> class that uses the default equality comparer.</summary>
		/// <param name="keyFunc">The function that provides the key for the collection.</param>
		/// <param name="items">The items.</param>
		public ReadOnlyKeyedCollection(Func<TItem, TKey> keyFunc, IEnumerable<TItem> items)
			: this(keyFunc, items, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ReadOnlyKeyedCollection{TKey, TItem}"/> class that uses the specified equality comparer.</summary>
		/// <param name="keyFunc">The function that provides the key for the collection.</param>
		/// <param name="items">The items.</param>
		/// <param name="comparer">The implementation of the <see cref="IEqualityComparer{TKey}"/> generic interface to use when comparing keys, or null to use the default equality comparer for the type of the key, obtained from <see cref="EqualityComparer{TKey}.Default"/>.</param>
		public ReadOnlyKeyedCollection(Func<TItem, TKey> keyFunc, IEnumerable<TItem> items, IEqualityComparer<TKey>? comparer)
			: this(items, comparer)
		{
			ThrowNull(keyFunc, nameof(keyFunc));
			this.keyFunc = keyFunc;
		}

		/// <summary>Initializes a new instance of the <see cref="ReadOnlyKeyedCollection{TKey, TItem}" /> class that uses the default equality comparer.</summary>
		/// <param name="items">The items.</param>
		protected ReadOnlyKeyedCollection(IEnumerable<TItem> items)
			: this(items, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ReadOnlyKeyedCollection{TKey, TItem}"/> class that uses the specified equality comparer.</summary>
		/// <param name="items">The items.</param>
		/// <param name="comparer">The implementation of the <see cref="IEqualityComparer{TKey}"/> generic interface to use when comparing keys, or null to use the default equality comparer for the type of the key, obtained from <see cref="EqualityComparer{TKey}.Default"/>.</param>
		protected ReadOnlyKeyedCollection(IEnumerable<TItem> items, IEqualityComparer<TKey>? comparer)
		{
			this.Comparer = comparer ?? EqualityComparer<TKey>.Default;
			this.items = new List<TItem>(items ?? Array.Empty<TItem>());
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the number of elements in the collection.</summary>
		/// <value>The number of elements in the collection.</value>
		public int Count => this.items.Count;

		/// <summary>Gets the generic equality comparer that is used to determine equality of keys in the collection.</summary>
		/// <value>The implementation of the <see cref="IEqualityComparer{TItem}"/> generic interface that is used to determine equality of keys in the collection.</value>
		public IEqualityComparer<TKey> Comparer { get; }

		/// <summary>Gets an enumerable collection that contains the keys in the collection.</summary>
		/// <value>The keys.</value>
		public IEnumerable<TKey> Keys => this.Dictionary.Keys;

		/// <summary>Gets an enumerable collection that contains the values in the collection.</summary>
		/// <value>The values.</value>
		public IEnumerable<TItem> Values => this.Dictionary.Values;
		#endregion

		#region Private Properties
		private Dictionary<TKey, TItem> Dictionary
		{
			// A manual getter is required, since the call to GetKeyForItem would result in a virtual call from the constructor if it were placed there, which is bad.
			get
			{
				if (this.dictionary == null)
				{
					this.dictionary = new Dictionary<TKey, TItem>(this.items.Count, this.Comparer);
				}

				// This seems to happen during debugging, perhaps when the initial list is not yet fully initialized? Not quite sure.
				if (this.dictionary.Count != this.items.Count)
				{
					this.dictionary.Clear();
					foreach (var item in this.items)
					{
						this.dictionary.Add(this.GetKeyForItem(item), item);
					}
				}

				return this.dictionary;
			}
		}
		#endregion

		#region Public Indexers

		/// <summary>Gets the <typeparamref name="TItem"/> with the specified key.</summary>
		/// <param name="key">The key of the element to get.</param>
		/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
		/// <exception cref="ArgumentNullException">key is null.</exception>
		/// <exception cref="KeyNotFoundException">The property is retrieved and key does not exist in the collection.</exception>
		public TItem this[TKey key] => this.Dictionary[key];

		/// <summary>Gets the <typeparamref name="TItem"/> at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The <typeparamref name="TItem"/>.</returns>
		public TItem this[int index] => this.items[index];
		#endregion

		#region Public Methods

		/// <summary>Determines whether the collection contains an element with the specified key.</summary>
		/// <param name="key">The key to locate in the <see cref="ReadOnlyKeyedCollection{TKey, TItem}"/>.</param>
		/// <returns><see langword="true"/> if the <see cref="ReadOnlyKeyedCollection{TKey, TItem}"/> contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">key is null.</exception>
		public bool Contains(TKey key) => this.Dictionary.ContainsKey(key);

		/// <summary>Determines whether the read-only dictionary contains an element that has the specified key.</summary>
		/// <param name="key">The key to locate.</param>
		/// <returns><see langword="true"/> if the read-only dictionary contains an element that has the specified key; otherwise, <see langword="false"/>.</returns>
		public bool ContainsKey(TKey key) => this.Dictionary.ContainsKey(key);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<TItem> GetEnumerator() => this.items.GetEnumerator();

		IEnumerator<KeyValuePair<TKey, TItem>> IEnumerable<KeyValuePair<TKey, TItem>>.GetEnumerator() => this.Dictionary.GetEnumerator();

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TItem value) => this.Dictionary.TryGetValue(key, out value);

		/// <summary>Returns the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The value associated with the specified key, or <see langword="default"/> if not found.</returns>
		[return: MaybeNull]
		public TItem ValueOrDefault(TKey key) => key != null && this.Dictionary.TryGetValue(key, out var value) ? value : default;
		#endregion

		#region Protected Virtual Methods

		/// <summary>When specified in the constructor or implemented in a derived class, extracts the key from the specified element.</summary>
		/// <param name="item">The element from which to extract the key.</param>
		/// <returns>The key for the specified element.</returns>
		protected virtual TKey GetKeyForItem(TItem item) => this.keyFunc != null ? this.keyFunc(item) : throw new InvalidOperationException(Resources.NoKeyFunction);
		#endregion
	}
}