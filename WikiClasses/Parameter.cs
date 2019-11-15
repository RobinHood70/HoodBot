namespace RobinHood70.WikiClasses
{
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using System.Text.RegularExpressions;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a template parameter.</summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public sealed class Parameter
	{
		#region Static Fields
		private static readonly Regex EqualsFinder = new Regex(@"(&#(x3b|61);|\{\{=}}|<nowiki>=</nowiki>)", RegexOptions.IgnoreCase);
		#endregion

		#region Private Fields
		private bool anonymous;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class.</summary>
		/// <param name="value">The parameter value.</param>
		public Parameter(string? value)
			: this(null, new PaddedString(value), true)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class.</summary>
		/// <param name="value">The full parameter value, including any leading and trailing whitespace.</param>
		public Parameter(PaddedString value)
			: this(null, value, true)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		public Parameter(string? name, string? value)
			: this(name == null ? null : new PaddedString(name), new PaddedString(value), name == null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class.</summary>
		/// <param name="name">The full parameter name, including any leading and trailing whitespace. May be null.</param>
		/// <param name="value">The full parameter value, including any leading and trailing whitespace. May not be null.</param>
		/// <param name="anonymous">Whether the parameter should be treated as anonymous. The <paramref name="name"/> parameter must be non-null for this to take effect.</param>
		public Parameter(string? name, string? value, bool anonymous)
			: this(name == null ? null : new PaddedString(name), new PaddedString(value), anonymous)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class.</summary>
		/// <param name="name">The full parameter name, including any leading and trailing whitespace.</param>
		/// <param name="value">The full parameter value, including any leading and trailing whitespace.</param>
		public Parameter(PaddedString? name, PaddedString value)
			: this(name, value, name == null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class.</summary>
		/// <param name="name">The full parameter name, including any leading and trailing whitespace. May be null.</param>
		/// <param name="value">The full parameter value, including any leading and trailing whitespace. May not be null.</param>
		/// <param name="anonymous">Whether the parameter should be treated as anonymous. The <paramref name="name"/> parameter must be non-null for this to take effect.</param>
		public Parameter(PaddedString? name, PaddedString value, bool anonymous)
		{
			// name can be null; value cannot.
			ThrowNull(value, nameof(value));
			this.FullName = name;
			this.FullValue = value;
			this.anonymous = anonymous;
		}

		/// <summary>Initializes a new instance of the <see cref="Parameter"/> class from an existing one.</summary>
		/// <param name="copy">The instance to copy.</param>
		/// <remarks>This is a deep copy.</remarks>
		private Parameter(Parameter copy)
		{
			ThrowNull(copy, nameof(copy));
			this.FullName = copy.FullName?.Clone();
			this.FullValue = copy.FullValue.Clone();
			this.anonymous = copy.anonymous;
		}

		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether this <see cref="Parameter"/> is anonymous. A parameter with <c><see cref="FullName"/> == <see langword="null"/></c> will always return <see langword="true"/>.</summary>
		/// <value><see langword="true"/> if the parameter is anonymous; otherwise, <see langword="false"/>.</value>
		/// <remarks>
		/// <para>Setting this property directly performs no checks on the data, which may result in unintended consequences. For example, a template parameter of <c>formula=a² + b² = c²</c> which is made anonymous would effectively result in a new parameter named <c>a² + b²</c> with a value of <c>c²</c>. To provide some degree of safety, use the <see cref="Anonymize(string)"/> method instead.</para>
		/// <para>Also note that setting the value to <see langword="false"/> when no name exists will still result in a <see langword="true"/> value being returned until such time as a name is set.</para>
		/// <para>If a name is set and a parameter is anonymous, the parameter continues to be addressable by name. This allows the user to create aliases to specific anonymous parameters for their own internal use.</para>
		/// </remarks>
		public bool Anonymous
		{
			get => this.anonymous || this.FullName == null;
			set => this.anonymous = value;
		}

		/// <summary>Gets or sets the full name.</summary>
		/// <value>The full parameter name, including any leading or trailing whitespace.</value>
		public PaddedString? FullName { get; set; }

		/// <summary>Gets the full value.</summary>
		/// <value>The full parameter value, including any leading or trailing whitespace.</value>
		public PaddedString FullValue { get; }

		/// <summary>Gets or sets the name.</summary>
		/// <value>The parameter name.</value>
		/// <remarks>This is a convenience property, equivalent to <c>FullName.Value</c>.</remarks>
		public string? Name
		{
			get => this.FullName?.Value;
			set
			{
				if (value == null)
				{
					this.FullName = null;
				}
				else
				{
					this.FullName ??= new PaddedString();
					this.FullName.Value = value;
				}
			}
		}

		/// <summary>Gets or sets the value.</summary>
		/// <value>The parameter value.</value>
		/// <remarks>This is a convenience property, equivalent to <c>this.FullValue.Value</c>.</remarks>
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

				return !this.Anonymous ? this.Name + "=" + retval : retval;
			}
		}
		#endregion

		#region Public Static Methods

		/// <summary>Escapes the specified text.</summary>
		/// <param name="text">The text to escape.</param>
		/// <returns>The same as the input value, with <i>all</i> equals signs replaced by the HTML entity <c>&#61;</c>.</returns>
		/// <remarks>This is currently a dumb replace. If you need something more intelligent, for example something that handles equals signs in embedded templates and image links, you will have to implement it yourself.</remarks>
		public static string Escape(string? text) => text?.Replace("=", "&#61;") ?? string.Empty;

		/// <summary>Determines whether the provided parameter is null or has an empty value.</summary>
		/// <param name="item">The item to check.</param>
		/// <returns><see langword="true"/> if the parameter is null or its value is an empty string; otherwise, <see langword="false"/>.</returns>
		public static bool IsNullOrEmpty(Parameter? item) => item == null || item.Value.Length == 0;

		/// <summary>Unescapes the specified text, converting common equivalents of equals signs to an actual equals sign.</summary>
		/// <param name="txt">The text to unescape.</param>
		/// <returns>The same as the input value, with any of the following converted back to an equals sign: <c>&#x3b;</c>, <c>&#61;</c>, <c>{{=}}</c>, <c>&lt;nowiki>=&lt;/nowiki></c>.</returns>
		public static string Unescape(string txt) => EqualsFinder.Replace(txt, "=");
		#endregion

		#region Public Methods

		/// <summary>Anonymizes the specified parameter.</summary>
		/// <param name="nameIfNeeded">The name for the parameter if anonymization is not possible.</param>
		/// <returns><see langword="true"/> if any changes were made to the parameter (whether anonymization or renaming); otherwise <see langword="false"/>.</returns>
		/// <remarks>The name provided will override the current name, if one exists, for consistency of results. For example, if you are anonymizing the "image" parameter to be the first unnamed parameter, but it contains an unescaped equals sign, you can provide "1" as the parameter to ensure that the name "image" is changed, even if a name is still required.</remarks>
		public bool Anonymize(string nameIfNeeded)
		{
			var retval = false;
			if (EqualsFinder.Replace(this.Value, string.Empty).Contains("="))
			{
				// Can't anonymize because it contains an equals sign, so use provided label instead.
				if (this.Name != nameIfNeeded || this.Anonymous)
				{
					this.Rename(nameIfNeeded);
					this.anonymous = false;
					retval = true;
				}
			}
			else
			{
				retval = !this.anonymous || (this.FullValue.LeadingWhiteSpace?.Length + this.FullValue.TrailingWhiteSpace?.Length) > 0;
				this.anonymous = true;
				this.FullValue.LeadingWhiteSpace = string.Empty;
				this.FullValue.TrailingWhiteSpace = string.Empty;
			}

			return retval;
		}

		/// <summary>Builds the parameter text using the specified builder.</summary>
		/// <param name="builder">The builder.</param>
		/// <returns>A copy of the <see cref="StringBuilder"/> passed into the method.</returns>
		public StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			if (!this.Anonymous && this.FullName != null)
			{
				this.FullName.Build(builder);
				builder.Append('=');
			}

			this.FullValue.Build(builder);

			return builder;
		}

		/// <summary>Clones this instance.</summary>
		/// <returns>A deep copy of this instance.</returns>
		public Parameter DeepClone() => new Parameter(this);

		/// <summary>Escapes the parameter value as per the static <see cref="Escape(string)"/> method.</summary>
		public void Escape() => this.Value = Escape(this.Value);

		/// <summary>Renames the parameter to the specified name.</summary>
		/// <param name="newName">The new name.</param>
		/// <returns><see langword="true"/> if the parameter name changed; otherwise <see langword="false"/>.</returns>
		/// <remarks>Renaming a parameter sets <see cref="Anonymous"/> to false unless the new name is identical to the old one.</remarks>
		public bool Rename(string newName)
		{
			if (this.Name != newName)
			{
				this.anonymous = false;
				this.Name = newName;

				return true;
			}

			return false;
		}

		/// <summary>Trims both the name and value of the parameter.</summary>
		public void Trim() => this.Trim(true);

		/// <summary>Trims both the name and value of the parameter.</summary>
		/// <param name="trimText">Whether to trim the text of the name and value or just clear the whitespace properties of each.</param>
		public void Trim(bool trimText)
		{
			this.FullName?.Trim(trimText);
			this.FullValue.Trim(trimText);
		}

		/// <summary>Unescapes the parameter value as per the static <see cref="Unescape(string)"/> method.</summary>
		public void Unescape() => this.Value = Unescape(this.Value);
		#endregion

		#region Public Override Methods

		/// <summary>Converts the parameter to its equivalent wiki text.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Build(new StringBuilder()).ToString();
		#endregion
	}
}