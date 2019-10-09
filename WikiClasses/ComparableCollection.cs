namespace RobinHood70.WikiClasses
{
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>A list collection that provides a Comparer.</summary>
	/// <typeparam name="T">The type of list.</typeparam>
	/// <seealso cref="IList{T}" />
	public class ComparableCollection<T> : IList<T>
	{
		#region Fields
		private readonly List<T> list;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ComparableCollection{T}"/> class.</summary>
		/// <param name="comparer">The comparer.</param>
		public ComparableCollection(IEqualityComparer<T> comparer)
		{
			this.list = new List<T>();
			this.Comparer = comparer;
		}

		/// <summary>Initializes a new instance of the <see cref="ComparableCollection{T}"/> class.</summary>
		/// <param name="capacity">The collection capacity.</param>
		/// <param name="comparer">The comparer.</param>
		public ComparableCollection(int capacity, IEqualityComparer<T> comparer)
		{
			this.list = new List<T>(capacity);
			this.Comparer = comparer;
		}

		/// <summary>Initializes a new instance of the <see cref="ComparableCollection{T}"/> class.</summary>
		/// <param name="collection">The collection to initialize from.</param>
		/// <param name="comparer">The comparer.</param>
		public ComparableCollection(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		{
			this.list = new List<T>(collection);
			this.Comparer = comparer;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the comparer for the collection.</summary>
		public IEqualityComparer<T> Comparer { get; }

		/// <summary>Gets the number of elements contained in the collection.</summary>
		public int Count => this.list.Count;

		/// <summary>Gets a value indicating whether the collection is read-only.</summary>
		public bool IsReadOnly { get; } = false;
		#endregion

		#region Indexers

		/// <summary>Gets or sets the item at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <value>The item.</value>
		/// <returns>The item at the specified index.</returns>
		public T this[int index]
		{
			get => this.list[index];

			set => this.list[index] = value;
		}
		#endregion

		#region Public Methods

		/// <summary>Adds an item to the collection.</summary>
		/// <param name="item">The object to add to the collection.</param>
		public void Add(T item) => this.list.Add(item);

		/// <summary>Removes all items from the collection.</summary>
		public void Clear() => this.list.Clear();

		/// <summary>Determines whether this instance contains the object.</summary>
		/// <param name="item">The object to locate in the collection.</param>
		/// <returns><see langword="true"/> if <paramref name="item" /> is found in the collection; otherwise, <see langword="false"/>.</returns>
		public bool Contains(T item) => this.IndexOf(item) >= 0;

		/// <summary>Copies the elements of the collection to an array, starting at a particular array index.</summary>
		/// <param name="array">The one-dimensional array that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(T[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator() => this.list.GetEnumerator();

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.list.GetEnumerator();

		/// <summary>Determines the index of a specific item in the list.</summary>
		/// <param name="item">The object to locate in the list.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
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

		/// <summary>Inserts an item to the list at the specified index.</summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The object to insert into the list.</param>
		public void Insert(int index, T item) => this.list.Insert(index, item);

		/// <summary>Removes the first occurrence of a specific object from the collection.</summary>
		/// <param name="item">The object to remove from the collection.</param>
		/// <returns><see langword="true"/> if <paramref name="item" /> was successfully removed from the collection; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if <paramref name="item" /> is not found in the original collection.</returns>
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

		/// <summary>Removes the list item at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index) => this.list.RemoveAt(index);
		#endregion
	}
}