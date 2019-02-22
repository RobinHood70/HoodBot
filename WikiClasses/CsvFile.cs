namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Reads or writes a CSV file, including tab-separated files and similar formats.</summary>
	/// <remarks>This class is primarily designed to handle entire files at once. <see cref="TextReader"/>- and <see cref="TextWriter"/>-based methods are also available to support streaming and the like.</remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Unintuitive to refer to it as a 'CsvRowCollection'.")]
	public sealed class CsvFile : IList<CsvRow>
	{
		#region Fields
		private readonly List<CsvRow> rows = new List<CsvRow>();
		private readonly Dictionary<string, int> nameMap = new Dictionary<string, int>();
		private bool hasHeader = false;
		#endregion

		#region Public Properties

		/// <summary>Gets the number of rows currently in the file.</summary>
		public int Count => this.HasHeader
			? this.rows.Count > 0 ? this.rows.Count - 1 : 0
			: this.rows.Count;

		/// <summary>Gets the data rows, ignoring the header, if it exists.</summary>
		/// <value>The data rows.</value>
		public IEnumerable<CsvRow> DataRows
		{
			get
			{
				using (var enumerator = this.GetEnumerator())
				{
					if (this.HasHeader)
					{
						enumerator.MoveNext();
					}

					while (enumerator.MoveNext())
					{
						yield return enumerator.Current;
					}
				}
			}
		}

		/// <summary>Gets or sets a value indicating whether to double up the <see cref="FieldDelimiter"/> character if emitted as part of the field value or use the <see cref="EscapeCharacter"/>.</summary>
		/// <value>
		///   <c>true</c> if a delimiter character should be emitted twice; <c>false</c> if it should be escaped instead.</value>
		public bool DoubleUpDelimiters { get; set; } = true;

		/// <summary>Gets or sets the text to emit if a field is present but is an empty string.</summary>
		/// <value>The text to use for empty fields.</value>
		/// <remarks>If this field is null, empty fields will be treated the same as null fields. If it's an empty string, two field delimiters will be emitted with nothing between them. For any other value, that value will be emitted, with field delimiters emitted (or not) as normal.</remarks>
		public string EmptyFieldText { get; set; } = string.Empty;

		/// <summary>Gets or sets the escape character.</summary>
		/// <value>The escape character.</value>
		public char? EscapeCharacter { get; set; } = null;

		/// <summary>Gets or sets the field delimiter.</summary>
		/// <value>The field delimiter. Defaults to a double-quote (<c>"</c>).</value>
		public char? FieldDelimiter { get; set; } = '"';

		/// <summary>Gets or sets the field separator.</summary>
		/// <value>The field separator. Defaults to a comma (<c>,</c>).</value>
		public char FieldSeparator { get; set; } = ',';

		/// <summary>Gets or sets a value indicating whether this instance has a header.</summary>
		/// <value>
		///   <c>true</c> if this instance has a header; otherwise, <c>false</c>. Altering this value changes only how the first row is treated; no data will be added or removed.</value>
		public bool HasHeader
		{
			get => this.hasHeader;
			set
			{
				if (this.hasHeader != value)
				{
					this.hasHeader = value;
					this.ResetHeader();
				}
			}
		}

		/// <summary>Gets the header row.</summary>
		/// <value>The header row. <c>null</c> if there is no header row (<c>HasHeader = false</c> or there are no rows in the file).</value>
		public CsvRow HeaderRow => (this.hasHeader && this.rows.Count > 0) ? this.rows[0] : null;

		/// <summary>Gets or sets a value indicating whether to ignore surrounding white space.</summary>
		/// <value><c>true</c> if leading or trailing whitespace in a field should be ignored when no delimiter is present; otherwise, <c>false</c>.</value>
		/// <remarks>When this is set to <c>true</c>, a row of <c>ABC, DEF</c> is treated the same as <c>ABC,DEF</c>; when false, the second value would be " DEF" rather than "DEF".</remarks>
		public bool IgnoreSurroundingWhiteSpace { get; set; } = true;
		#endregion

		#region Interface Properties
		bool ICollection<CsvRow>.IsReadOnly { get; } = false;
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="CsvRow"/> at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <value>The <see cref="CsvRow"/>.</value>
		/// <returns>The row at the specified index.</returns>
		public CsvRow this[int index]
		{
			get => this.rows[index];
			set => this.rows[index] = value;
		}
		#endregion

		#region Public Methods

		/// <summary>Adds the specified field values.</summary>
		/// <param name="fields">The field values, converted to strings using the default ToString() method for the object.</param>
		/// <returns>The specified values as a <see cref="CsvRow"/>.</returns>
		public CsvRow Add(IEnumerable<object> fields)
		{
			ThrowNull(fields, nameof(fields));
			var list = new List<string>();
			foreach (var item in fields)
			{
				list.Add(item.ToString());
			}

			return this.Add(list);
		}

		/// <summary>Adds the specified field values.</summary>
		/// <param name="fields">The field values.</param>
		/// <returns>The specified values as a <see cref="CsvRow"/>.</returns>
		public CsvRow Add(params string[] fields) => this.Add(fields as IEnumerable<string>);

		/// <summary>Adds the specified field values.</summary>
		/// <param name="fields">The field values.</param>
		/// <returns>The specified values as a <see cref="CsvRow"/>.</returns>
		public CsvRow Add(IEnumerable<string> fields)
		{
			var row = new CsvRow(fields, this.nameMap);
			this.Add(row);
			return row;
		}

		/// <summary>Adds a <see cref="CsvRow"/> directly to the file.</summary>
		/// <param name="item">The row to add to the file.</param>
		public void Add(CsvRow item)
		{
			ThrowNull(item, nameof(item));
			this.Insert(this.rows.Count, item);
		}

		/// <summary>Adds a header with the specified field names.</summary>
		/// <param name="fieldNames">The field names.</param>
		public void AddHeader(params string[] fieldNames) => this.AddHeader(fieldNames as IEnumerable<string>);

		/// <summary>Adds a header with the specified field names.</summary>
		/// <param name="fieldNames">The field names.</param>
		public void AddHeader(IEnumerable<string> fieldNames) => this.AddHeader(new CsvRow(fieldNames, this.nameMap));

		/// <summary>Adds the specified row object as a header.</summary>
		/// <param name="header">The row to add.</param>
		public void AddHeader(CsvRow header)
		{
			if (this.hasHeader && this.rows.Count > 0)
			{
				throw new InvalidOperationException("File already has a header.");
			}

			this.hasHeader = true;
			this.Insert(0, header); // Automatically triggers a refresh of nameMap.
		}

		/// <summary>Removes all items from the file.</summary>
		public void Clear()
		{
			this.rows.Clear();
			this.ResetHeader();
		}

		/// <summary>Copies the rows of the file to an array, starting at a particular array index.</summary>
		/// <param name="array">The one-dimensional array that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(CsvRow[] array, int arrayIndex) => this.rows.CopyTo(array, arrayIndex);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<CsvRow> GetEnumerator() => this.rows.GetEnumerator();

		/// <summary>Inserts a row into the file at the specified index.</summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The row to insert into the file.</param>
		public void Insert(int index, CsvRow item)
		{
			ThrowNull(item, nameof(item));
			this.rows.Insert(index, item);
			if (index == 0 && this.HasHeader)
			{
				this.ResetHeader();
			}
		}

		/// <summary>Reads and parses a CSV file with UTF-8 encoding.</summary>
		/// <param name="fileName">Name of the file.</param>
		public void ReadFile(string fileName) => this.ReadFile(fileName, Encoding.UTF8);

		/// <summary>Reads and parses a CSV file.</summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="encoding">The encoding.</param>
		public void ReadFile(string fileName, Encoding encoding)
		{
			using (var reader = new StreamReader(fileName, encoding))
			{
				this.ReadText(reader);
			}
		}

		/// <summary>Reads a single row from a <see cref="TextReader"/>.</summary>
		/// <param name="reader">The <see cref="TextReader"/> to read from.</param>
		/// <returns>A <see cref="CsvRow"/> with the field values. If names are provided and not enough fields are present to match the name count, the row will be padded with empty strings.</returns>
		public CsvRow ReadRow(TextReader reader)
		{
			ThrowNull(reader, nameof(reader));
			var row = new CsvRow(null, this.nameMap);
			var field = new StringBuilder();
			var fieldNum = 0;
			var insideQuotes = false;
			var endOfLine = false;
			var outsideValue = true;

			while (!endOfLine && reader.Peek() != -1)
			{
				var character = (char)reader.Read();
				if ("\n\r\u2028\u2029".IndexOf(character) != -1)
				{
					if (insideQuotes)
					{
						field.Append(character);
					}
					else
					{
						if (character == '\n' && reader.Peek() == '\r')
						{
							reader.Read();
						}

						row[fieldNum] = field.ToString();
						field.Clear();
						endOfLine = true;
					}
				}
				else if (character == this.FieldDelimiter)
				{
					if (!outsideValue && this.DoubleUpDelimiters && reader.Peek() == this.FieldDelimiter)
					{
						field.Append('"');
					}
					else
					{
						outsideValue = insideQuotes;
						insideQuotes = !insideQuotes;
					}
				}
				else if (character == this.FieldSeparator)
				{
					if (insideQuotes)
					{
						outsideValue = false;
						field.Append(character);
					}
					else
					{
						row[fieldNum] = field.ToString();
						field.Clear();
					}
				}
				else if (character == this.EscapeCharacter)
				{
					var newChar = reader.Read();
					if (newChar == -1)
					{
						outsideValue = true;
						field.Append(character);
					}
					else
					{
						outsideValue = false;
						field.Append(newChar);
					}
				}
				else if (!outsideValue || !this.IgnoreSurroundingWhiteSpace || !char.IsWhiteSpace(character))
				{
					outsideValue = false;
					field.Append(character);
				}
			}

			if (field.Length > 0)
			{
				row[fieldNum] = field.ToString();
				fieldNum++;
			}

			return fieldNum == 0 ? null : row;
		}

		/// <summary>Reads an entire file from a <see cref="TextReader"/> derivative.</summary>
		/// <param name="reader">The <see cref="TextReader"/> to read from.</param>
		public void ReadText(TextReader reader)
		{
			ThrowNull(reader, nameof(reader));
			CsvRow row = null;
			do
			{
				row = this.ReadRow(reader);
				if (row != null)
				{
					this.Add(row);
				}
			}
			while (row != null);
		}

		/// <summary>Removes the first occurrence of a specific row from the file.</summary>
		/// <param name="item">The row to remove from the file.</param>
		/// <returns>
		///   <span class="keyword">
		///     <span class="languageSpecificText">
		///       <span class="cs">true</span>
		///       <span class="vb">True</span>
		///       <span class="cpp">true</span>
		///     </span>
		///   </span>
		///   <span class="nu">
		///     <span class="keyword">true</span> (<span class="keyword">True</span> in Visual Basic)</span> if <paramref name="item" /> was successfully removed from the file; otherwise, <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span>. This method also returns <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span> if <paramref name="item" /> is not found in the original file.
		/// </returns>
		/// <remarks>Rows are searched for first by reference then, if not found, by value. Value searches must match all values, including the number of values. Names, if present, are ignored.</remarks>
		public bool Remove(CsvRow item)
		{
			ThrowNull(item, nameof(item));

			var index = this.rows.IndexOf(item);
			if (index >= 0)
			{
				this.RemoveAt(index);
				return true;
			}

			for (index = 0; index < this.Count; index++)
			{
				var row = this[index];
				if (item.Count == row.Count)
				{
					var match = true;
					for (var i = 0; i < item.Count; i++)
					{
						if (item[i] != row[i])
						{
							match = false;
							break;
						}
					}

					if (match)
					{
						this.RemoveAt(index);
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>Removes the row at the specified index.</summary>
		/// <param name="index">The zero-based index of the row to remove.</param>
		public void RemoveAt(int index)
		{
			this.rows.RemoveAt(index);
			if (this.HasHeader && index == 0)
			{
				this.ResetHeader();
			}
		}

		/// <summary>Writes a CSV file to the specified file with UTF-8 encoding.</summary>
		/// <param name="fileName">The name of the file.</param>
		public void WriteFile(string fileName) => this.WriteFile(fileName, Encoding.UTF8);

		/// <summary>Writes a CSV file to the specified file.</summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="encoding">The encoding.</param>
		public void WriteFile(string fileName, Encoding encoding)
		{
			using (var fileStream = File.OpenWrite(fileName))
			using (var writeStream = new StreamWriter(fileStream, encoding))
			{
				this.WriteText(writeStream);
			}
		}

		/// <summary>Writes a row to the specified <see cref="TextWriter"/> derivative.</summary>
		/// <param name="writer">The <see cref="TextWriter"/> derivative to write to.</param>
		/// <param name="row">The values for the row. This parameter allows for any string enumeration and may thus be either plain data or a <see cref="CsvRow"/>.</param>
		public void WriteRow(TextWriter writer, IEnumerable<string> row) => this.InternalWriteRow(writer, row, 0, this.GetSpecialCharacters());

		/// <summary>Writes the file to the specified <see cref="TextWriter"/> derivative.</summary>
		/// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
		public void WriteText(TextWriter writer)
		{
			// We're allowing rows to be ragged internally, so figure out the highest column count and use that. If a header is specified, that always takes priority. Count could, of course, just be assumed from the first row, but even in a large list, the scan is very quick, so there's no reason not to.
			int columnCount;
			if (this.HasHeader)
			{
				columnCount = this.HeaderRow.Count;
			}
			else
			{
				columnCount = 0;
				foreach (var row in this)
				{
					if (row.Count > columnCount)
					{
						columnCount = row.Count;
					}
				}
			}

			var specialChars = this.GetSpecialCharacters();
			foreach (var row in this)
			{
				this.InternalWriteRow(writer, row, columnCount, specialChars);
			}
		}
		#endregion

		#region Interface Methods
		bool ICollection<CsvRow>.Contains(CsvRow item) => this.rows.Contains(item);

		int IList<CsvRow>.IndexOf(CsvRow item) => this.rows.IndexOf(item);

		IEnumerator IEnumerable.GetEnumerator() => this.rows.GetEnumerator();
		#endregion

		#region Private Methods
		private char[] GetSpecialCharacters()
		{
			var specialList = new List<char> { '\n', '\r', '\u2028', '\u2029', this.FieldSeparator };
			if (this.EscapeCharacter.HasValue)
			{
				specialList.Add(this.EscapeCharacter.Value);
			}

			if (this.FieldDelimiter.HasValue)
			{
				specialList.Add(this.FieldDelimiter.Value);
			}

			return specialList.ToArray();
		}

		private void InternalWriteRow(TextWriter textWriter, IEnumerable<string> row, int columnCount, char[] specialChars)
		{
			var rewriteFields = new List<string>(columnCount);
			if (columnCount == 0)
			{
				columnCount = int.MaxValue;
			}

			var columnNumber = 0;
			foreach (var field in row)
			{
				if (columnNumber < columnCount)
				{
					rewriteFields.Add(
						field == null ? string.Empty :
						(field.Length > 0 && field.IndexOfAny(specialChars) == -1) ? field :
						this.RewriteField(field));
					columnNumber++;
				}
			}

			textWriter.WriteLine(string.Join(this.FieldSeparator.ToString(CultureInfo.InvariantCulture), rewriteFields));
		}

		private void ResetHeader()
		{
			this.nameMap.Clear();
			var header = this.HeaderRow;
			if (header != null)
			{
				foreach (var field in header)
				{
					this.nameMap.Add(field, this.nameMap.Count);
				}
			}
		}

		private string RewriteField(string field)
		{
			var sb = new StringBuilder();
			if (field.Length == 0)
			{
				field = this.EmptyFieldText;
				if (field == null)
				{
					return string.Empty;
				}
			}

			var addDelimiter = field.Length == 0;
			foreach (var character in field)
			{
				if (character == this.FieldDelimiter)
				{
					addDelimiter = true;
					if (this.DoubleUpDelimiters)
					{
						sb.Append(new string(this.FieldDelimiter.Value, 2));
					}
					else
					{
						sb.Append(this.EscapeCharacter + this.FieldDelimiter.Value);
					}
				}
				else if (character == this.EscapeCharacter)
				{
					sb.Append(new string(this.EscapeCharacter.Value, 2));
				}
				else if (character == this.FieldSeparator)
				{
					addDelimiter = this.FieldDelimiter.HasValue;
					if (addDelimiter)
					{
						sb.Append(this.FieldSeparator);
					}
					else
					{
						sb.Append(this.EscapeCharacter + this.FieldSeparator);
					}
				}
				else
				{
					if ("\n\r\u2028\u2029".IndexOf(character) != -1)
					{
						addDelimiter = true;
					}

					sb.Append(character);
				}
			}

			return (addDelimiter && this.FieldDelimiter.HasValue) ? this.FieldDelimiter.Value + sb.ToString() + this.FieldDelimiter.Value : sb.ToString();
		}
		#endregion
	}
}