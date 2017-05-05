namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Design;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "I prefer abstract classes to end in Base.")]
	public abstract class TitleCollectionBase<TTitle> : IList<TTitle>
		where TTitle : IWikiTitle
	{
		#region Fields
		private Dictionary<string, TTitle> dictionary = new Dictionary<string, TTitle>();
		private List<TTitle> items = new List<TTitle>();
		#endregion

		#region Constructors
		protected TitleCollectionBase([ValidatedNotNull] Site site)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
		}
		#endregion

		#region Public Properties
		public int Count => this.items.Count;

		public bool IsReadOnly { get; } = false;

		public Site Site { get; }
		#endregion

		#region Protected Properties
		protected IReadOnlyDictionary<string, TTitle> Dictionary => this.dictionary;

		protected IReadOnlyList<TTitle> Items => this.items;
		#endregion

		#region Public Indexers
		public TTitle this[int index]
		{
			get => this.items[index];
			set
			{
				ThrowNull(value, nameof(value));
				this.items[index] = value;
				this.dictionary[value.Key] = value;
			}
		}

		public virtual TTitle this[string key]
		{
			get => this.dictionary[key];
			set
			{
				var index = this.IndexOf(key);
				if (index < 0)
				{
					this.items.Add(value);
				}
				else
				{
					this.items[index] = value;
				}

				this.dictionary[key] = value;
			}
		}
		#endregion

		#region Public Methods
		public void Add(TTitle item)
		{
			var index = this.IndexOf(item);
			if (index != -1)
			{
				// We don't touch the dictionary here because InsertItem will simply write over top of the existing entry.
				this.items.RemoveAt(index);
			}

			this.InsertItem(this.items.Count, item);
		}

		public void AddRange(IEnumerable<TTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(title);
				}
			}
		}

		public bool Contains(TTitle item) => this.dictionary.ContainsKey(item?.Key);

		public bool Contains(string key) => this.dictionary.ContainsKey(key);

		public void CopyTo(TTitle[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex);

		public IEnumerator<TTitle> GetEnumerator() => this.items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

		public int IndexOf(TTitle item) => this.IndexOf(item?.Key);

		public int IndexOf(string key)
		{
			// ContainsKey is O(1), so check to make sure item exists before iterating the collection.
			if (this.dictionary.ContainsKey(key))
			{
				for (var i = 0; i < this.items.Count; i++)
				{
					if (this[i].Key == key)
					{
						return i;
					}
				}
			}

			return -1;
		}

		public void Insert(int index, TTitle item) => this.InsertItem(index, item);

		public bool Remove(TTitle item) => this.Remove(item?.Key);

		public bool Remove(string key)
		{
			ThrowNull(key, nameof(key));
			var index = this.IndexOf(key);
			if (index == -1)
			{
				return false;
			}

			this.RemoveItem(index);
			return true;
		}

		public void RemoveAt(int index) => this.RemoveItem(index);

		public void Sort() => (this.items as List<IWikiTitle>).Sort(new IWikiTitleComparerKey());

		public void Sort(Comparison<IWikiTitle> comparison) => (this.items as List<IWikiTitle>).Sort(comparison);

		public void Sort(IComparer<IWikiTitle> comparer) => (this.items as List<IWikiTitle>).Sort(comparer);

		public bool TryGetValue(string key, out TTitle value) => this.dictionary.TryGetValue(key, out value);
		#endregion

		#region Public Virtual Methods
		public virtual void Clear()
		{
			this.items.Clear();
			this.dictionary.Clear();
		}
		#endregion

		#region Protected Override Methods
		protected virtual void InsertItem(int index, TTitle item)
		{
			ThrowNull(item, nameof(item));
			if (item.Site != this.Site)
			{
				throw new InvalidOperationException(CurrentCulture(InvalidSite));
			}

			this.dictionary[item.Key] = item;
			this.items.Insert(index, item);
		}

		protected virtual void RemoveItem(int index)
		{
			if (index >= this.items.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			this.dictionary.Remove(this.items[index].Key);
			this.items.RemoveAt(index);
		}
		#endregion
	}
}