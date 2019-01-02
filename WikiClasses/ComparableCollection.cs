namespace RobinHood70.WikiClasses
{
	using System.Collections;
	using System.Collections.Generic;

	public class ComparableCollection<T> : IList<T>
	{
		#region Fields
		private readonly List<T> list;
		#endregion

		#region Constructors
		public ComparableCollection(IEqualityComparer<T> comparer)
		{
			this.list = new List<T>();
			this.Comparer = comparer;
		}

		public ComparableCollection(int capacity, IEqualityComparer<T> comparer)
		{
			this.list = new List<T>(capacity);
			this.Comparer = comparer;
		}

		public ComparableCollection(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		{
			this.list = new List<T>(collection);
			this.Comparer = comparer;
		}
		#endregion

		#region Public Properties
		public IEqualityComparer<T> Comparer { get; }

		public int Count => this.list.Count;

		public bool IsReadOnly { get; } = false;
		#endregion

		#region Indexers
		public T this[int index]
		{
			get => this.list[index];

			set => this.list[index] = value;
		}
		#endregion

		#region Public Methods
		public void Add(T item) => this.list.Add(item);

		public void Clear() => this.list.Clear();

		public bool Contains(T item) => this.IndexOf(item) >= 0;

		public void CopyTo(T[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);

		public IEnumerator<T> GetEnumerator() => this.list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.list.GetEnumerator();

		public int IndexOf(T item)
		{
			var comparer = this.Comparer;
			for (var i = 0; i < this.list.Count; i++)
			{
				if (comparer.Equals(item, this.list[i]))
				{
					return i;
				}
			}

			return -1;
		}

		public void Insert(int index, T item) => this.list.Insert(index, item);

		public bool Remove(T item)
		{
			// Behaviour here mirrors that of List<T>, which only removes the first instance of item, not all instances.
			var comparer = this.Comparer;
			foreach (var otherItem in this)
			{
				if (comparer.Equals(item, otherItem))
				{
					return this.list.Remove(otherItem);
				}
			}

			return false;
		}

		public void RemoveAt(int index) => this.list.RemoveAt(index);
		#endregion
	}
}