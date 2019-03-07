namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using System.Text.RegularExpressions;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a template as a name and collection of <see cref="Parameter"/>s.</summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Template is a more meaningful name.")]
	public class Template : ParameterCollection
	{
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
		/// <param name="templateText">The full text of the template.</param>
		/// <param name="caseInsensitive">Whether parameter names should be treated case-insensitively.</param>
		/// <param name="ignoreWhiteSpaceRules">Whether MediaWiki whitespace rules should be ignored. If true, all surrounding whitespace will be included in the parameter names and values.</param>
		/// <remarks>The text can optionally include opening and closing braces, but these are not required except in the rare case where the template has two or more braces at both the start and the end which are not the enclosing braces (e.g., <c>{{{{Template to provide name}}|param={{{param|}}}}}</c>). In any other case, no braces are required, meaning that a new template can be created by specifying only the template name, if required.</remarks>
		public Template(string templateText, bool caseInsensitive, bool ignoreWhiteSpaceRules)
			: this(templateText, caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, ignoreWhiteSpaceRules)
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
		/// <param name="templateText">The full text of the template.</param>
		/// <param name="comparer">The <see cref="StringComparer"/> to use for parameter names.</param>
		/// <param name="ignoreWhiteSpaceRules">Whether MediaWiki whitespace rules should be ignored when parsing the template text. If true, all surrounding whitespace will be included in the parameter names and values.</param>
		/// <remarks>The text can optionally include opening and closing braces, but these are not required except in the rare case where the template has two or more braces at both the start and the end which are not the enclosing braces (e.g., <c>{{{{Template to provide name}}|param={{{param|}}}}}</c>). In any other case, no braces are required, meaning that a new template can be created by specifying only the template name, if required.</remarks>
		public Template(string templateText, StringComparer comparer, bool ignoreWhiteSpaceRules)
			: base(comparer)
		{
			templateText = templateText?.Trim() ?? string.Empty;
			var parser = new ParameterParser(templateText, false, false, ignoreWhiteSpaceRules);
			this.DefaultNameFormat = parser.DefaultFormat(true);
			this.DefaultValueFormat = parser.DefaultFormat(false);
			this.LeadingColon = parser.LeadingColon;
			this.NameLeadingWhiteSpace = parser.Name.LeadingWhiteSpace;
			this.Name = parser.Name.Value;
			this.NameTrailingWhiteSpace = parser.Name.TrailingWhiteSpace;
			foreach (var parameter in parser.Parameters)
			{
				this.Add(parameter);
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether to force a colon to be prepended to the name. For templates, this is usually for Main space; for links, it's used to force a File or Category to be a link to the page instead of its normal functionality.</summary>
		/// <value><c>true</c> if the wiki link should be forced to be a link; otherwise, <c>false</c>.</value>
		public bool LeadingColon { get; set; }

		/// <summary>Gets or sets the template name.</summary>
		/// <value>The template name.</value>
		public string Name { get; set; }

		/// <summary>Gets or sets the white space displayed before the template name.</summary>
		/// <value>The leading white space.</value>
		public string NameLeadingWhiteSpace { get; set; }

		/// <summary>Gets or sets the white space displayed after the template name, but before the first parameter (if any).</summary>
		/// <value>The trailing white space.</value>
		public string NameTrailingWhiteSpace { get; set; }
		#endregion

		#region Private Properties
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by debugger UI")]
		private string DebuggerDisplay => Invariant($"{this.Name}: Count = {this.Count}");
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

		/// <summary>Reformats all parameters to use the <see cref="ParameterCollection.DefaultNameFormat"/> and <see cref="ParameterCollection.DefaultValueFormat"/>. See <see cref="Reformat(ParameterString, ParameterString)"/> for further details.</summary>
		public void Reformat() => this.Reformat(this.DefaultNameFormat, this.DefaultValueFormat);

		/// <summary>Reformats anonymous parameters so that the specified number of parameters appear on each physical line. Optionally, remaining parameters will be formatting to use the <see cref="ParameterCollection.DefaultNameFormat"/> and <see cref="ParameterCollection.DefaultValueFormat"/>. See <see cref="Reformat(ParameterString, ParameterString, int)"/> for further details.</summary>
		/// <param name="anonsPerLine">If greater than zero, the number of anonymous parameters to group on the same line.</param>
		/// <param name="anonsOnly">Whether to reformat only anonymous parameters or all parameters.</param>
		/// <remarks>This method is intended to format anonymous parameters into groupings of related data. For example, a series of pogs might have anonymous parameters equivalent to X, Y, and Name. These could be formatted with each grouping of three on separate lines.</remarks>
		public void Reformat(int anonsPerLine, bool anonsOnly) => this.Reformat(anonsOnly ? null : this.DefaultNameFormat, anonsOnly ? null : this.DefaultValueFormat, anonsPerLine);

		/// <summary>Reformats all parameters using the specified formats. See <see cref="Reformat(ParameterString, ParameterString, int)"/> for further details.</summary>
		/// <param name="nameFormat">Whitespace to add before and after every parameter's name.</param>
		/// <param name="valueFormat">Whitespace to add before and after every parameter's value.</param>
		public void Reformat(ParameterString nameFormat, ParameterString valueFormat) => this.Reformat(nameFormat, valueFormat, 0);

		/// <summary>Reformats all parameters using the specified formats. If a template has no parameters, trailing space will always be removed, regardless of the format specified. Otherwise, the template name is given the valueFormat's TrailingWhiteSpace value. The template name's leading space is always left untouched.</summary>
		/// <param name="nameFormat">Whitespace to add before and after every parameter's name. The <see cref="ParameterString.Value"/> property is ignored.</param>
		/// <param name="valueFormat">Whitespace to add before and after every parameter's value. The <see cref="ParameterString.Value"/> property is ignored.</param>
		/// <param name="anonsPerLine">If greater than zero, the number of anonymous parameters to group on the same line.</param>
		/// <remarks>When formatting anonymous parameters in groups, valueFormat is ignored and all space surrounding an anonymous parameter will be removed in favour of having the specified number of parameters per line. Note that, because of the way anonymous parameters work, the two WhiteSpace properties will be set to string.Empty and the Value parameter will be altered as needed to achieve the appropriate formatting. This ensures that the value formats reported by this class match how MediaWiki itself would interpret them.</remarks>
		public void Reformat(ParameterString nameFormat, ParameterString valueFormat, int anonsPerLine)
		{
			if (this.Count == 0)
			{
				this.NameTrailingWhiteSpace = string.Empty;
				return;
			}

			if (valueFormat != null)
			{
				this.NameTrailingWhiteSpace = valueFormat.TrailingWhiteSpace;
			}

			var anons = 0;
			ParameterString lastNamed = null;
			Parameter lastAnon = null;
			var doName = false;
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
							lastNamed.TrailingWhiteSpace = lastNamed.TrailingWhiteSpace.Trim() + '\n';
							lastNamed = null;
						}
						else if (!doName)
						{
							doName = true;
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
					if (nameFormat != null && param.FullName != null)
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

			if (doName)
			{
				this.NameTrailingWhiteSpace = this.NameTrailingWhiteSpace.TrimEnd() + '\n';
			}

			// If number of anonymous parameters isn't an even multiple of anonsPerLine, format the last parameter properly.
			if (anons > 0 && lastAnon != null && (lastAnon.Value.Length == 0 || lastAnon.Value[lastAnon.Value.Length - 1] != '\n'))
			{
				lastAnon.Value += '\n';
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Builds the template text into the specified <see cref="StringBuilder"/>.</summary>
		/// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
		/// <returns>The <paramref name="builder" /> to allow for method chaining.</returns>
		public override StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder.Append("{{");
			if (this.LeadingColon)
			{
				builder.Append(':');
			}

			builder
				.Append(this.NameLeadingWhiteSpace)
				.Append(this.Name)
				.Append(this.NameTrailingWhiteSpace);
			return base
				.Build(builder)
				.Append("}}");
		}
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
	}
}