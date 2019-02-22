namespace RobinHood70.WikiClasses
{
	using System;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a string with optional whitespace before and after it, such as a parameter name or value, handling each element separately.</summary>
	/// <remarks>There are no limitations on what is considered to be whitespace. This allows HTML comments and other unvalued text to be stored as needed.</remarks>
	public sealed class TemplateString : IEquatable<TemplateString>
	{
		#region Fields
		private string leadingWhiteSpace;
		private string trailingWhiteSpace;
		private string valueText;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TemplateString"/> class.</summary>
		public TemplateString()
			: this(string.Empty, string.Empty, string.Empty)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TemplateString"/> class.</summary>
		/// <param name="value">The value of the string.</param>
		public TemplateString(string value)
			: this(string.Empty, value, string.Empty)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TemplateString"/> class.</summary>
		/// <param name="leadingWhiteSpace">The leading whitespace.</param>
		/// <param name="trailingWhiteSpace">The trailing whitespace.</param>
		/// <remarks>This constructor is primarily intended for use with the <see cref="Template.DefaultNameFormat"/> and <see cref="Template.DefaultValueFormat"/> properties.</remarks>
		public TemplateString(string leadingWhiteSpace, string trailingWhiteSpace)
			: this(leadingWhiteSpace, null, trailingWhiteSpace)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="TemplateString"/> class.</summary>
		/// <param name="leadingWhiteSpace">The leading whitespace.</param>
		/// <param name="value">The value.</param>
		/// <param name="trailingWhiteSpace">The trailing whitespace.</param>
		public TemplateString(string leadingWhiteSpace, string value, string trailingWhiteSpace)
		{
			this.LeadingWhiteSpace = leadingWhiteSpace;
			this.Value = value;
			this.TrailingWhiteSpace = trailingWhiteSpace;
		}

		/// <summary>Initializes a new instance of the <see cref="TemplateString"/> class from an existing one.</summary>
		/// <param name="copy">The instance to copy.</param>
		/// <remarks>Since all values are strings, deep/shallow does not apply.</remarks>
		private TemplateString(TemplateString copy)
		{
			ThrowNull(copy, nameof(copy));
			this.LeadingWhiteSpace = copy.LeadingWhiteSpace;
			this.Value = copy.Value;
			this.TrailingWhiteSpace = copy.TrailingWhiteSpace;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the leading whitespace surrounding the string.</summary>
		public string LeadingWhiteSpace
		{
			get => this.leadingWhiteSpace;
			set => this.leadingWhiteSpace = value ?? string.Empty;
		}

		/// <summary>Gets or sets the trailing white space.</summary>
		public string TrailingWhiteSpace
		{
			get => this.trailingWhiteSpace;
			set => this.trailingWhiteSpace = value ?? string.Empty;
		}

		/// <summary>Gets or sets the value.</summary>
		public string Value
		{
			get => this.valueText;
			set => this.valueText = value ?? string.Empty;
		}
		#endregion

		#region Public Operators

		/// <summary>Implements the operator ==.</summary>
		/// <param name="string1">The first string.</param>
		/// <param name="string2">The second string.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(TemplateString string1, TemplateString string2) => string1 == null ? string2 == null : string1.Equals(string2);

		/// <summary>Implements the operator !=.</summary>
		/// <param name="string1">The first string.</param>
		/// <param name="string2">The second string.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(TemplateString string1, TemplateString string2) => !(string1 == string2);
		#endregion

		#region Public Methods

		/// <summary>Builds the full text of the value, including surrounding whitespace, into the provided StringBuilder.</summary>
		/// <param name="builder">The StringBuilder to append to.</param>
		public void Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder.Append(this.LeadingWhiteSpace);
			builder.Append(this.Value);
			builder.Append(this.TrailingWhiteSpace);
		}

		/// <summary>Clones this instance.</summary>
		/// <returns>A copy of this instance.</returns>
		/// <remarks>Since all values are strings, deep/shallow does not apply.</remarks>
		public TemplateString Clone() => new TemplateString(this);

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
		/// <returns>
		///   <span class="keyword">
		///     <span class="languageSpecificText">
		///       <span class="cs">true</span>
		///       <span class="vb">True</span>
		///       <span class="cpp">true</span>
		///     </span>
		///   </span>
		///   <span class="nu">
		///     <span class="keyword">true</span> (<span class="keyword">True</span> in Visual Basic)</span> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span>.</returns>
		public bool Equals(TemplateString other) =>
			other != null &&
			this.leadingWhiteSpace == other.leadingWhiteSpace &&
			this.trailingWhiteSpace == other.trailingWhiteSpace &&
			this.valueText == other.valueText;

		/// <summary>Returns a string that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is always the same as the <see cref="Value"/> property.</remarks>
		public override string ToString() => this.Value;

		/// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj) => this.Equals(obj as TemplateString);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode() => CompositeHashCode(this.leadingWhiteSpace, this.trailingWhiteSpace, this.valueText);
		#endregion
	}
}