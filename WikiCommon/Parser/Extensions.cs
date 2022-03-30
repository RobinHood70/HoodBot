﻿namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Ardalis.GuardClauses;
	using RobinHood70.CommonCode;

	#region Public Enumerations

	/// <summary>The format to use when adding new parameters or reformatting existing ones.</summary>
	public enum ParameterFormat
	{
		/// <summary>Parameter values will not be changed.</summary>
		NoChange,

		/// <summary>Remove all trailing whitespace.</summary>
		PackedTrail,

		/// <summary>Remove all leading and trailing whitespace.</summary>
		Packed,

		/// <summary>Change trailing space to a single carriage return.</summary>
		OnePerLine,

		/// <summary>Replace leading and trailing space with the spacing from the previous parameter of the same type.</summary>
		Copy,
	}
	#endregion

	/// <summary>Parser extensions class.</summary>
	public static class Extensions
	{
		#region Fields
		private static readonly Regex LeadingSpace = new(@"\A\s*", RegexOptions.None, Globals.DefaultRegexTimeout);
		private static readonly Regex TrailingSpace = new(@"\s*\Z", RegexOptions.None, Globals.DefaultRegexTimeout);
		#endregion

		#region IBacklink Methods

		/// <summary>Parses the title and returns the trimmed value.</summary>
		/// <param name="backlink">The backlink to get the title for.</param>
		/// <returns>The title.</returns>
		public static string GetTitleText(this IBacklinkNode backlink)
		{
			var retval = backlink.NotNull(nameof(backlink)).Title.ToValue();
			retval = WikiTextUtilities.DecodeAndNormalize(retval);
			return retval.Trim();
		}
		#endregion

		#region IEnumerable<IParameterNode> Methods

		/// <summary>Converts a collection of <see cref="IParameterNode"/>s to key and value strings.</summary>
		/// <param name="parameters">The parameters to convert.</param>
		/// <returns>An enumeration of key/value strings. The key string is nullable.</returns>
		public static IEnumerable<(string? Key, string Value)> ToKeyValue(this IEnumerable<IParameterNode> parameters)
		{
			parameters.ThrowNull(nameof(parameters));
			foreach (var param in parameters)
			{
				var value = param.Value.ToRaw();
				yield return param.Name is NodeCollection name
					? (name.ToRaw().Trim(), value.Trim())
					: (null, value);
			}
		}
		#endregion

		#region IHeaderNode Extensions

		/// <summary>Gets the text inside the heading delimiters.</summary>
		/// <param name="header">The header to get the title for.</param>
		/// <param name="innerTrim">if set to <see langword="true"/>, trims the inner text before returning it.</param>
		/// <returns>The text inside the heading delimiters.</returns>
		/// <remarks>This is method is provided as a temporary measure. The intent is to alter the parser itself so as to make this method unnecessary.</remarks>
		public static string GetInnerText(this IHeaderNode header, bool innerTrim)
		{
			var text = WikiTextVisitor.Raw(header.NotNull(nameof(header))).TrimEnd();
			text = text.Substring(header.Level, text.Length - header.Level * 2);
			return innerTrim ? text.Trim() : text;
		}
		#endregion

		#region IParameterNode Extensions

		/// <summary>Determines whether the specified parameter node is null or whitespace.</summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <c>false</c>.</returns>
		/// <remarks>For the purposes of this method, whitespace is considered to be a single text node with whitespace. Anything else, including HTML comment nodes and other unvalued nodes, will cause this to return <see langword="false"/>.</remarks>
		public static bool IsNullOrWhitespace(this IParameterNode? parameter) => parameter == null || parameter.Value.Count switch
		{
			0 => true,
			1 => parameter.Value[0] is ITextNode textNode && textNode.Text.TrimStart().Length == 0,
			_ => false,
		};

		/// <summary>Sets the name to the specified text.</summary>
		/// <param name="parameter">The parameter to set the name of.</param>
		/// <param name="name">The name.</param>
		public static void SetName(this IParameterNode parameter, string name) => parameter
			.NotNull(nameof(parameter))
			.SetName(parameter.Factory.Parse(name.NotNull(nameof(name))));

		/// <summary>Sets the name from a list of nodes.</summary>
		/// <param name="parameter">The parameter to set the name of.</param>
		/// <param name="name">The name.</param>
		public static void SetName(this IParameterNode parameter, IEnumerable<IWikiNode> name)
		{
			parameter.ThrowNull(nameof(parameter));
			name.ThrowNull(nameof(name));
			if (parameter.Name is null)
			{
				parameter.AddName(name);
			}
			else
			{
				parameter.Name.Clear();
				parameter.Name.AddRange(name);
			}
		}

		/// <summary>Sets the value to the specified text.</summary>
		/// <param name="parameter">The parameter to set the value of.</param>
		/// <param name="value">The value.</param>
		public static void SetValue(this IParameterNode parameter, string? value, ParameterFormat format)
		{
			parameter.ThrowNull(nameof(parameter));
			var paramValue = parameter.Value;
			if (value == null || value.Length == 0)
			{
				paramValue.Clear();
				return;
			}

			if (format == ParameterFormat.Copy)
			{
				var (leading, trailing) = GetSurroundingSpace(paramValue.ToValue());
				value = TrimValue(value, format);
				var copyNodes = parameter.Factory.Parse(value);
				paramValue.Clear();
				paramValue.AddText(leading);
				paramValue.AddRange(copyNodes);
				paramValue.AddText(trailing);
				return;
			}

			value = TrimValue(value, format);
			var nodes = parameter.Factory.Parse(value);
			paramValue.Clear();
			paramValue.AddRange(nodes);
		}

		/// <summary>Converts a parameter to its raw key=value format without a leading pipe.</summary>
		/// <param name="parameter">The parameter to convert.</param>
		public static string ToKeyValue(this IParameterNode parameter) => parameter.NotNull(nameof(parameter)).Name is NodeCollection name
			? name.ToRaw() + '=' + parameter.Value.ToRaw()
			: parameter.Value.ToRaw();

		/// <summary>Determines whether the specified parameter node is null or whitespace.</summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="value">A variable to place the value into. Note that blank values will still be returned here with their full content, even when the return value is true.</param>
		/// <remarks>For the purposes of this method, whitespace is considered to be a single text node with whitespace. Anything else, including HTML comment nodes and other unvalued nodes, will cause this to return <see langword="false"/>.</remarks>
		/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <c>false</c>.</returns>
		public static bool TryGetValue(this IParameterNode? parameter, out NodeCollection? value)
		{
			value = parameter?.Value;
			return value?.Count > 0 && value[0] is ITextNode && value?.ToRaw().Trim().Length > 0; // Check the whole thing in case of fragmented text nodes.
		}

		/// <summary>Returns the value of a template parameter or the default value.</summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="defaultValue">The value to return if the node is absent or empty.</param>
		/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <c>false</c>.</returns>
		public static string ValueOrDefault(this IParameterNode? parameter, string defaultValue)
		{
			if (parameter?.Value is not NodeCollection nullNodes || nullNodes.Count == 0)
			{
				return defaultValue;
			}

			var retval = nullNodes.ToRaw();
			return retval.Trim().Length == 0
				? defaultValue
				: retval;
		}
		#endregion

		#region ITemplateNode Extensions

		/// <summary>Adds a new parameter to the template. Copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The added parameter.</returns>
		public static IParameterNode Add(this ITemplateNode template, string name, string value) => template.Add(name, value, ParameterFormat.Copy);

		/// <summary>Adds a new parameter to the template. Optionally, copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
		/// <returns>The added parameter.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the parameter is not found.</exception>
		public static IParameterNode Add(this ITemplateNode template, string name, string value, ParameterFormat paramFormat)
		{
			Guard.Against.Null(template, nameof(template));
			IParameterNode retval;
			value = TrimValue(value, paramFormat);

			var index = paramFormat == ParameterFormat.Copy ? template.FindCopyParameter(false) : -1;
			if (index != -1)
			{
				if (template.Find(name) != null)
				{
					throw new InvalidOperationException(Globals.CurrentCulture(Properties.Resources.ParameterExists, name));
				}

				var previous = template.Parameters[index];
				retval = template.Factory.ParameterNodeFromOther(previous, name, value);
				template.Parameters.Insert(index + 1, retval);
			}
			else
			{
				retval = template.Factory.ParameterNodeFromParts(name, value);
				template.Parameters.Add(retval);
			}

			return retval;
		}

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The added parameter.</returns>
		public static IParameterNode Add(this ITemplateNode template, string value) => template.Add(value, ParameterFormat.Copy);

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
		/// <returns>The added parameter.</returns>
		public static IParameterNode Add(this ITemplateNode template, string value, ParameterFormat paramFormat)
		{
			Guard.Against.Null(template, nameof(template));
			IParameterNode retval;
			value = TrimValue(value, paramFormat);

			var index = paramFormat == ParameterFormat.Copy ? template.FindCopyParameter(true) : -1;
			if (index != -1)
			{
				var previous = template.Parameters[index];
				retval = template.Factory.ParameterNodeFromOther(previous, value);
				template.Parameters.Insert(index + 1, retval);
			}
			else
			{
				retval = template.Factory.ParameterNodeFromParts(value);
				template.Parameters.Add(retval);
			}

			return retval;
		}

		/// <summary>Adds a parameter with the specified value if it does not already exist.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <remarks>If the value already exists, even if blank, it will remain unchanged.</remarks>
		/// <returns>The parameter that was altered.</returns>
		public static IParameterNode AddIfNotExists(this ITemplateNode template, string name, string value) => template.Find(name) is IParameterNode parameter
			? parameter
			: template.Add(name, value);

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public static int FindNumberedIndex(this ITemplateNode template, int number)
		{
			template.ThrowNull(nameof(template));
			var retval = -1;
			var i = 0;
			var name = number.ToStringInvariant();
			for (var index = 0; index < template.Parameters.Count; index++)
			{
				var node = template.Parameters[index];
				if (node.Name == null)
				{
					if (++i == number)
					{
						retval = index;
					}
				}
				else if (string.Equals(node.Name?.ToValue(), name, StringComparison.Ordinal))
				{
					retval = index;
				}
			}

			return retval;
		}

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public static IParameterNode? Find(this ITemplateNode template, int number)
		{
			var index = template.FindNumberedIndex(number);
			return index == -1 ? null : template.Parameters[index];
		}

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public static IParameterNode? Find(this ITemplateNode template, params string[] parameterNames) => template.Find(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public static IParameterNode? Find(this ITemplateNode template, bool ignoreCase, params string[] parameterNames)
		{
			IParameterNode? retval = null;
			foreach (var parameter in template.FindAll(ignoreCase, parameterNames))
			{
				retval = parameter;
			}

			return retval;
		}

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public static IEnumerable<IParameterNode> FindAll(this ITemplateNode template, params string[] parameterNames) => template.FindAll(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public static IEnumerable<IParameterNode> FindAll(this ITemplateNode template, IEnumerable<string> parameterNames) => template.FindAll(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public static IEnumerable<IParameterNode> FindAll(this ITemplateNode template, bool ignoreCase, params string[] parameterNames) => template.FindAll(ignoreCase, (IEnumerable<string>)parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public static IEnumerable<IParameterNode> FindAll(this ITemplateNode template, bool ignoreCase, IEnumerable<string> parameterNames)
		{
			HashSet<string> nameSet = new(parameterNames.NotNull(nameof(parameterNames)), ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			foreach (var (name, parameter) in GetResolvedParameters(template))
			{
				if (nameSet.Contains(name))
				{
					yield return parameter;
				}
			}
		}

		/// <summary>Finds the index of the named or numbered parameter.</summary>
		/// <param name="template">The template.</param>
		/// <param name="name">The name.</param>
		/// <returns>The index of the requested parameter or -1 if not found.</returns>
		public static int FindIndex(this ITemplateNode template, string name)
		{
			var retval = -1;
			var anonIndex = 0;
			for (var i = 0; i < template.NotNull(nameof(template)).Parameters.Count; i++)
			{
				var paramName = template.Parameters[i].Name?.ToValue() ?? (++anonIndex).ToStringInvariant();
				if (string.Equals(paramName, name, StringComparison.Ordinal))
				{
					retval = i;
				}
			}

			return retval;
		}

		/// <summary>Gets a simple collection of all numbered parameters.</summary>
		/// <param name="template">The template to work on.</param>
		/// <value>The numbered parameters.</value>
		/// <remarks>Parameters returned by this function include both fully anonymous and numerically named parameters. The index returned is not guaranteed to be unique or consecutive. For example, a template like <c>{{Test|anon1a|anon2|1=anon1b|anon3}}</c> would return, in order: 1=anon1a, 2=anon2, 1=anon1b, 3=anon3.</remarks>
		/// <returns>A tuple containing the parameter number as well as the parameter itself.</returns>
		public static IEnumerable<(int Index, IParameterNode Parameter)> GetNumericParameters(this ITemplateNode template)
		{
			var i = 0;
			foreach (var parameter in template.NotNull(nameof(template)).Parameters)
			{
				if (parameter.Anonymous)
				{
					yield return (++i, parameter);
				}
				else if (int.TryParse(parameter.Name?.ToValue(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var namedNumber) && namedNumber > 0)
				{
					yield return (namedNumber, parameter);
				}
			}
		}

		/// <summary>Gets numeric parameters in order, resolving conflicts in the same manner as MediaWiki does.</summary>
		/// <param name="template">The template to work on.</param>
		/// <returns>A read-only dictionary of the parameters.</returns>
		public static IReadOnlyDictionary<int, IParameterNode> GetNumericParametersSorted(this ITemplateNode template) => GetNumericParametersSorted(template, false);

		/// <summary>Gets numeric parameters in order, resolving conflicts in the same manner as MediaWiki does.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="addMissing">Set to <see langword="true"/> if missing parameters (e.g., <c>{{Template|1=First|3=Missing2}}</c>) should be inserted as <see langword="null"/> values.</param>
		/// <returns>A read-only dictionary of the parameters.</returns>
		public static IReadOnlyDictionary<int, IParameterNode> GetNumericParametersSorted(this ITemplateNode template, bool addMissing)
		{
			SortedDictionary<int, IParameterNode> retval = new();
			var highest = 0;
			foreach (var (index, parameter) in GetNumericParameters(template))
			{
				if (index > highest)
				{
					highest = index;
				}

				retval[index] = parameter;
			}

			if (addMissing)
			{
				for (var i = 1; i < highest; i++)
				{
					if (!retval.ContainsKey(i))
					{
						retval.Add(i, template.Factory.ParameterNodeFromParts(string.Empty));
					}
				}
			}

			return retval;
		}

		/// <summary>Get the raw value of a parameter or null.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The raw text of the parameter value or <see langword="null"/> if not found.</returns>
		public static string? GetRaw(this ITemplateNode template, int number) => template.NotNull(nameof(template)).Find(number)?.Value.ToRaw().Trim();

		/// <summary>Get the raw value of a parameter or null.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to search for.</param>
		/// <returns>The raw text of the parameter value or <see langword="null"/> if not found.</returns>
		public static string? GetRaw(this ITemplateNode template, string name) => template.NotNull(nameof(template)).Find(name)?.Value.ToRaw().Trim();

		/// <summary>Gets the parameters with the indexed named for anonymous parameters.</summary>
		/// <param name="template">The template to work on.</param>
		/// <returns>A tuple containing the parameter name as well as the parameter itself.</returns>
		public static IEnumerable<(string Name, IParameterNode Parameter)> GetResolvedParameters(this ITemplateNode template)
		{
			var anonIndex = 0;
			foreach (var parameter in template.NotNull(nameof(template)).Parameters)
			{
				var name = parameter.Name?.ToValue().Trim() ?? (++anonIndex).ToStringInvariant();
				yield return (name, parameter);
			}
		}

		/// <summary>Get the value of a parameter or null.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The raw text of the parameter value or <see langword="null"/> if not found.</returns>
		public static string? GetValue(this ITemplateNode template, int number) => template.NotNull(nameof(template)).Find(number)?.Value.ToValue().Trim();

		/// <summary>Get the value of a parameter or null.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to search for.</param>
		/// <returns>The raw text of the parameter value or <see langword="null"/> if not found.</returns>
		public static string? GetValue(this ITemplateNode template, string name) => template.NotNull(nameof(template)).Find(name)?.Value.ToValue().Trim();

		/// <summary>Determines whether any parameters have numeric names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <returns><see langword="true"/> if the parameter collection has any names which are valid integers; otherwise, <see langword="false"/>.</returns>
		public static bool HasNumericNames(this ITemplateNode template)
		{
			foreach (var param in template.NotNull(nameof(template)).Parameters)
			{
				if (!param.Anonymous && int.TryParse(param.Name?.ToValue(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Gets template parameters grouped into clusters based on their index.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="length">The length of each cluster.</param>
		/// <returns>Numeric and numerically-numbered parameters in groups of <paramref name="length"/>.</returns>
		/// <example>Using <c>ParameterCluster(2)</c> on <c>{{MyTemplate|A|1|B|2|C|2=0}}</c> would return three lists: { "A", "0" }, { "B", "2" }, and { "C", null }. In the first case, "0" is returned because of the overridden parameter <c>2=0</c>. In the last case, <see langword="null"/> is returned because the parameter has no pairing within the template call. </example>
		public static IEnumerable<IList<IParameterNode>> ParameterCluster(this ITemplateNode template, int length)
		{
			var parameters = template.NotNull(nameof(template)).GetNumericParametersSorted(true);
			var i = 1;
			List<IParameterNode> retval = new();
			while (i < parameters.Count)
			{
				for (var j = 0; j < length; j++)
				{
					retval.Add(parameters[i + j]);
				}

				yield return retval;
				retval = new List<IParameterNode>();
				i += length;
			}

			if (retval.Count > 0)
			{
				while (retval.Count < length)
				{
					retval.Add(template.Factory.ParameterNodeFromParts(string.Empty));
				}

				yield return retval;
			}
		}

		/// <summary>Removes any parameters with the same name/index as a later parameter.</summary>
		/// <param name="template">The template to work on.</param>
		public static void RemoveDuplicates(this ITemplateNode template)
		{
			var index = template.NotNull(nameof(template)).Parameters.Count;
			HashSet<string> nameList = new(StringComparer.Ordinal);
			var anonIndex = 0;
			while (index >= 0 && index < template.Parameters.Count)
			{
				var name = template.Parameters[index].Name?.ToValue() ?? (++anonIndex).ToStringInvariant();
				if (nameList.Contains(name))
				{
					template.Parameters.RemoveAt(index);
				}
				else
				{
					nameList.Add(name);
				}

				index--;
			}
		}

		/// <summary>Finds the parameters with the given name and removes it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns><see langword="true"/>if any parameters were removed.</returns>
		/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
		public static bool Remove(this ITemplateNode template, string parameterName)
		{
			var retval = false;
			var anonIndex = 0;
			var i = 0;
			while (i < template.NotNull(nameof(template)).Parameters.Count)
			{
				var name = template.Parameters[i].Name?.ToValue();
				if (string.IsNullOrEmpty(name))
				{
					++anonIndex;
					name = anonIndex.ToStringInvariant();
				}

				if (string.Equals(name, parameterName, StringComparison.Ordinal))
				{
					template.Parameters.RemoveAt(i);
					retval = true;
				}
				else
				{
					i++;
				}
			}

			return retval;
		}

		/// <summary>Sets a new Title value. preserving whitespace.</summary>
		/// <param name="template">The template.</param>
		/// <param name="newTitle">The new title.</param>
		public static void SetTitle(this ITemplateNode template, string newTitle)
		{
			var (leading, trailing) = GetSurroundingSpace(template.NotNull(nameof(template)).Title.ToValue());
			template.Title.Clear();
			template.Title.AddText(leading + newTitle + trailing);
		}

		/// <summary>Sorts parameters in the order specified.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="sortOrder">A list of parameter names in the order to sort them.</param>
		/// <remarks>Any parameters not specified in <paramref name="sortOrder"/> will be moved after the specified parameters, and will otherwise retain their original order.</remarks>
		public static void Sort(this ITemplateNode template, params string[] sortOrder) => template.Sort(sortOrder as IEnumerable<string>);

		/// <summary>Sorts parameters in the order specified.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="sortOrder">A list of parameter names in the order to sort them.</param>
		/// <remarks>Any parameters not specified in <paramref name="sortOrder"/> will be moved after the specified parameters, and will otherwise retain their original order.</remarks>
		public static void Sort(this ITemplateNode template, IEnumerable<string> sortOrder)
		{
			template.ThrowNull(nameof(template));
			Dictionary<string, int> indeces = new(StringComparer.Ordinal);
			var i = 0;
			foreach (var value in sortOrder.NotNull(nameof(sortOrder)))
			{
				indeces.Add(value, i);
				i++;
			}

			IParameterNode?[]? sorted = new IParameterNode?[indeces.Count];
			List<IParameterNode> unsorted = new();
			foreach (var (name, parameter) in GetResolvedParameters(template))
			{
				var index = indeces.GetValueOrDefault(name, -1);
				if (index == -1)
				{
					unsorted.Add(parameter);
				}
				else
				{
					sorted[index] = parameter;
				}
			}

			template.Parameters.Clear();
			foreach (var parameter in sorted)
			{
				if (parameter != null)
				{
					template.Parameters.Add(parameter);
				}
			}

			foreach (var parameter in unsorted)
			{
				template.Parameters.Add(parameter);
			}
		}

		/// <summary>Returns the value of a template parameter or the default value.</summary>
		/// <param name="template">The template to search.</param>
		/// <param name="parameterName">The parameter name.</param>
		/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <c>false</c>.</returns>
		public static bool TrueOrFalse(this ITemplateNode? template, string parameterName) =>
			template?.Find(parameterName)?.Value is NodeCollection nullNodes &&
			nullNodes.Count != 0 &&
			nullNodes.ToRaw().Trim().Length != 0;

		/// <summary>Changes the value of a parameter to the specified value, or adds the parameter if it doesn't exist.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The parameter that was altered.</returns>
		public static IParameterNode? Update(this ITemplateNode template, string name, string? value) => Update(template, name, value, ParameterFormat.Copy);

		/// <summary>Adds a new parameter to the template. Optionally, copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="format">The type of formatting to apply to the parameter value if being added. For existing parameters, the existing format will be retained.</param>
		/// <returns>The added parameter.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the parameter is not found.</exception>
		public static IParameterNode? Update(this ITemplateNode template, string name, string? value, ParameterFormat format)
		{
			Guard.Against.Null(template, nameof(template));
			Guard.Against.Null(name, nameof(name));
			IParameterNode retval;
			if (value == null)
			{
				template.Remove(name);
				return null;
			}

			if (template.Find(name) is IParameterNode existing)
			{
				existing.SetValue(value, format);
				return existing;
			}

			var index = format == ParameterFormat.Copy ? template.FindCopyParameter(false) : -1;
			if (index == -1)
			{
				value = TrimValue(value, format);
				retval = template.Factory.ParameterNodeFromParts(name, value);
				template.Parameters.Add(retval);
				return retval;
			}

			var previous = template.Parameters[index];
			retval = template.Factory.ParameterNodeFromOther(previous, name, value);
			template.Parameters.Insert(index + 1, retval);
			return retval;
		}

		/// <summary>Updates a parameter value if the current value is entirely whitespace or the parameter is missing.</summary>
		/// <param name="template">The template to update.</param>
		/// <param name="name">The name of the parameter to update.</param>
		/// <param name="value">The value to update the parameter to.</param>
		/// <returns>The parameter affected, regardless of whether it was changed.</returns>
		public static IParameterNode UpdateIfEmpty(this ITemplateNode template, string name, string value)
		{
			if (template.Find(name) is IParameterNode parameter)
			{
				if (parameter.Value.ToValue().Trim().Length == 0)
				{
					Update(template, name, value);
				}

				return parameter;
			}

			return template.Add(name, value);
		}

		/// <summary>Returns the value of a template parameter or the default value.</summary>
		/// <param name="template">The template to search.</param>
		/// <param name="parameterName">The parameter name.</param>
		/// <param name="defaultValue">The value to return if the node is absent or empty.</param>
		/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <c>false</c>.</returns>
		public static string ValueOrDefault(this ITemplateNode? template, string parameterName, string defaultValue)
		{
			if (template?.Find(parameterName)?.Value is not NodeCollection nullNodes || nullNodes.Count == 0)
			{
				return defaultValue;
			}

			var retval = nullNodes.ToRaw();
			return retval.Trim().Length == 0
				? defaultValue
				: retval;
		}
		#endregion

		#region String Extensions

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int Span([Localizable(false)] this string text, char mask, int offset) => Span(text, new string(mask, 1), offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int Span([Localizable(false)] this string text, char mask, int offset, int limit) => Span(text, new string(mask, 1), offset, limit);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int Span([Localizable(false)] this string text, string mask, int offset) => Span(text, mask, offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int Span([Localizable(false)] this string text, string mask, int offset, int limit)
		{
			text.ThrowNull(nameof(text));
			mask.ThrowNull(nameof(mask));
			if (offset < 0)
			{
				offset += text.Length;
			}

			if (offset < 0 || offset >= text.Length)
			{
				return 0;
			}

			if (limit < 0)
			{
				limit += text.Length;
				if (limit < 0)
				{
					return 0;
				}
			}
			else
			{
				limit += offset;
			}

			if (limit > text.Length)
			{
				limit = text.Length;
			}

			var baseOffset = offset;
			while (offset < limit && mask.IndexOf(text[offset], StringComparison.Ordinal) != -1)
			{
				offset++;
			}

			return offset - baseOffset;
		}

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse([Localizable(false)] this string text, char mask, int offset) => SpanReverse(text, new string(mask, 1), offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse([Localizable(false)] this string text, char mask, int offset, int limit) => SpanReverse(text, new string(mask, 1), offset, limit);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse([Localizable(false)] this string text, string mask, int offset) => SpanReverse(text, mask, offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse([Localizable(false)] this string text, string mask, int offset, int limit)
		{
			text.ThrowNull(nameof(text));
			mask.ThrowNull(nameof(mask));
			if (offset < 0)
			{
				offset += text.Length;
			}

			if (offset <= 0 || offset > text.Length)
			{
				return 0;
			}

			limit = limit < 0 ? -limit : offset - limit;
			if (limit < 0)
			{
				limit = 0;
			}
			else if (limit >= offset)
			{
				return 0;
			}

			// Decrement offset because we're going in the reverse direction, so want to look at the character *before* the current one.
			offset--;
			var baseOffset = offset;
			while (offset >= limit && mask.IndexOf(text[offset], StringComparison.Ordinal) != -1)
			{
				offset--;
			}

			return baseOffset - offset;
		}
		#endregion

		#region Private Methods
		private static int FindCopyParameter(this ITemplateNode template, bool isAnon)
		{
			for (var i = template.Parameters.Count - 1; i >= 0; i--)
			{
				if (template.Parameters[i].Anonymous == isAnon)
				{
					return i;
				}
			}

			return -1;
		}

		private static (string Leading, string Trailing) GetSurroundingSpace(string old)
		{
			string trailingLine;
			if (old.Length > 0 && old[^1] == '\n')
			{
				trailingLine = "\n";
				old = old[0..^1];
			}
			else
			{
				trailingLine = string.Empty;
			}

			var leadingMatch = LeadingSpace.Match(old);
			var trailingMatch = TrailingSpace.Match(old);
			return (leadingMatch.Value, ((leadingMatch.Index == trailingMatch.Index) ? string.Empty : trailingMatch.Value) + trailingLine);
		}

		private static string TrimValue(string? value, ParameterFormat paramFormat)
		{
			value ??= string.Empty;
			value = paramFormat switch
			{
				ParameterFormat.NoChange => value,
				ParameterFormat.PackedTrail => value.TrimEnd(),
				ParameterFormat.Packed => value.Trim(),
				ParameterFormat.OnePerLine => value.TrimEnd() + '\n',
				ParameterFormat.Copy => value.Trim(),
				_ => value,
			};
			return value;
		}
		#endregion
	}
}