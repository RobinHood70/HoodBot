namespace RobinHood70.WikiClasses
{
	using System;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a string with optional whitespace before and after it, such as a parameter name or value, handling each element separately.</summary>
	/// <remarks>There are no limitations on what is considered to be whitespace. This allows HTML comments and other unvalued text to be stored as needed.</remarks>
	public sealed class PaddedString : IEquatable<PaddedString>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PaddedString"/> class.</summary>
		public PaddedString()
			: this(string.Empty, string.Empty, string.Empty)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PaddedString"/> class.</summary>
		/// <param name="value">The value of the string.</param>
		public PaddedString(string value)
			: this(string.Empty, value, string.Empty)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PaddedString"/> class.</summary>
		/// <param name="leadingWhiteSpace">The leading whitespace.</param>
		/// <param name="trailingWhiteSpace">The trailing whitespace.</param>
		/// <remarks>This constructor is primarily intended for use with the <see cref="ParameterCollection.DefaultNameFormat"/> and <see cref="ParameterCollection.DefaultValueFormat"/> properties. It initializes only the space properties with no value.</remarks>
		public PaddedString(string leadingWhiteSpace, string trailingWhiteSpace)
			: this(leadingWhiteSpace, string.Empty, trailingWhiteSpace)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PaddedString"/> class.</summary>
		/// <param name="leadingWhiteSpace">The leading whitespace.</param>
		/// <param name="value">The value.</param>
		/// <param name="trailingWhiteSpace">The trailing whitespace.</param>
		public PaddedString(string leadingWhiteSpace, string value, string trailingWhiteSpace)
		{
			this.LeadingWhiteSpace = leadingWhiteSpace;
			this.Value = value;
			this.TrailingWhiteSpace = trailingWhiteSpace;
		}

		/// <summary>Initializes a new instance of the <see cref="PaddedString"/> class from an existing one.</summary>
		/// <param name="copy">The instance to copy.</param>
		/// <remarks>Since all values are strings, deep/shallow does not apply.</remarks>
		private PaddedString(PaddedString copy)
		{
			ThrowNull(copy, nameof(copy));
			this.LeadingWhiteSpace = copy.LeadingWhiteSpace;
			this.TrailingWhiteSpace = copy.TrailingWhiteSpace;
			this.Value = copy.Value;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the length of the string, including surrounding whitespace.</summary>
		/// <value>The length of the string, including surrounding whitespace.</value>
		public int Length => this.LeadingWhiteSpace.Length + this.Value.Length + this.TrailingWhiteSpace.Length;

		/// <summary>Gets or sets the leading whitespace surrounding the string.</summary>
		public string LeadingWhiteSpace { get; set; }

		/// <summary>Gets or sets the trailing white space.</summary>
		public string TrailingWhiteSpace { get; set; }

		/// <summary>Gets or sets the value.</summary>
		public string Value { get; set; }
		#endregion

		#region Implicit Conversion Operators

		/// <summary>Performs an implicit conversion from <see cref="PaddedString"/> to <see cref="string"/>.</summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns>The result of the conversion.</returns>
		public static implicit operator string(PaddedString parameter) => parameter?.Value ?? string.Empty;
		#endregion

		#region Public Operators

		/// <summary>Implements the operator ==.</summary>
		/// <param name="string1">The first string.</param>
		/// <param name="string2">The second string.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(PaddedString? string1, PaddedString? string2) => string1?.Equals(string2) ?? string2 is null;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="string1">The first string.</param>
		/// <param name="string2">The second string.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(PaddedString? string1, PaddedString? string2) => !(string1 == string2);
		#endregion

		#region Public Methods

		/// <summary>Builds the full text of the value, including surrounding whitespace, into the provided StringBuilder.</summary>
		/// <param name="builder">The StringBuilder to append to.</param>
		/// <returns>The original StringBuilder.</returns>
		public StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder.Append(this.LeadingWhiteSpace);
			builder.Append(this.Value);
			builder.Append(this.TrailingWhiteSpace);

			return builder;
		}

		/// <summary>Clones this instance.</summary>
		/// <returns>A copy of this instance.</returns>
		/// <remarks>Since all values are strings, deep/shallow does not apply.</remarks>
		public PaddedString Clone() => new PaddedString(this);

		/// <summary>Copies the format from the source ParameterString.</summary>
		/// <param name="source">The source.</param>
		public void CopyFormatFrom(PaddedString source)
		{
			ThrowNull(source, nameof(source));
			this.LeadingWhiteSpace = source.LeadingWhiteSpace;
			this.TrailingWhiteSpace = source.TrailingWhiteSpace;
		}

		/// <summary>Copies the entire contents from the source ParameterString.</summary>
		/// <param name="source">The source.</param>
		public void CopyFrom(PaddedString source)
		{
			ThrowNull(source, nameof(source));
			this.CopyFormatFrom(source);
			this.Value = source.Value;
		}

		/// <summary>Merges leading and trailing space into the value and clears the space properties.</summary>
		public void Merge()
		{
			this.Value = this.LeadingWhiteSpace + this.Value + this.TrailingWhiteSpace;
			this.LeadingWhiteSpace = string.Empty;
			this.TrailingWhiteSpace = string.Empty;
		}

		/// <summary>Builds the full text of the value, including surrounding whitespace.</summary>
		/// <param name="includeWhiteSpace">Whether to include <see cref="LeadingWhiteSpace"/> and <see cref="TrailingWhiteSpace"/> in the return value.</param>
		/// <returns>The full text of the value, including surrounding whitespace.</returns>
		public string ToString(bool includeWhiteSpace) => includeWhiteSpace ? this.LeadingWhiteSpace + this.Value + this.TrailingWhiteSpace : this.Value;

		/// <summary>Trims all leading and trailing whitespace from the string, including any found in the value itself.</summary>
		/// <seealso cref="Trim(bool)"/>
		public void Trim() => this.Trim(true);

		/// <summary>Trims all leading and trailing whitespace from the string, optionally including any found in the value itself.</summary>
		/// <param name="trimValue">Whether to include the <see cref="Value"/> property in the trim.</param>
		/// <remarks><see cref="LeadingWhiteSpace"/> and <see cref="TrailingWhiteSpace"/> are set to empty strings with this operation. If specified, the value will be trimmed using a standard <see cref="string.Trim()"/> operation.</remarks>
		public void Trim(bool trimValue)
		{
			this.LeadingWhiteSpace = string.Empty;
			this.TrailingWhiteSpace = string.Empty;
			if (trimValue)
			{
				this.Value = this.Value.Trim();
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns><see langword="true"/> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		public bool Equals(PaddedString? other) =>
			other != null &&
			this.LeadingWhiteSpace == other.LeadingWhiteSpace &&
			this.TrailingWhiteSpace == other.TrailingWhiteSpace &&
			this.Value == other.Value;

		/// <summary>Returns a string that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is always the same as the <see cref="Value"/> property.</remarks>
		public override string ToString() => this.Value;

		/// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
		/// <returns><see langword="true"/> if the specified <see cref="object"/> is equal to this instance; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object obj) => this.Equals(obj as PaddedString);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode() => CompositeHashCode(this.LeadingWhiteSpace, this.TrailingWhiteSpace, this.Value);
		#endregion
	}
}