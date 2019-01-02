namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Unintuitive to refer to it as a 'CsvRowCollection'.")]
	public sealed class CsvFile : IList<CsvRow>
	{
		#region Fields
		private readonly List<CsvRow> rows = new List<CsvRow>();
		private readonly Dictionary<string, int> nameMap = new Dictionary<string, int>();
		private bool hasHeader = false;
		#endregion

		#region Public Properties
		public int Count => this.HasHeader
			? this.rows.Count > 0 ? this.rows.Count - 1 : 0
			: this.rows.Count;

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

		public bool DoubleUpDelimiters { get; set; } = true;

		public char? EscapeCharacter { get; set; } = null;

		public char? FieldDelimiter { get; set; } = '"';

		public char FieldSeparator { get; set; } = ',';

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

		public CsvRow HeaderRow => (this.hasHeader && this.rows.Count > 0) ? this.rows[0] : null;

		public bool IgnoreSurroundingWhiteSpace { get; set; } = true;
		#endregion

		#region Interface Properties
		bool ICollection<CsvRow>.IsReadOnly { get; } = false;
		#endregion

		#region Public Indexers
		public CsvRow this[int index]
		{
			get => this.rows[index];
			set => this.rows[index] = value;
		}
		#endregion

		#region Public Methods
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

		public CsvRow Add(params string[] fields) => this.Add(fields as IEnumerable<string>);

		public CsvRow Add(IEnumerable<string> fields)
		{
			var row = new CsvRow(fields, this.nameMap);
			this.Add(row);
			return row;
		}

		public void Add(CsvRow item)
		{
			ThrowNull(item, nameof(item));
			this.Insert(this.rows.Count, item);
		}

		public void AddColumn() => this.AddColumn(string.Empty);

		public void AddColumn(string name)
		{
			if (this.HasHeader)
			{
				this.nameMap.Add(name, this.nameMap.Count);
				this.HeaderRow.Add(name);
			}
		}

		public void AddHeader(params string[] fieldNames) => this.AddHeader(fieldNames as IEnumerable<string>);

		public void AddHeader(IEnumerable<string> fieldNames)
		{
			if (this.hasHeader && this.rows.Count > 0)
			{
				throw new InvalidOperationException("File already has a header.");
			}

			var header = new CsvRow(new List<string>(fieldNames), this.nameMap);
			this.hasHeader = true;
			this.Insert(0, header); // Automatically triggers a refresh of nameMap.
		}

		public void Clear()
		{
			this.rows.Clear();
			this.ResetHeader();
		}

		public void CopyTo(CsvRow[] array, int arrayIndex) => this.rows.CopyTo(array, arrayIndex);

		public IEnumerator<CsvRow> GetEnumerator() => this.rows.GetEnumerator();

		public void Insert(int index, CsvRow item)
		{
			ThrowNull(item, nameof(item));
			this.rows.Insert(index, item);
			if (index == 0 && this.HasHeader)
			{
				this.ResetHeader();
			}
		}

		public void ReadFile(string fileName)
		{
			using (var fileStream = File.OpenText(fileName))
			{
				this.ReadStream(fileStream);
			}
		}

		public IList<string> ReadRow(StreamReader stream)
		{
			ThrowNull(stream, nameof(stream));
			var row = new List<string>();
			var field = new StringBuilder();
			var insideQuotes = false;
			var endOfLine = false;
			var outsideValue = true;

			while (!endOfLine && !stream.EndOfStream)
			{
				var character = (char)stream.Read();
				if ("\n\r\u2028\u2029".IndexOf(character) != -1)
				{
					if (insideQuotes)
					{
						field.Append(character);
					}
					else
					{
						if (character == '\n' && stream.Peek() == '\r')
						{
							stream.Read();
						}

						row.Add(field.ToString());
						field.Clear();
						endOfLine = true;
					}
				}
				else if (character == this.FieldDelimiter)
				{
					if (!outsideValue && this.DoubleUpDelimiters && stream.Peek() == this.FieldDelimiter)
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
						row.Add(field.ToString());
						field.Clear();
					}
				}
				else if (character == this.EscapeCharacter)
				{
					var newChar = stream.Read();
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
				row.Add(field.ToString());
			}

			return row.Count == 0 ? null : row;
		}

		public void ReadStream(StreamReader stream)
		{
			ThrowNull(stream, nameof(stream));
			do
			{
				var row = this.ReadRow(stream);
				if (row != null)
				{
					this.Add(row);
				}
			}
			while (!stream.EndOfStream);
		}

		public bool Remove(CsvRow item)
		{
			var index = this.rows.IndexOf(item);
			if (index == -1)
			{
				return false;
			}

			this.RemoveAt(index);
			return true;
		}

		public void RemoveAt(int index)
		{
			this.rows.RemoveAt(index);
			if (this.HasHeader && index == 0)
			{
				this.ResetHeader();
			}
		}

		public void WriteFile(string fileName)
		{
			using (var fileStream = File.OpenWrite(fileName))
			using (var writeStream = new StreamWriter(fileStream, Encoding.UTF8))
			{
				this.WriteStream(writeStream);
			}
		}

		public void WriteRow(StreamWriter streamWriter, IEnumerable<string> row) => this.InternalWriteRow(streamWriter, row, 0, this.GetSpecialCharacters());

		public void WriteStream(StreamWriter stream)
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
				this.InternalWriteRow(stream, row, columnCount, specialChars);
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

		private void InternalWriteRow(StreamWriter streamWriter, IEnumerable<string> row, int columnCount, char[] specialChars)
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
					rewriteFields.Add(field.IndexOfAny(specialChars) == -1 ? field : this.RewriteField(field));
					columnNumber++;
				}
			}

			streamWriter.WriteLine(string.Join(this.FieldSeparator.ToString(CultureInfo.InvariantCulture), rewriteFields));
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
			var addDelimiter = false;
			var sb = new StringBuilder();
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