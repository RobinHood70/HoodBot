namespace RobinHood70.WikiClasses
{
	using System.Collections;
	using System.Collections.Generic;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Not intuitive to refer to this as a collection.")]
	public class CsvRow : IList<string>, IReadOnlyList<string>, IReadOnlyCollection<string>, IEnumerable<string>
	{
		#region Fields
		private readonly IReadOnlyDictionary<string, int> nameMap;
		private readonly IList<string> fields;
		#endregion

		#region Constructors
		internal CsvRow(IEnumerable<string> fields, IReadOnlyDictionary<string, int> nameMap)
		{
			if (nameMap?.Count > 0)
			{
				var newFields = new List<string>(nameMap.Count);
				newFields.AddRange(fields);
				while (newFields.Count < nameMap.Count)
				{
					newFields.Add(string.Empty);
				}

				this.fields = newFields;
			}
			else
			{
				this.fields = new List<string>(fields);
			}

			this.nameMap = nameMap;
		}
		#endregion

		#region Public Properties
		public int Count => this.fields.Count;

		public bool IsReadOnly { get; } = false;
		#endregion

		#region Public Indexers
		public string this[int index]
		{
			get => this.fields[index];
			set => this.fields[index] = value;
		}

		public string this[string fieldName]
		{
			get
			{
				if (this.nameMap.TryGetValue(fieldName, out var index))
				{
					return this.fields[index];
				}

				throw new KeyNotFoundException();
			}

			set
			{
				if (!this.nameMap.TryGetValue(fieldName, out var index))
				{
					throw new KeyNotFoundException();
				}

				this.fields[index] = value;
			}
		}
		#endregion

		#region Public Methods
		public void Add(string item) => this.fields.Add(item);

		public void Clear() => this.fields.Clear();

		public bool Contains(string item) => this.fields.Contains(item);

		public void CopyTo(string[] array, int arrayIndex) => this.fields.CopyTo(array, arrayIndex);

		public IEnumerator<string> GetEnumerator() => this.fields.GetEnumerator();

		public int IndexOf(string item) => this.fields.IndexOf(item);

		public void Insert(int index, string item) => this.fields.Insert(index, item);

		public bool Remove(string item) => this.fields.Remove(item);

		public void RemoveAt(int index) => this.fields.RemoveAt(index);

		IEnumerator IEnumerable.GetEnumerator() => this.fields.GetEnumerator();
		#endregion
	}
}
