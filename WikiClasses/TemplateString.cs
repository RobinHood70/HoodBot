namespace RobinHood70.WikiClasses
{
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	public class TemplateString
	{
		#region Constructors
		public TemplateString()
			: this(string.Empty, string.Empty, string.Empty)
		{
		}

		public TemplateString(string value)
			: this(string.Empty, value, string.Empty)
		{
		}

		// Used for Default___Format properties.
		public TemplateString(string leadingWhiteSpace, string trailingWhiteSpace)
			: this(leadingWhiteSpace, null, trailingWhiteSpace)
		{
		}

		public TemplateString(string leadingWhiteSpace, string value, string trailingWhiteSpace)
		{
			this.LeadingWhiteSpace = leadingWhiteSpace;
			this.Value = value;
			this.TrailingWhiteSpace = trailingWhiteSpace;
		}
		#endregion

		#region Public Properties
		public string LeadingWhiteSpace { get; set; }

		public string TrailingWhiteSpace { get; set; }

		public string Value { get; set; }
		#endregion

		#region Public Methods
		public string Build() => this.LeadingWhiteSpace + this.Value + this.TrailingWhiteSpace;

		public void Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder.Append(this.LeadingWhiteSpace);
			builder.Append(this.Value);
			builder.Append(this.TrailingWhiteSpace);
		}

		public TemplateString Copy() => new TemplateString(this.LeadingWhiteSpace, this.Value, this.TrailingWhiteSpace);

		public void CopyFrom(TemplateString source)
		{
			ThrowNull(source, nameof(source));
			this.LeadingWhiteSpace = source.LeadingWhiteSpace;
			this.Value = source.Value;
			this.TrailingWhiteSpace = source.TrailingWhiteSpace;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Value;
		#endregion
	}
}