namespace RobinHood70.WikiClasses
{
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a string with optional whitespace before and after it, such as a parameter name or value, handling each element separately.</summary>
	/// <remarks>There are no limitations on what is considered to be whitespace. This allows HTML comments and other unvalued text to be stored as needed.</remarks>
	public class TemplateString
	{
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
		protected TemplateString(TemplateString copy)
		{
			ThrowNull(copy, nameof(copy));
			this.LeadingWhiteSpace = copy.LeadingWhiteSpace;
			this.Value = copy.Value;
			this.TrailingWhiteSpace = copy.TrailingWhiteSpace;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the leading whitespace surrounding the string.</summary>
		public string LeadingWhiteSpace { get; set; }

		/// <summary>Gets or sets the trailing white space.</summary>
		public string TrailingWhiteSpace { get; set; }

		/// <summary>Gets or sets the value.</summary>
		public string Value { get; set; }
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

		/// <summary>Returns a string that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is always the same as the <see cref="Value"/> property.</remarks>
		public override string ToString() => this.Value;
		#endregion
	}
}