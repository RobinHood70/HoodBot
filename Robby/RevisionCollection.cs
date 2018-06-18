namespace RobinHood70.Robby
{
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
		public int Count => this.revisions.Count;

		public Revision Current { get; set; }
		#endregion

		#region Public Indexers
		public Revision this[long key] => this.revisions[key];
		#endregion

		#region Public Methods
		public bool Contains(long key) => this.revisions.ContainsKey(key);

		public IEnumerator<Revision> GetEnumerator() => this.revisions.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public bool TryGetValue(long key, out Revision revision) => this.revisions.TryGetValue(key, out revision);
		#endregion

		#region Internal Methods
		internal void Add(Revision revision)
		{
			this.revisions.Add(revision.Id, revision);
			if (revision.Id > (this.Current?.Id ?? 0))
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
