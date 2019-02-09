namespace RobinHood70.Robby
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>This class functions much like a read-only KeyedCollection&lt;long, Revision>, but without the ambiguity of having both an int and long indexer.</summary>
	public class RevisionCollection : IReadOnlyCollection<Revision>
	{
		#region Fields
		private readonly Dictionary<long, Revision> revisions = new Dictionary<long, Revision>();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="RevisionCollection"/> class.</summary>
		protected internal RevisionCollection()
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the number of revisions in the collection.</summary>
		/// <value>The number of revisions in the collection.</value>
		public int Count => this.revisions.Count;

		/// <summary>Gets or sets the current revision.</summary>
		/// <value>The current revision.</value>
		/// <remarks>This will be updated to the highest-numbered revision ID automatically whenever the Add method is called. However, it can then be changed as needed, since the bot cannot be aware of the intent of the user. For example, a deleted revision might normally not be considered current, but if it's the latest revision in a deleted revisions collection, that might be a different case. When retrieved as part of a normal page load, the Page object will always try to set this property to what the wiki's reported latest revision ID.</remarks>
		public Revision Current { get; set; }
		#endregion

		#region Public Indexers

		/// <summary>Gets the <see cref="Revision"/> with the specified ID.</summary>
		/// <param name="id">The revision ID (revid).</param>
		/// <returns>The <see cref="Revision"/> with the specified ID.</returns>
		public Revision this[long id] => this.revisions[id];
		#endregion

		#region Public Methods

		/// <summary>Determines whether the collection contains the specified revision ID.</summary>
		/// <param name="id">The revision ID.</param>
		/// <returns><c>true</c> if the collection contains the specified revision ID; otherwise, <c>false</c>.</returns>
		public bool Contains(long id) => this.revisions.ContainsKey(id);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Revision> GetEnumerator() => this.revisions.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the revision associated with the specified ID.</summary>
		/// <param name="id">The ID of the revision to get.</param>
		/// <param name="value">When this method returns, contains the revision with the specified ID, if found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains a revision with the specified ID; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="id" /> is <see langword="null" />.</exception>
		public bool TryGetValue(long id, out Revision value) => this.revisions.TryGetValue(id, out value);
		#endregion

		#region Internal Methods
		internal void Add(Revision revision)
		{
			this.revisions[revision.Id] = revision;
			if (this.Current == null || revision.Id > this.Current.Id)
			{
				this.Current = revision;
			}
		}

		internal void Clear()
		{
			this.Current = null;
			this.revisions.Clear();
		}
		#endregion
	}
}
