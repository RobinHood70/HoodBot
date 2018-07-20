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

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Template is a more meaningful name.")]
	public class Template : IList<Parameter>
	{
		#region Fields
		private readonly List<Parameter> parameters = new List<Parameter>();
		private ComparableCollection<string> order;
		#endregion

		#region Constructors
		public Template()
			: this(string.Empty, StringComparer.Ordinal, false)
		{
		}

		public Template(string text)
			: this(text, StringComparer.Ordinal, false)
		{
		}

		public Template(string text, bool caseInsensitive, bool ignoreWhiteSpaceRules)
			: this(text, caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, ignoreWhiteSpaceRules)
		{
		}

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

		public Template(StringComparer comparer)
			: this(string.Empty, comparer, false)
		{
		}
		#endregion

		#region Public Properties
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

		public StringComparer Comparer { get; }

		public bool CopyLast { get; set; } = true;

		public int Count => this.parameters.Count;

		public TemplateString DefaultNameFormat { get; set; } = new TemplateString();

		public TemplateString DefaultValueFormat { get; set; } = new TemplateString();

		public TemplateString FullName { get; set; } = new TemplateString();

		public bool IsReadOnly { get; } = false;

		public string Name
		{
			get => this.FullName.Value;
			set => this.FullName.Value = value;
		}

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
		public Parameter this[int index]
		{
			get => this.parameters[index];

			set => this.parameters[index] = value;
		}

		public Parameter this[string key]
		{
			get
			{
				// We have to search the entire collection, rather than bailing out at first match, because parameters could be duplicated or have anonymous vs. numbered conflicts.
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
		public static Template Create(string name) => new Template() { Name = name };

		public static string ExpandName(string name) => name == null || name.Contains(":") ? name : "Template:" + name;

		public static Regex Find(string name) => Find(null, name, null);

		public static Regex Find(IEnumerable<string> names) => new Regex(InternalRegexText(null, RegexName(names), null), RegexOptions.None, TimeSpan.FromSeconds(10));

		public static Regex Find(string regexBefore, string name, string regexAfter) => Find(regexBefore, name, regexAfter, RegexOptions.None, 10);

		public static Regex Find(string regexBefore, string name, string regexAfter, RegexOptions options, int findTimeout) => new Regex(InternalRegexText(regexBefore, RegexName(name), regexAfter), options, TimeSpan.FromSeconds(findTimeout));

		public static Regex Find(string regexBefore, IEnumerable<string> names, string regexAfter) => Find(regexBefore, names, regexAfter, RegexOptions.None, 10);

		public static Regex Find(string regexBefore, IEnumerable<string> names, string regexAfter, RegexOptions options, int findTimeout) => new Regex(InternalRegexText(regexBefore, RegexName(names), regexAfter), options, TimeSpan.FromSeconds(findTimeout));

		public static Template FindTemplate(string name, string text)
		{
			var finder = Find(name).Match(text);
			if (finder.Success)
			{
				return new Template(finder.Value);
			}

			return null;
		}

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
		public void Add(Parameter item) => this.parameters.Add(item);

		public Parameter Add(string name, string value)
		{
			var parameter = this.CreateParameter(name, value);
			this.Add(parameter);

			return parameter;
		}

		public void AddAfter(string afterName, Parameter parameter)
		{
			var offset = this.IndexOf(this[afterName]);
			if (offset == -1)
			{
				this.Add(parameter);
			}
			else
			{
				this.Insert(offset + 1, parameter);
			}
		}

		public void AddAfter(string afterName, string name, string value)
		{
			var parameter = this.CreateParameter(name, value);
			var offset = this.IndexOf(this[afterName]);
			if (offset == -1)
			{
				this.Add(parameter);
			}
			else
			{
				this.Insert(offset + 1, parameter);
			}
		}

		public Parameter AddAnonymous(string value)
		{
			var p = new Parameter(value);
			this.Add(p);
			return p;
		}

		public void AddBefore(string nameBefore, Parameter parameter)
		{
			var offset = this.IndexOf(this[nameBefore]);
			if (offset == -1)
			{
				offset = 0;
			}

			this.Insert(offset, parameter);
		}

		public void AddBefore(string beforeName, string name, string value)
		{
			var parameter = this.CreateParameter(name, value);
			var offset = this.IndexOf(this[beforeName]);
			if (offset == -1)
			{
				offset = 0;
			}

			this.Insert(offset, parameter);
		}

		public Parameter AddIfBlank(string name, string value)
		{
			var param = this[name];
			if (param == null)
			{
				param = this.Add(name, value);
			}
			else
			{
				if (param.Value.Length == 0)
				{
					param.Value = value;
				}
			}

			return param;
		}

		public Parameter AddIfNotPresent(string name, string value) => this[name] ?? this.Add(name, value);

		public Parameter AddOrChange(string name, string value) => this.AddOrChange(name, value, false);

		public Parameter AddOrChange(string name, int value) => this.AddOrChange(name, value.ToString(CultureInfo.InvariantCulture));

		public Parameter AddOrChange(string name, string value, bool onlyChangeIfBlank)
		{
			var parameter = this.AddIfNotPresent(name, value);
			if (!onlyChangeIfBlank || string.IsNullOrWhiteSpace(parameter.Value))
			{
				parameter.Value = value;
			}

			return parameter;
		}

		public void Anonymize(string name, int position)
		{
			var param = this[name];
			if (param != null)
			{
				param.Anonymize(position.ToStringInvariant());
				var newPos = this.GetAnonymousPosition(param);
				if (newPos != position)
				{
					throw new InvalidOperationException(CurrentCulture(AnonymizeBad, param.Name, newPos, position));
				}
			}
		}

		public void Build(StringBuilder builder)
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
		}

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

		public void Clear() => this.parameters.Clear();

		public bool Contains(Parameter item) => this.parameters.Contains(item);

		public bool Contains(string name) => this[name] != null;

		public Template Copy()
		{
			var copy = new Template(this.Comparer)
			{
				CopyLast = this.CopyLast,
				FullName = this.FullName.Copy(),
				DefaultNameFormat = this.DefaultNameFormat.Copy(),
				DefaultValueFormat = this.DefaultValueFormat.Copy()
			};

			foreach (var param in this)
			{
				copy.Add(param.Copy());
			}

			return copy;
		}

		public void CopyTo(Parameter[] array, int arrayIndex) => this.parameters.CopyTo(array, arrayIndex);

		public IEnumerable<string> DuplicateNames()
		{
			var names = new HashSet<string>();
			foreach (var param in this)
			{
				var name = param.Name;
				if (names.Contains(name))
				{
					yield return name;
				}
				else
				{
					names.Add(name);
				}
			}
		}

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

		public void ForceNames()
		{
			var anon = 0;
			foreach (var param in this.AnonymousOnly)
			{
				anon++;
				param.Name = anon.ToStringInvariant();
			}
		}

		public int GetAnonymousPosition(Parameter input)
		{
			ThrowNull(input, nameof(input));
			var anon = 0;
			if (!input.Anonymous)
			{
				throw new InvalidOperationException("Input is not anonymous.");
			}

			foreach (var param in this.AnonymousOnly)
			{
				anon++;
				if (param == input)
				{
					return anon;
				}
			}

			throw new InvalidOperationException("Input provided isn't from this template.");
		}

		public IEnumerator<Parameter> GetEnumerator() => this.parameters.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.parameters.GetEnumerator();

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

		public int IndexOf(Parameter item) => this.parameters.IndexOf(item);

		public void Insert(int index, Parameter item) => this.parameters.Insert(index, item);

		public void Reformat() => this.Reformat(this.DefaultNameFormat, this.DefaultValueFormat, 0);

		public void Reformat(int anonsPerLine, bool anonsOnly) => this.Reformat(anonsOnly ? null : this.DefaultNameFormat, anonsOnly ? null : this.DefaultValueFormat, anonsPerLine);

		public void Reformat(TemplateString nameFormat, TemplateString valueFormat) => this.Reformat(nameFormat, valueFormat, 0);

		/// <summary>Reformats the template so that all parameters follow the specified formats. If a template has no parameters, trailing space will always be removed, regardless of the format specified. Otherwise, the template name is given the valueFormat's TrailingWhiteSpace value. The template name's leading space is always left untouched.</summary>
		/// <param name="nameFormat">Text to add before and after every parameter's name.</param>
		/// <param name="valueFormat">Text to add before and after every parameter's value.</param>
		/// <param name="anonsPerLine">If greater than zero, valueFormat is ignored and all space surrounding an anonymous parameter will be removed in favour of having the specified number of parameters per line. Note that, because of the way anonymous parameters work, spacing values will be set to string.Empty and the Value parameter will be altered as needed to achieve the appropriate formatting.</param>
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
						param.Trim();
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
							param.Trim();
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

		public bool Remove(Parameter item) => this.parameters.Remove(item);

		public void Remove(params string[] names)
		{
			ThrowNull(names, nameof(names));
			foreach (var name in names)
			{
				this.Remove(this[name]);
			}
		}

		public void RemoveAt(int index) => this.parameters.RemoveAt(index);

		public void RemoveEmpty()
		{
			for (var i = this.Count - 1; i >= 0; i--)
			{
				var param = this[i];
				if (!param.Anonymous && param.Value.Length == 0)
				{
					this.RemoveAt(i);
				}
			}
		}

		public void RemoveEmpty(params string[] names)
		{
			if (names != null)
			{
				foreach (var name in names)
				{
					var param = this[name];
					if (param != null && param.Value.Length == 0)
					{
						this.Remove(param);
					}
				}
			}
		}

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

		public bool RemoveIfEqualsNormalized(string name, string value)
		{
			ThrowNull(name, nameof(name));
			var param = this[name];
			if (param != null && DecodeAndNormalize(param.Value) == DecodeAndNormalize(value))
			{
				this.Remove(param);
				return true;
			}

			return false;
		}

		public void RemoveIfIn(string name, params string[] values) => this.RemoveIfIn(name, values as IEnumerable<string>);

		public void RemoveIfIn(string name, IEnumerable<string> values)
		{
			ThrowNull(name, nameof(name));
			var param = this[name];
			if (param != null && values != null)
			{
				foreach (var value in values)
				{
					if (param.Value == value)
					{
						this.Remove(param);
					}
				}
			}
		}

		public void RenameParameter(string from, string to) => this.RenameParameter(this[from], to);

		public void RenameParameter(Parameter from, string to)
		{
			if (from != null)
			{
				if (this[to] == null)
				{
					from.Name = to;
				}
				else
				{
					throw new InvalidOperationException(CurrentCulture(ParameterExists, to));
				}
			}
		}

		public void Sort(params string[] sortOrder)
		{
			ThrowNull(sortOrder, nameof(sortOrder));
			if (sortOrder.Length > 0)
			{
				this.order = new ComparableCollection<string>(sortOrder, this.Comparer);
			}

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

		public string ValueOrDefault(string key) => this.ValueOrDefault(key, string.Empty);

		public string ValueOrDefault(string key, string defaultValue) => this[key]?.Value ?? defaultValue;
		#endregion

		#region Public Override Methods
		public override string ToString()
		{
			var sb = new StringBuilder();
			this.Build(sb);
			return sb.ToString();
		}
		#endregion

		#region Private Static Methods
		private static string InternalRegexText(string regexBefore, string escapedName, string regexAfter)
		{
			var retval = string.Concat(
				@"(?<!{)",
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
				@"\s*}}");

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