namespace RobinHood70.WikiClasses
{
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using System.Text.RegularExpressions;

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class Parameter
	{
		#region Static Fields
		private static Regex equalsFinder = new Regex(@"(&#x3b|&#61;|\{\{=}}|<nowiki>=</nowiki>)", RegexOptions.IgnoreCase);
		#endregion

		#region Constructors
		public Parameter(string value)
			: this(new TemplateString(), new TemplateString(value), true)
		{
		}

		public Parameter(TemplateString value)
			: this(new TemplateString(), value, true)
		{
		}

		public Parameter(string name, string value)
			: this(new TemplateString(name), new TemplateString(value), name.Length == 0)
		{
		}

		public Parameter(TemplateString name, TemplateString value)
			: this(name, value, name.Value.Length == 0)
		{
		}

		public Parameter(TemplateString name, TemplateString value, bool anonymous)
		{
			this.FullName = name;
			this.FullValue = value;
			this.Anonymous = anonymous;
		}
		#endregion

		#region Public Properties
		public bool Anonymous { get; private set; }

		public TemplateString FullName { get; internal set; }

		public TemplateString FullValue { get; internal set; }

		public string Name
		{
			get => this.FullName.Value;
			set => this.FullName.Value = value;
		}

		public string Value
		{
			get => this.FullValue.Value;
			set => this.FullValue.Value = value;
		}
		#endregion

		#region Private Properties
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by debugger UI")]
		private string DebuggerDisplay
		{
			get
			{
				var retval = this.Value;
				if (retval.Length > 20)
				{
					retval = retval.Substring(0, 17) + "...";
				}

				if (!this.Anonymous)
				{
					return this.Name + "=" + retval;
				}

				return retval;
			}
		}
		#endregion

		#region Public Static Methods
		public static string Escape(string text) => text?.Replace("=", "&#61;");

		public static bool IsNullOrEmpty(Parameter faction) => faction == null || faction.Value.Length == 0;

		public static string Unescape(string text) => equalsFinder.Replace(text, "=");
		#endregion

		#region Public Methods
		public void Anonymize(string nameIfNeeded)
		{
			if (equalsFinder.Replace(this.Value, string.Empty).Contains("="))
			{
				// Can't anonymize because it contains an equals sign, so number it and force number to display instead.
				this.Name = nameIfNeeded;
				this.Anonymous = false;
			}
			else
			{
				this.Anonymous = true;
				this.FullValue.LeadingWhiteSpace = string.Empty;
				this.FullValue.TrailingWhiteSpace = string.Empty;
			}
		}

		public void Build(StringBuilder builder)
		{
			if (!this.Anonymous)
			{
				this.FullName.Build(builder);
				builder.Append('=');
			}

			this.FullValue.Build(builder);
		}

		public Parameter Copy() => new Parameter(this.FullName.Copy(), this.FullValue.Copy(), this.Anonymous);

		public void Escape() => this.Value = Escape(this.Value);

		public void Trim()
		{
			this.FullValue.LeadingWhiteSpace = string.Empty;
			this.FullValue.TrailingWhiteSpace = string.Empty;
			this.FullValue.Value = this.FullValue.Value.Trim();
		}

		public void Unescape() => this.Value = Unescape(this.Value);
		#endregion

		#region Public Override Methods
		public override string ToString()
		{
			if (this.Anonymous)
			{
				return this.FullValue.Build();
			}

			return this.FullName.Build() + "=" + this.FullValue.Build();
		}
		#endregion
	}
}