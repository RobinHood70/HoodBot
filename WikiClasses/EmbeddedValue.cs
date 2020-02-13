namespace RobinHood70.WikiClasses
{
	using System;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a string with optional text before and after it, such as a value surrounded by whitespace or delimiters.</summary>
	public sealed class EmbeddedValue : IEquatable<EmbeddedValue>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="EmbeddedValue"/> class.</summary>
		public EmbeddedValue()
			: this(string.Empty, string.Empty, string.Empty)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="EmbeddedValue"/> class.</summary>
		/// <param name="value">The value of the string.</param>
		public EmbeddedValue(string? value)
			: this(string.Empty, value, string.Empty)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="EmbeddedValue"/> class.</summary>
		/// <param name="before">The text before the value.</param>
		/// <param name="after">The text after the value.</param>
		/// <remarks>This constructor is primarily intended for use with the <see cref="ParameterCollection.DefaultNameFormat"/> and <see cref="ParameterCollection.DefaultValueFormat"/> properties. It initializes only the space properties with no value.</remarks>
		public EmbeddedValue(string before, string after)
			: this(before, string.Empty, after)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="EmbeddedValue"/> class.</summary>
		/// <param name="before">The text before the value.</param>
		/// <param name="value">The value.</param>
		/// <param name="after">The text after the value.</param>
		public EmbeddedValue(string before, string? value, string after)
		{
			this.Before = before;
			this.Value = value ?? string.Empty;
			this.After = after;
		}

		/// <summary>Initializes a new instance of the <see cref="EmbeddedValue"/> class from an existing one.</summary>
		/// <param name="copy">The instance to copy.</param>
		/// <remarks>Since all values are strings, deep/shallow does not apply.</remarks>
		private EmbeddedValue(EmbeddedValue copy)
		{
			ThrowNull(copy, nameof(copy));
			this.Before = copy.Before;
			this.After = copy.After;
			this.Value = copy.Value;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the length of the string, including surrounding whitespace.</summary>
		/// <value>The length of the string, including surrounding whitespace.</value>
		public int Length => this.Before.Length + this.Value.Length + this.After.Length;

		/// <summary>Gets or sets the text before the value.</summary>
		public string Before { get; set; }

		/// <summary>Gets or sets the text after the value.</summary>
		public string After { get; set; }

		/// <summary>Gets or sets the value.</summary>
		public string Value { get; set; }
		#endregion

		#region Public Operators

		/// <summary>Implements the operator ==.</summary>
		/// <param name="string1">The first string.</param>
		/// <param name="string2">The second string.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(EmbeddedValue? string1, EmbeddedValue? string2) => string1?.Equals(string2) ?? string2 is null;

		/// <summary>Implements the operator !=.</summary>
		/// <param name="string1">The first string.</param>
		/// <param name="string2">The second string.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(EmbeddedValue? string1, EmbeddedValue? string2) => !(string1 == string2);
		#endregion

		#region Public Methods

		/// <summary>Builds the full text of the value, including surrounding whitespace, into the provided StringBuilder.</summary>
		/// <param name="builder">The StringBuilder to append to.</param>
		/// <returns>The original StringBuilder.</returns>
		public StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			return builder
				.Append(this.Before)
				.Append(this.Value)
				.Append(this.After);
		}

		/// <summary>Clones this instance.</summary>
		/// <returns>A copy of this instance.</returns>
		/// <remarks>Since all values are strings, deep/shallow does not apply.</remarks>
		public EmbeddedValue Clone() => new EmbeddedValue(this);

		/// <summary>Copies the entire contents from the source EmbeddedValue.</summary>
		/// <param name="source">The source.</param>
		public void CopyFrom(EmbeddedValue source)
		{
			ThrowNull(source, nameof(source));
			this.CopySurroundingText(source);
			this.Value = source.Value;
		}

		/// <summary>Copies the surrounding text, but not the value, from the source EmbeddedValue.</summary>
		/// <param name="source">The source.</param>
		public void CopySurroundingText(EmbeddedValue source)
		{
			ThrowNull(source, nameof(source));
			this.Before = source.Before;
			this.After = source.After;
		}

		/// <summary>Merges the surrounding text into the value, then clears surrounding text.</summary>
		public void Merge()
		{
			this.Value = this.Before + this.Value + this.After;
			this.Before = string.Empty;
			this.After = string.Empty;
		}

		/// <summary>Trims all leading and text after the value from the string, including any found in the value itself.</summary>
		/// <seealso cref="Trim(bool)"/>
		public void Trim() => this.Trim(true);

		/// <summary>Trims all leading and text after the value from the string, optionally including any found in the value itself.</summary>
		/// <param name="trimValue">Whether to include the <see cref="Value"/> property in the trim.</param>
		/// <remarks><see cref="Before"/> and <see cref="After"/> are set to empty strings with this operation. If specified, the value will be trimmed using a standard <see cref="string.Trim()"/> operation.</remarks>
		public void Trim(bool trimValue)
		{
			this.Before = string.Empty;
			this.After = string.Empty;
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
		public bool Equals(EmbeddedValue? other) =>
			other != null &&
			this.Before == other.Before &&
			this.After == other.After &&
			this.Value == other.Value;

		/// <summary>Returns a string that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is always the same as the <see cref="Value"/> property.</remarks>
		public override string ToString() => this.Before + this.Value + this.After;

		/// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
		/// <returns><see langword="true"/> if the specified <see cref="object"/> is equal to this instance; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object? obj) => this.Equals(obj as EmbeddedValue);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode() => CompositeHashCode(this.Before, this.After, this.Value);
		#endregion
	}
}