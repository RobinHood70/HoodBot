namespace RobinHood70.WikiClasses
{
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>Represents a single row in a <see cref="CsvFile"/>.</summary>
	/// <remarks>Once created, the number of values in a row may not be changed.</remarks>
	public class CsvRow : IReadOnlyList<string>, IReadOnlyCollection<string>, IEnumerable<string>
	{
		#region Fields
		private readonly IReadOnlyDictionary<string, int> nameMap;
		private readonly List<string> fields;
		#endregion

		#region Constructors
		internal CsvRow(IEnumerable<string> fields, IReadOnlyDictionary<string, int> nameMap)
		{
			this.fields = fields == null ? new List<string>(nameMap.Count) : new List<string>(fields);
			this.nameMap = nameMap;
			if (nameMap?.Count > this.fields.Count)
			{
				this.fields.Capacity = nameMap.Count;
				this.fields.AddRange(new string[nameMap.Count - this.fields.Count]);
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the number of fields in the row.</summary>
		public int Count => this.fields.Count;
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the field (as a <see cref="string"/>) at the specified index.</summary>
		/// <param name="index">The index of the field.</param>
		/// <returns>The field value, formatted as a <see cref="string"/>.</returns>
		public string this[int index]
		{
			get => this.fields[index];
			set => this.fields[index] = value;
		}

		/// <summary>Gets or sets the <see cref="string"/> with the specified field name.</summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns>The field value, formatted as a <see cref="string"/>.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when there are no columns with the provided field name.</exception>
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

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<string> GetEnumerator() => this.fields.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.fields.GetEnumerator();
		#endregion
	}
}