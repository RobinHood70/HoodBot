namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;
	using static RobinHood70.WikiClasses.Properties.Resources;

	/// <summary>Represents a template as a name and collection of <see cref="Parameter"/>s.</summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Template is a more meaningful name.")]
	public class Template : IList<Parameter>
	{
		#region Fields
		private readonly List<Parameter> parameters = new List<Parameter>();
		private ComparableCollection<string> order;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Template"/> class with no name or parameters.</summary>
		public Template()
			: this(string.Empty, StringComparer.Ordinal, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Template"/> class. The name and parameters will be parsed from the provided text.</summary>
		/// <param name="templateText">The full text of the template.</param>
		/// <remarks>The text can optionally include opening and closing braces, but these are not required except in the rare case where the template has two or more braces at both the start and the end which are not the enclosing braces (e.g., <c>{{{{Template to provide name}}|param={{{param|}}}}}</c>). In any other case, no braces are required, meaning that a new template can be created by specifying only the template name, if required.</remarks>
		public Template(string templateText)
			: this(templateText, StringComparer.Ordinal, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Template"/> class. The name and parameters will be parsed from the provided text.</summary>
		/// <param name="text">The full text of the template.</param>
		/// <param name="caseInsensitive">Whether parameter names should be treated case-insensitively.</param>
		/// <param name="ignoreWhiteSpaceRules">Whether MediaWiki whitespace rules should be ignored. If true, all surrounding whitespace will be included in the parameter names and values.</param>
		/// <remarks>The text can optionally include opening and closing braces, but these are not required except in the rare case where the template has two or more braces at both the start and the end which are not the enclosing braces (e.g., <c>{{{{Template to provide name}}|param={{{param|}}}}}</c>). In any other case, no braces are required, meaning that a new template can be created by specifying only the template name, if required.</remarks>
		public Template(string text, bool caseInsensitive, bool ignoreWhiteSpaceRules)
			: this(text, caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, ignoreWhiteSpaceRules)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Template"/> class. The name and parameters will be parsed from the provided text.</summary>
		/// <param name="comparer">The <see cref="StringComparer"/> to use for parameter names.</param>
		/// <remarks>The text can optionally include opening and closing braces, but these are not required except in the rare case where the template has two or more braces at both the start and the end which are not the enclosing braces (e.g., <c>{{{{Template to provide name}}|param={{{param|}}}}}</c>). In any other case, no braces are required, meaning that a new template can be created by specifying only the template name, if required.</remarks>
		public Template(StringComparer comparer)
			: this(string.Empty, comparer, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Template"/> class. The name and parameters will be parsed from the provided text.</summary>
		/// <param name="text">The full text of the template.</param>
		/// <param name="comparer">The <see cref="StringComparer"/> to use for parameter names.</param>
		/// <param name="ignoreWhiteSpaceRules">Whether MediaWiki whitespace rules should be ignored when parsing the template text. If true, all surrounding whitespace will be included in the parameter names and values.</param>
		/// <remarks>The text can optionally include opening and closing braces, but these are not required except in the rare case where the template has two or more braces at both the start and the end which are not the enclosing braces (e.g., <c>{{{{Template to provide name}}|param={{{param|}}}}}</c>). In any other case, no braces are required, meaning that a new template can be created by specifying only the template name, if required.</remarks>
		public Template(string text, StringComparer comparer, bool ignoreWhiteSpaceRules)
		{
			ThrowNull(text, nameof(text));
			ThrowNull(comparer, nameof(comparer));
			this.Comparer = comparer;
			text = text.Trim();
			if (text.StartsWith("{{", StringComparison.Ordinal) && text.EndsWith("}}", StringComparison.Ordinal))
			{
				text = text.Substring(2, text.Length - 4);
			}

			if (text.Length != 0)
			{
				var parser = new TemplateParser(text);
				parser.ParseIntoTemplate(this, ignoreWhiteSpaceRules);
			}
			else
			{
				this.FullName = new TemplateString();
			}
		}

		/// <summary>Initializes a new instance of the <see cref="Template"/> class from an existing one.</summary>
		/// <param name="copy">The instance to copy.</param>
		/// <remarks>This is a deep copy.</remarks>
		protected Template(Template copy)
		{
			ThrowNull(copy, nameof(copy));
			this.Comparer = copy.Comparer;
			this.CopyLast = copy.CopyLast;
			this.FullName = copy.FullName.Clone();
			this.DefaultNameFormat = this.DefaultNameFormat.Clone();
			this.DefaultValueFormat = this.DefaultValueFormat.Clone();
			foreach (var param in copy)
			{
				this.parameters.Add(param.DeepClone());
			}
		}

		#endregion

		#region Public Properties

		/// <summary>Gets the parameter collection filtered to anonymous parameters only.</summary>
		public IEnumerable<Parameter> AnonymousOnly
		{
			get
			{
				foreach (var param in this)
				{
					if (param.Anonymous)
					{
						yield return param;
					}
				}
			}
		}

		/// <summary>Gets the template's <see cref="StringComparer"/>.</summary>
		public StringComparer Comparer { get; }

		/// <summary>Gets or sets a value indicating whether to copy the format of the final parameter when adding new ones.</summary>
		/// <value><c>true</c> if the last parameter's format should be copied; <c>false</c> to use <see cref="DefaultNameFormat"/> and <see cref="DefaultValueFormat"/>.</value>
		/// <remarks>If set to <c>true</c> and there are no parameters in the template, the default formats will be used.</remarks>
		public bool CopyLast { get; set; } = true;

		/// <summary>Gets the number of parameters in this template.</summary>
		public int Count => this.parameters.Count;

		/// <summary>Gets or sets the default format to use for the whitespace surrounding the Name property of any new parameters.</summary>
		/// <remarks>Only the whitespace properties are used for formatting; the Value property is ignored.</remarks>
		/// <seealso cref="CopyLast"/>
		public TemplateString DefaultNameFormat { get; set; } = new TemplateString();

		/// <summary>Gets or sets the default format to use for the whitespace surrounding the Value property of any new parameters.</summary>
		/// <remarks>Only the whitespace properties are used for formatting; the Value property is ignored.</remarks>
		/// <seealso cref="CopyLast"/>
		public TemplateString DefaultValueFormat { get; set; } = new TemplateString();

		/// <summary>Gets or sets the full name.</summary>
		/// <value>The full name.</value>
		public TemplateString FullName { get; set; } = new TemplateString();

		/// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</summary>
		public bool IsReadOnly { get; } = false;

		/// <summary>Gets or sets the name.</summary>
		/// <value>The name.</value>
		/// <remarks>This is a convenience property, equivalent to <c>FullName.Value</c>.</remarks>
		public string Name
		{
			get => this.FullName.Value;
			set => this.FullName.Value = value;
		}

		/// <summary>Gets or sets the trailing whitespace.</summary>
		/// <remarks>This represents any trailing whitespace before the final <c>}}</c>. This is not a discrete value of its own, instead mapping to either <c>FullName.TrailingWhiteSpace</c> or the last parameter's <c>TrailingWhiteSpace</c> property, as appropriate.</remarks>
		public string TrailingWhiteSpace
		{
			get => this.Count == 0 ? this.FullName.TrailingWhiteSpace : this[this.Count - 1].FullValue.TrailingWhiteSpace;
			set
			{
				if (this.Count == 0)
				{
					this.FullName.TrailingWhiteSpace = value;
				}
				else
				{
					this[this.Count - 1].FullValue.TrailingWhiteSpace = value;
				}
			}
		}
		#endregion

		#region Private Properties
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by debugger UI")]
		private string DebuggerDisplay => Invariant($"{this.Name}: Count = {this.Count}");
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="Parameter"/> at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="Parameter"/>.</returns>
		public Parameter this[int index]
		{
			get => this.parameters[index];

			set => this.parameters[index] = value;
		}

		/// <summary>Gets the <see cref="Parameter"/> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The specified <see cref="Parameter"/> or <c>null</c> if the key was not found.</returns>
		/// <remarks>Numeric keys will match either a named parameter or an anonymous parameter in the specified position. Conflicts will be resolved following the same rules that MediaWiki templates use (i.e., last match wins, not first). The template's <see cref="Comparer"/> property will be used to determine if parameter names are a match.</remarks>
		public Parameter this[string key]
		{
			get
			{
				// We have to search the entire collection, rather than bailing out at the first match, because parameters could be duplicated or have anonymous vs. numbered conflicts.
				Parameter match = null;
				var anon = 0;
				foreach (var param in this)
				{
					string name;
					if (param.Anonymous)
					{
						anon++;
						name = anon.ToStringInvariant();
					}
					else
					{
						name = param.Name;
					}

					if (this.Comparer.Compare(key, name) == 0)
					{
						match = param;
					}
				}

				return match;
			}
		}
		#endregion

		#region Public Static Methods

		/// <summary>Returns a Regex that searches for all template calls with the specified name.</summary>
		/// <param name="name">The name to search for.</param>
		/// <returns>A Regex that searches for all template calls with the specified name.</returns>
		/// <remarks>The Regex will be set to timeout after 10 seconds. This ensures that malformed templates will not lock the process.</remarks>
		public static Regex Find(string name) => Find(null, name, null);

		/// <summary>Returns a Regex that searches for all template calls with the specified names.</summary>
		/// <param name="names">The names to search for.</param>
		/// <returns>A Regex that searches for all template calls with the specified names.</returns>
		/// <remarks>The Regex will be set to timeout after 10 seconds. This ensures that malformed templates will not lock the process.</remarks>
		public static Regex Find(IEnumerable<string> names) => FindRaw(null, RegexName(names), null, RegexOptions.None, 10);

		/// <summary>Returns a Regex that searches for all template calls with the specified name, optionally including text before and after the template call.</summary>
		/// <param name="regexBefore">A Regex fragment to search for before the template call.</param>
		/// <param name="name">The name to search for.</param>
		/// <param name="regexAfter">A Regex fragment to search for after the template call.</param>
		/// <returns>A Regex that searches for all template calls with the specified name.</returns>
		/// <remarks>The Regex will be set to timeout after 10 seconds. This ensures that malformed templates will not lock the process.</remarks>
		public static Regex Find(string regexBefore, string name, string regexAfter) => Find(regexBefore, name, regexAfter, RegexOptions.None, 10);

		/// <summary>Returns a Regex that searches for all template calls with the specified name, optionally including text before and after the template call.</summary>
		/// <param name="regexBefore">A Regex fragment to search for before the template call.</param>
		/// <param name="name">The name to search for.</param>
		/// <param name="regexAfter">A Regex fragment to search for after the template call.</param>
		/// <param name="options">The <see cref="RegexOptions"/> to create the Regex with.</param>
		/// <param name="findTimeout">The timeout to be applied to the Regex.</param>
		/// <returns>A Regex that searches for all template calls with the specified name.</returns>
		public static Regex Find(string regexBefore, string name, string regexAfter, RegexOptions options, int findTimeout) => FindRaw(regexBefore, RegexName(name), regexAfter, options, findTimeout);

		/// <summary>Returns a Regex that searches for all template calls with the specified names, optionally including text before and after the template call.</summary>
		/// <param name="regexBefore">A Regex fragment to search for before the template call.</param>
		/// <param name="names">The names to search for.</param>
		/// <param name="regexAfter">A Regex fragment to search for after the template call.</param>
		/// <returns>A Regex that searches for all template calls with the specified names.</returns>
		/// <remarks>The Regex will be set to timeout after 10 seconds. This ensures that malformed templates will not lock the process.</remarks>
		public static Regex Find(string regexBefore, IEnumerable<string> names, string regexAfter) => Find(regexBefore, names, regexAfter, RegexOptions.None, 10);

		/// <summary>Returns a Regex that searches for all template calls with the specified names, optionally including text before and after the template call.</summary>
		/// <param name="regexBefore">A Regex fragment to search for before the template call.</param>
		/// <param name="names">The names to search for.</param>
		/// <param name="regexAfter">A Regex fragment to search for after the template call.</param>
		/// <param name="options">The <see cref="RegexOptions"/> to create the Regex with.</param>
		/// <param name="findTimeout">The timeout to be applied to the Regex.</param>
		/// <returns>A Regex that searches for all template calls with the specified names.</returns>
		public static Regex Find(string regexBefore, IEnumerable<string> names, string regexAfter, RegexOptions options, int findTimeout) => FindRaw(regexBefore, RegexName(names), regexAfter, options, findTimeout);

		/// <summary>Returns a Regex that searches for all template calls with the specified Regex-based names, optionally including text before and after the template call.</summary>
		/// <param name="regexBefore">A Regex fragment to search for before the template call.</param>
		/// <param name="regexNames">The names to search for.</param>
		/// <param name="regexAfter">A Regex fragment to search for after the template call.</param>
		/// <param name="options">The <see cref="RegexOptions"/> to create the Regex with.</param>
		/// <param name="findTimeout">The timeout to be applied to the Regex.</param>
		/// <returns>A Regex that searches for all template calls with the specified names.</returns>
		public static Regex FindRaw(string regexBefore, string regexNames, string regexAfter, RegexOptions options, int findTimeout) => new Regex(InternalRegexText(regexBefore, regexNames, regexAfter), options, TimeSpan.FromSeconds(findTimeout));

		/// <summary>Finds the first template with the specified name, within the specified text, and returns it as a <see cref="Template"/> object.</summary>
		/// <param name="name">The name to search for.</param>
		/// <param name="text">The text to search in.</param>
		/// <returns>A <see cref="Template"/> object populated with the first template found, or null if no matching template was found.</returns>
		public static Template FindTemplate(string name, string text)
		{
			var finder = Find(name).Match(text);
			if (finder.Success)
			{
				return new Template(finder.Value);
			}

			return null;
		}

		/// <summary>Finds the first template that matches the provided Regex, within the specified text, and returns it as a <see cref="Template"/> object.</summary>
		/// <param name="finder">The Regex pattern to find the template.</param>
		/// <param name="text">The text to search in.</param>
		/// <returns>A <see cref="Template"/> object populated with the first template found, or null if no matching template was found.</returns>
		public static Template FindTemplate(Regex finder, string text)
		{
			ThrowNull(finder, nameof(finder));
			var match = finder.Match(text);
			if (match.Success)
			{
				return new Template(match.Value);
			}

			return null;
		}
		#endregion

		#region Public Methods

		/// <summary>Adds a <see cref="Parameter"/> to the <see cref="Template"/>.</summary>
		/// <param name="item">The <see cref="Parameter"/> to add to the <see cref="Template"/>.</param>
		public void Add(Parameter item) => this.parameters.Add(item);

		/// <summary>Adds a parameter with the specified name and value to the <see cref="Template"/>.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> that was added.</returns>
		public Parameter Add(string name, string value)
		{
			var parameter = this.CreateParameter(name, value);
			this.Add(parameter);

			return parameter;
		}

		/// <summary>Adds a <see cref="Parameter"/> to the <see cref="Template"/> after the given name, or at the beginning if the name was not found.</summary>
		/// <param name="afterName">The parameter name to search for.</param>
		/// <param name="item">The <see cref="Parameter"/> to add to the <see cref="Template"/>.</param>
		public void AddAfter(string afterName, Parameter item)
		{
			ThrowNull(item, nameof(item));
			var offset = this.IndexOf(this[afterName]);
			if (offset == -1)
			{
				this.Add(item);
			}
			else
			{
				this.Insert(offset + 1, item);
			}
		}

		/// <summary>Adds a parameter with the specified name and value to the <see cref="Template"/> after the given name, or at the end if the name was not found.</summary>
		/// <param name="afterName">The parameter name to search for.</param>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> that was added.</returns>
		public Parameter AddAfter(string afterName, string name, string value)
		{
			var item = this.CreateParameter(name, value);
			var offset = this.IndexOf(this[afterName]);
			if (offset == -1)
			{
				this.Add(item);
			}
			else
			{
				this.Insert(offset + 1, item);
			}

			return item;
		}

		/// <summary>Adds an anonymous parameter with the specified value to the template.</summary>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> object that was added.</returns>
		public Parameter AddAnonymous(string value)
		{
			var p = new Parameter(value);
			this.Add(p);
			return p;
		}

		/// <summary>Adds a <see cref="Parameter"/> to the <see cref="Template"/> before the given name, or at the beginning if the name was not found.</summary>
		/// <param name="beforeName">The parameter name to search for.</param>
		/// <param name="item">The <see cref="Parameter"/> to add to the <see cref="Template"/>.</param>
		public void AddBefore(string beforeName, Parameter item)
		{
			var offset = this.IndexOf(this[beforeName]);
			if (offset == -1)
			{
				offset = 0;
			}

			this.Insert(offset, item);
		}

		/// <summary>Adds a parameter with the specified name and value to the <see cref="Template"/> after the given name, or at the beginning if the name was not found.</summary>
		/// <param name="beforeName">The parameter name to search for.</param>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> that was added.</returns>
		public Parameter AddBefore(string beforeName, string name, string value)
		{
			var item = this.CreateParameter(name, value);
			var offset = this.IndexOf(this[beforeName]);
			if (offset == -1)
			{
				offset = 0;
			}

			this.Insert(offset, item);

			return item;
		}

		/// <summary>Adds a parameter to the template if a parameter with that name is blank or does not exist.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter AddIfBlank(string name, string value)
		{
			var param = this[name];
			if (param == null)
			{
				param = this.Add(name, value);
			}
			else if (param.Value.Length == 0)
			{
				param.Value = value;
			}

			return param;
		}

		/// <summary>Adds a parameter to the template if a parameter with that name does not already exist.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter AddIfNotPresent(string name, string value) => this[name] ?? this.Add(name, value);

		/// <summary>Adds a parameter with the specified name and value, or changes the value if the parameter already exists.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter AddOrChange(string name, string value) => this.AddOrChange(name, value, false);

		/// <summary>Adds a parameter with the specified name and value, or changes the value if the parameter already exists.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter AddOrChange(string name, int value) => this.AddOrChange(name, value.ToString(CultureInfo.InvariantCulture));

		/// <summary>Adds a parameter with the specified name and value, or changes the value if the parameter already exists.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="onlyChangeIfBlank">Whether to change the parameter value only if the current value is blank or does not exist.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter AddOrChange(string name, string value, bool onlyChangeIfBlank)
		{
			var parameter = this.AddIfNotPresent(name, value);
			if (!onlyChangeIfBlank || string.IsNullOrWhiteSpace(parameter.Value))
			{
				parameter.Value = value;
			}

			return parameter;
		}

		/// <summary>Removes the parameter name, making it into an anonymous parameter, or a numbered parameter if anonymization is not possible.</summary>
		/// <param name="name">The current parameter name.</param>
		/// <param name="position">The anonymous position the parameter should end up in.</param>
		/// <returns><c>true</c> if the parameter was successfully anonymized; otherwise <c>false</c>.</returns>
		/// <remarks>If the parameter cannot be anonymized due to an equals sign in the current value, the parameter name will be changed to the position number if it's not already.</remarks>
		public bool Anonymize(string name, int position) => this.Anonymize(name, position, position.ToStringInvariant());

		/// <summary>Removes the parameter name, making it into an anonymous parameter, or changing the name of the parameter to the specified label if anonymization is not possible.</summary>
		/// <param name="name">The current parameter name.</param>
		/// <param name="position">The anonymous position the parameter should end up in.</param>
		/// <param name="label">The name to change the parameter to if anonymization is not possible.</param>
		/// <returns><c>true</c> if the parameter was successfully anonymized; otherwise <c>false</c>.</returns>
		/// <remarks>If the parameter cannot be anonymized due to an equals sign in the current value, the parameter name will be changed to the position number if it's not already.</remarks>
		public bool Anonymize(string name, int position, string label)
		{
			var retval = false;
			var param = this[name];
			if (param != null)
			{
				retval |= param.Anonymize(label);
				if (param.Anonymous)
				{
					var newPos = this.GetAnonymousPosition(param);
					if (newPos != position)
					{
						throw new InvalidOperationException(CurrentCulture(AnonymizeBad, param.Name, newPos, position));
					}
				}
			}

			return retval;
		}

		/// <summary>Builds the template text into the specified <see cref="StringBuilder"/>.</summary>
		/// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
		/// <returns>The <paramref name="builder"/> to allow for method chaining.</returns>
		public StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder.Append("{{");
			this.FullName.Build(builder);
			foreach (var param in this.parameters)
			{
				builder.Append('|');
				param.Build(builder);
			}

			builder.Append("}}");

			return builder;
		}

		/// <summary>Removes all parameters from the template.</summary>
		/// <remarks>The name of the template is unaffected.</remarks>
		public void Clear() => this.parameters.Clear();

		/// <summary>Determines whether this template contains the specified parameter.</summary>
		/// <param name="item">The parameter to locate in the template.</param>
		/// <returns>
		///   <span class="keyword">
		///     <span class="languageSpecificText">
		///       <span class="cs">true</span>
		///       <span class="vb">True</span>
		///       <span class="cpp">true</span>
		///     </span>
		///   </span>
		///   <span class="nu">
		///     <span class="keyword">true</span> (<span class="keyword">True</span> in Visual Basic)</span> if <paramref name="item" /> is found in the <see cref="Template"/>; otherwise, <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span>.</returns>
		public bool Contains(Parameter item) => this.parameters.Contains(item);

		/// <summary>Determines whether this template contains a parameter with the specified name.</summary>
		/// <param name="name">The parameter name to locate in the template.</param>
		/// <returns>
		///   <span class="keyword">
		///     <span class="languageSpecificText">
		///       <span class="cs">true</span>
		///       <span class="vb">True</span>
		///       <span class="cpp">true</span>
		///     </span>
		///   </span>
		///   <span class="nu">
		///     <span class="keyword">true</span> (<span class="keyword">True</span> in Visual Basic)</span> if <paramref name="name" /> is found in the <see cref="Template"/>; otherwise, <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span>.</returns>
		public bool Contains(string name) => this[name] != null;

		/// <summary>Copies the elements of the <see cref="Template"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.</summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="Template"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(Parameter[] array, int arrayIndex) => this.parameters.CopyTo(array, arrayIndex);

		/// <summary>Creates a deep copy of the existing template.</summary>
		/// <returns>A deep copy of the existing template.</returns>
		public Template DeepClone() => new Template(this);

		/// <summary>Returns a <see cref="HashSet{T}"/> of duplicate parameter names within the template.</summary>
		/// <returns>A <see cref="HashSet{T}"/> of duplicate parameter names within the template.</returns>
		public HashSet<string> DuplicateNames()
		{
			var names = new HashSet<string>();
			var duplicates = new HashSet<string>();
			foreach (var param in this)
			{
				var name = param.Name;
				if (names.Contains(name))
				{
					duplicates.Add(name);
				}
				else
				{
					names.Add(name);
				}
			}

			return duplicates;
		}

		/// <summary>Finds a parameter whose name is in the parameter list, preferring names that come first in parameter order.</summary>
		/// <param name="names">The parameter names to check for.</param>
		/// <returns>The best match among the <see cref="Parameter"/>s or <c>null</c> if no match was found.</returns>
		/// <remarks>This function is primarily intended to reflect MediaWiki's parameter search. If a parameter within the template code is specified as <c>{{{name|{{{1|}}}}}}</c>, you would call this methods as <c>FindFirst("name", "1");</c> in order to find the "name" parameter if it exists, or the "1" parameter (either specifically named or anonymous) if "name" was not found.</remarks>
		public Parameter FindFirst(params string[] names)
		{
			ThrowNull(names, nameof(names));
			foreach (var name in names)
			{
				var retval = this[name];
				if (retval != null)
				{
					return retval;
				}
			}

			return null;
		}

		/// <summary>Forces all parameters to have names that match their anonymous position. This does not affect their status as anonymous.</summary>
		public void ForceNames()
		{
			var anon = 0;
			foreach (var param in this.AnonymousOnly)
			{
				anon++;
				param.Name = anon.ToStringInvariant();
			}
		}

		/// <summary>Gets the position of an anonymous parameter within the template.</summary>
		/// <param name="item">The parameter to find.</param>
		/// <returns>The anonymous position of the parameter within the template. This value is 1-based, corresponding to MediaWiki's numbering. In other words, given a well-formed template, the anonymous parameter corresponding to <c>{{{2|}}}</c> would return 2. A value of 0 will be returned if the parameter specified is not anonymous; a value of -1 will be returned if the parameter was not found.</returns>
		public int GetAnonymousPosition(Parameter item)
		{
			ThrowNull(item, nameof(item));
			if (!item.Anonymous)
			{
				return 0;
			}

			var anon = 0;
			foreach (var param in this.AnonymousOnly)
			{
				anon++;
				if (param == item)
				{
					return anon;
				}
			}

			return -1;
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Parameter> GetEnumerator() => this.parameters.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.parameters.GetEnumerator();

		/// <summary>Determines whether this instance has anonymous parameters.</summary>
		/// <returns><c>true</c> if this instance has anonymous parameters; otherwise, <c>false</c>.</returns>
		public bool HasAnonymous()
		{
			foreach (var param in this)
			{
				if (param.Anonymous)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Determines whether this template has multiple identically named or numbered parameters.</summary>
		/// <returns><c>true</c> if this instance has duplicates; otherwise, <c>false</c>.</returns>
		/// <remarks>This method uses short-circuit logic and is therefore usually faster than <see cref="DuplicateNames"/> if knowing whether duplicates exist is all that's required.</remarks>
		public bool HasDuplicates()
		{
			var names = new HashSet<string>();
			var anon = 0;

			foreach (var param in this)
			{
				string name;
				if (param.Anonymous)
				{
					anon++;
					name = anon.ToStringInvariant();
				}
				else
				{
					name = param.Name;
				}

				if (names.Contains(name))
				{
					return true;
				}

				names.Add(name);
			}

			return false;
		}

		/// <summary>Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.</summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		/// <seealso cref="GetAnonymousPosition(Parameter)"/>
		public int IndexOf(Parameter item) => this.parameters.IndexOf(item);

		/// <summary>Inserts a <see cref="Parameter"/> into the <see cref="Template"/> at the specified index (not to be confused with the anonymous position).</summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The parameter to insert into the <see cref="Template"/>.</param>
		public void Insert(int index, Parameter item) => this.parameters.Insert(index, item);

		/// <summary>Reformats all parameters to use the <see cref="DefaultNameFormat"/> and <see cref="DefaultValueFormat"/>. See <see cref="Reformat(TemplateString, TemplateString, int)"/> for further details.</summary>
		public void Reformat() => this.Reformat(this.DefaultNameFormat, this.DefaultValueFormat, 0);

		/// <summary>Reformats anonymous parameters so that the specified number of parameters appear on each physical line. Optionally, remaining parameters will be formatting to use the <see cref="DefaultNameFormat"/> and <see cref="DefaultValueFormat"/>. See <see cref="Reformat(TemplateString, TemplateString, int)"/> for further details.</summary>
		/// <param name="anonsPerLine">If greater than zero, the number of anonymous parameters to group on the same line.</param>
		/// <param name="anonsOnly">Whether to reformat only anonymous parameters or all parameters.</param>
		/// <remarks>This method is intended to format anonymous parameters into groupings of related data. For example, a series of pogs might have anonymous parameters equivalent to X, Y, and Name. These could be formatted with each grouping of three on separate lines.</remarks>
		public void Reformat(int anonsPerLine, bool anonsOnly) => this.Reformat(anonsOnly ? null : this.DefaultNameFormat, anonsOnly ? null : this.DefaultValueFormat, anonsPerLine);

		/// <summary>Reformats all parameters using the specified formats. See <see cref="Reformat(TemplateString, TemplateString, int)"/> for further details.</summary>
		/// <param name="nameFormat">Whitespace to add before and after every parameter's name.</param>
		/// <param name="valueFormat">Whitespace to add before and after every parameter's value.</param>
		public void Reformat(TemplateString nameFormat, TemplateString valueFormat) => this.Reformat(nameFormat, valueFormat, 0);

		/// <summary>Reformats all parameters using the specified formats. If a template has no parameters, trailing space will always be removed, regardless of the format specified. Otherwise, the template name is given the valueFormat's TrailingWhiteSpace value. The template name's leading space is always left untouched.</summary>
		/// <param name="nameFormat">Whitespace to add before and after every parameter's name. The <see cref="TemplateString.Value"/> property is ignored.</param>
		/// <param name="valueFormat">Whitespace to add before and after every parameter's value. The <see cref="TemplateString.Value"/> property is ignored.</param>
		/// <param name="anonsPerLine">If greater than zero, the number of anonymous parameters to group on the same line.</param>
		/// <remarks>When formatting anonymous parameters in groups, valueFormat is ignored and all space surrounding an anonymous parameter will be removed in favour of having the specified number of parameters per line. Note that, because of the way anonymous parameters work, the two WhiteSpace properties will be set to string.Empty and the Value parameter will be altered as needed to achieve the appropriate formatting. This ensures that the value formats reported by this class match how MediaWiki itself would interpret them.</remarks>
		public void Reformat(TemplateString nameFormat, TemplateString valueFormat, int anonsPerLine)
		{
			if (this.Count == 0)
			{
				this.FullName.TrailingWhiteSpace = string.Empty;
				return;
			}

			if (valueFormat != null)
			{
				this.FullName.TrailingWhiteSpace = valueFormat.TrailingWhiteSpace;
			}

			var anons = 0;
			var lastNamed = this.FullName;
			Parameter lastAnon = null;
			foreach (var param in this)
			{
				if (param.Anonymous)
				{
					if (anonsPerLine == -1)
					{
						param.FullValue.Trim(true);
					}
					else if (anonsPerLine > 0)
					{
						lastAnon = param;
						if (lastNamed != null)
						{
							lastNamed.TrailingWhiteSpace = "\n";
							lastNamed = null;
						}

						anons++;
						if (anons == anonsPerLine)
						{
							anons = 0;
							if (param.Value.Length == 0 || param.Value[param.Value.Length - 1] != '\n')
							{
								param.Value += '\n';
							}
						}
						else
						{
							param.FullValue.Trim(true);
						}
					}
				}
				else
				{
					lastNamed = param.FullValue;
					if (nameFormat != null)
					{
						param.FullName.LeadingWhiteSpace = nameFormat.LeadingWhiteSpace;
						param.FullName.TrailingWhiteSpace = nameFormat.TrailingWhiteSpace;
					}

					if (valueFormat != null)
					{
						param.FullValue.LeadingWhiteSpace = valueFormat.LeadingWhiteSpace;
						param.FullValue.TrailingWhiteSpace = valueFormat.TrailingWhiteSpace;
					}
				}
			}

			// If number of anonymous parameters isn't an even multiple of anonsPerLine, format the last parameter properly.
			if (anons > 0 && lastAnon != null && (lastAnon.Value.Length == 0 || lastAnon.Value[lastAnon.Value.Length - 1] != '\n'))
			{
				lastAnon.Value += '\n';
			}
		}

		/// <summary>Removes the first occurrence of the specified <see cref="Parameter"/> from the <see cref="Template"/>.</summary>
		/// <param name="item">The <see cref="Parameter"/> to remove from the <see cref="Template"/>.</param>
		/// <returns>
		///   <span class="keyword">
		///     <span class="languageSpecificText">
		///       <span class="cs">true</span>
		///       <span class="vb">True</span>
		///       <span class="cpp">true</span>
		///     </span>
		///   </span>
		///   <span class="nu">
		///     <span class="keyword">true</span> (<span class="keyword">True</span> in Visual Basic)</span> if <paramref name="item" /> was successfully removed from the <see cref="Template"/>; otherwise, <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span>. This method also returns <span class="keyword"><span class="languageSpecificText"><span class="cs">false</span><span class="vb">False</span><span class="cpp">false</span></span></span><span class="nu"><span class="keyword">false</span> (<span class="keyword">False</span> in Visual Basic)</span> if <paramref name="item" /> is not found in the original <see cref="Template"/>.
		/// </returns>
		public bool Remove(Parameter item) => this.parameters.Remove(item);

		/// <summary>Removes all parameters with the specified names from the template.</summary>
		/// <param name="names">The parameter names to remove.</param>
		/// <returns><c>true</c> if any parameters were removed; otherwise, <c>false</c>.</returns>
		public bool Remove(params string[] names)
		{
			ThrowNull(names, nameof(names));

			var allNames = this.CopyParameterNames();

			// Now, check the created names against those provided and remove all parameters with those names, including duplicates, anonymous, and numbered parameters.
			var retval = false;
			var uniqueNames = new HashSet<string>(names);
			for (var i = this.parameters.Count - 1; i >= 0; i--)
			{
				if (uniqueNames.Contains(allNames[i]))
				{
					this.parameters.RemoveAt(i);
					retval = true;
				}
			}

			return retval;
		}

		/// <summary>Removes the <see cref="Parameter"/> at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index) => this.parameters.RemoveAt(index);

		/// <summary>Cleans any parameters that would be ignored by a wiki because they have duplicate names (including hidden names for unnamed parameters).</summary>
		/// <returns>True if anything was removed.</returns>
		public bool RemoveDuplicates()
		{
			var keyOrder = new List<string>();
			var parameterCopy = new Dictionary<string, Parameter>();
			var anon = 0;

			foreach (var param in this)
			{
				string name;
				if (param.Anonymous)
				{
					anon++;
					name = anon.ToStringInvariant();
				}
				else
				{
					name = param.Name;
				}

				if (parameterCopy.TryGetValue(name, out var item))
				{
					keyOrder.Remove(name);
				}

				keyOrder.Add(name);
				parameterCopy[name] = param;
			}

			var retval = keyOrder.Count != this.Count;
			this.Clear();
			foreach (var name in keyOrder)
			{
				this.Add(parameterCopy[name]);
			}

			return retval;
		}

		/// <summary>Removes all parameters that have no value, except anonymous parameters (which may be important for ordering).</summary>
		/// <returns><c>true</c> if any values were removed; otherwise <c>false</c>.</returns>
		public bool RemoveEmpty()
		{
			var retval = false;
			for (var i = this.parameters.Count - 1; i >= 0; i--)
			{
				var param = this[i];
				if (!param.Anonymous && param.Value.Length == 0)
				{
					retval = true;
					this.parameters.RemoveAt(i);
				}
			}

			return retval;
		}

		/// <summary>Removes all parameters specified if they're named or numbered parameters and have no value.</summary>
		/// <param name="names">The parameter names to check. Numbered names will only be applied to explicitly numbered parameters.</param>
		/// <returns><c>true</c> if any values were removed; otherwise <c>false</c>.</returns>
		public bool RemoveEmpty(params string[] names)
		{
			var retval = false;
			if (names != null)
			{
				var uniqueNames = new HashSet<string>(names);
				for (var i = this.parameters.Count - 1; i >= 0; i--)
				{
					var param = this[i];
					if (!param.Anonymous && param.Value.Length == 0 && uniqueNames.Contains(param.Name))
					{
						this.parameters.RemoveAt(i);
					}
				}
			}

			return retval;
		}

		/// <summary>Removes a parameter if its value matches the value specified.</summary>
		/// <param name="name">The name of the parameter to check.</param>
		/// <param name="value">The value to check for.</param>
		/// <returns><c>true</c> if the parameter was removed; otherwise, <c>false</c>.</returns>
		public bool RemoveIfEquals(string name, string value)
		{
			ThrowNull(name, nameof(name));
			var param = this[name];
			if (param != null && param.Value == value)
			{
				this.Remove(param);
				return true;
			}

			return false;
		}

		/// <summary>Removes the specified parameter if it matches the provided predicate.</summary>
		/// <param name="name">The name of the parameter to check.</param>
		/// <param name="predicate">The predicate to check against.</param>
		/// <returns><c>true</c> if the parameter was removed; otherwise, <c>false</c>.</returns>
		public bool RemoveIf(string name, Predicate<string> predicate)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(predicate, nameof(predicate));
			var param = this[name];
			if (param != null && predicate(param.Value))
			{
				this.Remove(param);
				return true;
			}

			return false;
		}

		/// <summary>Renames the specified parameter.</summary>
		/// <param name="from">What to rename the parameter from.</param>
		/// <param name="to">What to rename the parameter to.</param>
		/// <returns><c>true</c> if the parameter was renamed; otherwise, <c>false</c>.</returns>
		/// <exception cref="InvalidOperationException">The <paramref name="to" /> name already exists in the template (except if it matches the <paramref name="from"/> name, which will be ignored).</exception>
		/// <remarks>To bypass the parameter name checks, use <see cref="Parameter.Rename(string)"/> instead.</remarks>
		public bool RenameParameter(string from, string to) => this.RenameParameter(this[from], to);

		/// <summary>Renames the specified parameter.</summary>
		/// <param name="from">The <see cref="Parameter"/> to rename.</param>
		/// <param name="to">What to rename the parameter to.</param>
		/// <returns><c>true</c> if the parameter was renamed; otherwise, <c>false</c>.</returns>
		/// <exception cref="InvalidOperationException">The <paramref name="to" /> name already exists in the template (except if it matches the <paramref name="from" /> name, which will be ignored).</exception>
		/// <remarks>To bypass the parameter name checks, use <see cref="Parameter.Rename(string)"/> instead.</remarks>
		public bool RenameParameter(Parameter from, string to) =>
			(from == null || from.Name == to) ? false :
			this.Contains(to) ? throw new InvalidOperationException(CurrentCulture(ParameterExists, to)) :
			from.Rename(to);

		/// <summary>Sorts parameters in the order specified.</summary>
		/// <param name="sortOrder">A list of parameter names in the order to sort them.</param>
		/// <remarks>Any parameters not specified in <paramref name="sortOrder"/> will be moved after the specified parameters, and will otherwise retain their original order.</remarks>
		public void Sort(params string[] sortOrder) => this.Sort(sortOrder as IEnumerable<string>);

		/// <summary>Sorts parameters in the order specified.</summary>
		/// <param name="sortOrder">A list of parameter names in the order to sort them.</param>
		/// <remarks>Any parameters not specified in <paramref name="sortOrder"/> will be moved after the specified parameters, and will otherwise retain their original order.</remarks>
		public void Sort(IEnumerable<string> sortOrder)
		{
			ThrowNull(sortOrder, nameof(sortOrder));
			this.order = new ComparableCollection<string>(sortOrder, this.Comparer);

			/* Ensures that any parameters not specified in the order will retain their original sorting. */
			var anon = 0;
			foreach (var param in this.parameters)
			{
				if (param.Anonymous)
				{
					anon++;
					param.Name = anon.ToStringInvariant();
				}

				if (!this.order.Contains(param.Name))
				{
					this.order.Add(param.Name);
				}
			}

			this.parameters.Sort(this.IndexedComparer);
		}

		/// <summary>Returns the value of the parameter if it exists; otherwise, <see cref="string.Empty"/>.</summary>
		/// <param name="key">The name of the parameter to get.</param>
		/// <returns>The value of the parameter if it exists; otherwise, <see cref="string.Empty"/>.</returns>
		public string ValueOrDefault(string key) => this.ValueOrDefault(key, string.Empty);

		/// <summary>Returns the value of the parameter if it exists; otherwise, <paramref name="defaultValue"/>.</summary>
		/// <param name="key">The name of the parameter to get.</param>
		/// <param name="defaultValue">The value to use if the parameter is not found.</param>
		/// <returns>The value of the parameter if it exists; otherwise, <paramref name="defaultValue"/>.</returns>
		public string ValueOrDefault(string key, string defaultValue) => this[key]?.Value ?? defaultValue;
		#endregion

		#region Public Override Methods

		/// <summary>Converts the template to its full wiki text.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is a simple wrapper around the <see cref="Build(StringBuilder)"/> method.</remarks>
		public override string ToString() => this.Build(new StringBuilder()).ToString();
		#endregion

		#region Private Static Methods
		private static string InternalRegexText(string regexBefore, string escapedName, string regexAfter)
		{
			var retval = string.Concat(
				@"(?<!{)",
				@"(?<template>",
				@"{{\s*",
				escapedName,
				@"(\s|<)*",
				@"(\|",
				@"(?>",
				@"(?!{|})(.|\n)",
				@"|",
				@"{(?<Depth>)",
				@"|",
				@"}(?<-Depth>)",
				@")*",
				@"(?(Depth)(?!))",
				@")*",
				@"\s*}})");

			if (regexBefore != null)
			{
				retval = @"(?<before>" + regexBefore + ")" + retval;
			}

			if (regexAfter != null)
			{
				retval += @"(?<after>" + regexAfter + ")";
			}

			return retval;
		}

		private static string RegexName(string name)
		{
			var retval = string.Empty;
			if (name.Length > 0)
			{
				if (name[0] == '#')
				{
					// Caller is searching for a parser function, so handle that.
					if (name[name.Length - 1] != ':')
					{
						name += ':';
					}

					retval = Regex.Escape(name) + ".+?";
				}
				else
				{
					retval += "(?i:" + Regex.Escape(name.Substring(0, 1)) + ")";
					if (name.Length > 1)
					{
						retval += Regex.Escape(name.Substring(1)).Replace(@"\ ", @"[_\ ]+");
					}
				}
			}

			return retval;
		}

		private static string RegexName(IEnumerable<string> names)
		{
			using (var namesEnumerator = names.GetEnumerator())
			{
				if (!namesEnumerator.MoveNext())
				{
					return @"[^#\|}]+?";
				}
			}

			var sb = new StringBuilder();
			foreach (var name in names)
			{
				sb.Append("|" + RegexName(name));
			}

			sb.Remove(0, 1);

			return "(" + sb.ToString() + ")";
		}
		#endregion

		#region Private Methods
		private string[] CopyParameterNames()
		{
			// Returns a copy of the list of parameter names so we're not corrupting the actual name for anonymous parameters, if the user has opted to change it.
			var allNames = new string[this.parameters.Count];
			var anon = 0;
			for (var i = 0; i < this.parameters.Count; i++)
			{
				var param = this.parameters[i];
				if (param.Anonymous)
				{
					anon++;
					allNames[i] = anon.ToStringInvariant();
				}
				else
				{
					allNames[i] = param.Name;
				}
			}

			return allNames;
		}

		private Parameter CreateParameter(string name, string value) => new Parameter(this.CreateTemplateString(name, true), this.CreateTemplateString(value, false));

		private TemplateString CreateTemplateString(string value, bool fromName)
		{
			TemplateString copyString;
			if (this.CopyLast && this.Count > 0)
			{
				var lastParam = this[this.Count - 1];
				copyString = fromName ? lastParam.FullName : lastParam.FullValue;
			}
			else
			{
				copyString = fromName ? this.DefaultNameFormat : this.DefaultValueFormat;
			}

			return new TemplateString(copyString.LeadingWhiteSpace, value, copyString.TrailingWhiteSpace);
		}

		// We don't need to handle the case where IndexOf returns -1 because all paramter names are added to the list by the Sort function.
		private int IndexedComparer(Parameter param1, Parameter param2) => this.order.IndexOf(param1.Name).CompareTo(this.order.IndexOf(param2.Name));
		#endregion
	}
}