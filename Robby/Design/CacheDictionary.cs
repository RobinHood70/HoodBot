namespace RobinHood70.Robby.Design
{
	using System.Collections;
	using System.Collections.Generic;

	// This class is not used internally and is provided only as a potentially useful object for consumers. Although implemented as a read-only dictionary, it has the ability to add and clear items. The full IDictionary spec was not implemented to keep the code simple.

	/// <summary>A FIFO dictionary to allow caching of generic objects. It's intended for use with things like page loading requests and the like.</summary>
	/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Rule for IReadOnlyDictionary is incorrect.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Rule for IReadOnlyDictionary is incorrect.")]
	public class CacheDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		#region Fields
		private Dictionary<TKey, TValue> cache = new Dictionary<TKey, TValue>();
		private TKey[] order;
		private int currentItem = -1;
		private int size = 0;
		#endregion

		#region Constructors
		public CacheDictionary(int cacheSize) => this.Size = cacheSize;
		#endregion

		#region Public Properties
		public int Count => this.cache?.Count ?? 0;

		public IReadOnlyCollection<TKey> Keys => this.cache.Keys;

		public IReadOnlyCollection<TValue> Values => this.cache.Values;

		public int Size
		{
			get => this.size;
			set
			{
				if (value == this.size)
				{
					return;
				}

				var newCache = new Dictionary<TKey, TValue>(value);
				var newOrder = new TKey[value];

				if (this.cache != null)
				{
					var numItems = this.cache.Count < value ? this.cache.Count : value;
					var firstItem = value < this.size ? this.ModOffset(this.OldestItem + this.size - value) : this.OldestItem;
					for (var index = 0; index < numItems; index++)
					{
						var newKey = this.order[this.ModOffset(firstItem + index)];
						var newValue = this.cache[newKey];
						newCache.Add(newKey, newValue);
						newOrder[index] = newKey;
					}

					this.currentItem = numItems - 1;
				}

				this.cache = newCache;
				this.order = newOrder;
				this.size = value;
			}
		}
		#endregion

		#region Private Properties
		private int OldestItem =>
			this.cache.Count == 0 ? -1 :
			this.cache.Count == this.size ? this.ModOffset(this.currentItem + 1) :
			0;
		#endregion

		#region Public Indexers
		public TValue this[TKey key]
		{
			get
			{
				this.cache.TryGetValue(key, out var value);
				return value;
			}
		}
		#endregion

		#region Public Methods
		public void Add(TKey key, TValue value)
		{
			if (this.size == 0)
			{
				return;
			}

			if (this.cache.Count == this.size)
			{
				this.cache.Remove(this.order[this.OldestItem]);
			}

			this.currentItem = this.ModOffset(this.currentItem + 1);
			this.order[this.currentItem] = key;
			this.cache.Add(key, value);
		}

		public void Clear()
		{
			this.cache.Clear();
			this.order = new TKey[this.size];
			this.currentItem = -1;
		}

		public bool ContainsKey(TKey key) => this.cache.ContainsKey(key);

		public bool TryGetValue(TKey key, out TValue value) => this.cache.TryGetValue(key, out value);

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			var firstItem = this.OldestItem;
			for (var index = firstItem; index < firstItem + this.cache.Count; index++)
			{
				var key = this.order[this.ModOffset(index)];
				var value = this.cache[key];
				yield return new KeyValuePair<TKey, TValue>(key, value);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Private Methods
		private int ModOffset(int index) => index % this.size;
		#endregion
	}
}