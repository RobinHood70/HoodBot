namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Class Extensions.</summary>
	public static class Extensions
	{
		#region Fields
		private static readonly Regex LeadingSpace = new(@"\A\s*", RegexOptions.None, DefaultRegexTimeout);
		private static readonly Regex TrailingSpace = new(@"\s*\Z", RegexOptions.None, DefaultRegexTimeout);
		#endregion

		#region IBacklink Methods

		/// <summary>Parses the title and returns the trimmed value.</summary>
		/// <param name="backlink">The backlink to get the title for.</param>
		/// <returns>The title.</returns>
		public static string GetTitleText(this IBacklinkNode backlink)
		{
			ThrowNull(backlink, nameof(backlink));
			var retval = backlink.Title.ToValue();
			retval = WikiTextUtilities.DecodeAndNormalize(retval);
			return retval.Trim();
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
			ThrowNull(header, nameof(header));
			var text = WikiTextVisitor.Value(header).TrimEnd();
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
		public static void SetName(this IParameterNode parameter, string name)
		{
			ThrowNull(parameter, nameof(parameter));
			ThrowNull(name, nameof(name));
			parameter.SetName(parameter.Factory.Parse(name));
		}

		/// <summary>Sets the name from a list of nodes.</summary>
		/// <param name="parameter">The parameter to set the name of.</param>
		/// <param name="name">The name.</param>
		public static void SetName(this IParameterNode parameter, IEnumerable<IWikiNode> name)
		{
			ThrowNull(parameter, nameof(parameter));
			ThrowNull(name, nameof(name));
			if (parameter.Name == null)
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
		public static void SetValue(this IParameterNode parameter, string? value)
		{
			ThrowNull(parameter, nameof(parameter));
			if (value == null)
			{
				parameter.Value.Clear();
			}
			else
			{
				var nodes = parameter.Factory.Parse(value);
				parameter.SetValue(nodes);
			}
		}

		/// <summary>Sets the value from a list of nodes.</summary>
		/// <param name="parameter">The parameter to set the value of.</param>
		/// <param name="value">The value.</param>
		public static void SetValue(this IParameterNode parameter, IEnumerable<IWikiNode>? value)
		{
			ThrowNull(parameter, nameof(parameter));
			var old = parameter.Value.ToValue();
			var leading = LeadingSpace.Match(old).Value;
			var trailing = TrailingSpace.Match(old).Value;
			parameter.Value.Clear();
			if (value != null)
			{
				parameter.Value.AddText(leading);
				parameter.Value.AddRange(value);
				parameter.Value.AddText(trailing);
			}
		}
		#endregion

		#region ITemplateNode Extensions

		/// <summary>Adds a new parameter to the template. Copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The added parameter.</returns>
		public static IParameterNode Add(this ITemplateNode template, string name, string value) => template.Add(name, value, true);

		/// <summary>Adds a new parameter to the template. Optionally, copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="copyFormat">Whether to copy the format of the previous parameter or use the values as provided.</param>
		/// <returns>The added parameter.</returns>
		public static IParameterNode Add(this ITemplateNode template, string name, string value, bool copyFormat)
		{
			ThrowNull(template, nameof(template));
			IParameterNode retval;
			var index = copyFormat ? template.FindCopyParameter(false) : -1;
			if (index != -1)
			{
				if (template.Find(name) != null)
				{
					throw new InvalidOperationException(CurrentCulture(Properties.Resources.ParameterExists, name));
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
		public static IParameterNode Add(this ITemplateNode template, string value) => template.Add(value, true);

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="copyFormat">Whether to copy the format of the previous parameter or use the values as provided.</param>
		/// <returns>The added parameter.</returns>
		public static IParameterNode Add(this ITemplateNode template, string value, bool copyFormat)
		{
			ThrowNull(template, nameof(template));
			IParameterNode retval;
			var index = copyFormat ? template.FindCopyParameter(true) : -1;
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

		/// <summary>Changes the value of a parameter to the specified value, or adds the parameter if it doesn't exist.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The parameter that was altered.</returns>
		public static IParameterNode AddOrChange(this ITemplateNode template, string name, string value)
		{
			if (template.Find(name) is not IParameterNode parameter)
			{
				return template.Add(name, value);
			}

			parameter.SetValue(value);
			return parameter;
		}

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public static int FindNumberedIndex(this ITemplateNode template, int number)
		{
			ThrowNull(template, nameof(template));
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
			ThrowNull(parameterNames, nameof(parameterNames));
			var nameSet = new HashSet<string>(parameterNames, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
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
			ThrowNull(template, nameof(template));
			var retval = -1;
			var anonIndex = 0;
			for (var i = 0; i < template.Parameters.Count; i++)
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
		public static IEnumerable<(int Index, IParameterNode Parameter)> GetNumberedParameters(this ITemplateNode template)
		{
			ThrowNull(template, nameof(template));
			var i = 0;
			foreach (var parameter in template.Parameters)
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

		/// <summary>Gets the parameters with the indexed named for anonymous parameters.</summary>
		/// <param name="template">The template to work on.</param>
		/// <returns>A tuple containing the parameter name as well as the parameter itself.</returns>
		public static IEnumerable<(string Name, IParameterNode Parameter)> GetResolvedParameters(this ITemplateNode template)
		{
			ThrowNull(template, nameof(template));
			var anonIndex = 0;
			foreach (var parameter in template.Parameters)
			{
				var name = parameter.Name?.ToValue().Trim() ?? (++anonIndex).ToStringInvariant();
				yield return (name, parameter);
			}
		}

		/// <summary>Determines whether any parameters have numeric names.</summary>
		/// <param name="template">The template to work on.</param>
		/// <returns><see langword="true"/> if the parameter collection has any names which are valid integers; otherwise, <see langword="false"/>.</returns>
		public static bool HasNumericNames(this ITemplateNode template)
		{
			ThrowNull(template, nameof(template));
			foreach (var param in template.Parameters)
			{
				if (!param.Anonymous && int.TryParse(param.Name?.ToValue(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Gets numeric parameters in order, resolving conflicts in the same manner as MediaWiki does.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="addMissing">Set to <see langword="true"/> if missing parameters (e.g., <c>{{Template|1=First|3=Missing2}}</c>) should be inserted as <see langword="null"/> values.</param>
		/// <returns>A read-only dictionary of the parameters.</returns>
		public static IReadOnlyDictionary<int, IParameterNode?> OrderedNumericParameters(this ITemplateNode template, bool addMissing)
		{
			var retval = new SortedDictionary<int, IParameterNode?>();
			var highest = 0;
			foreach (var (index, parameter) in GetNumberedParameters(template))
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
						retval.Add(i, null);
					}
				}
			}

			return retval;
		}

		/// <summary>Gets template parameters grouped into clusters based on their index.</summary>
		/// <param name="template">The template to work on.</param>
		/// <param name="length">The length of each cluster.</param>
		/// <returns>Numeric and numerically-numbered parameters in groups of <paramref name="length"/>.</returns>
		/// <example>Using <c>ParameterCluster(2)</c> on <c>{{MyTemplate|A|1|B|2|C|2=0}}</c> would return three lists: { "A", "0" }, { "B", "2" }, and { "C", null }. In the first case, "0" is returned because of the overridden parameter <c>2=0</c>. In the last case, <see langword="null"/> is returned because the parameter has no pairing within the template call. </example>
		public static IEnumerable<IList<IParameterNode?>> ParameterCluster(this ITemplateNode template, int length)
		{
			var parameters = template.OrderedNumericParameters(true);
			var i = 1;
			var retval = new List<IParameterNode?>();
			while (i < parameters.Count)
			{
				for (var j = 0; j < length; j++)
				{
					retval.Add(parameters[i + j]);
				}

				yield return retval;
				retval = new List<IParameterNode?>();
				i += length;
			}

			if (retval.Count > 0)
			{
				while (retval.Count < length)
				{
					retval.Add(null);
				}

				yield return retval;
			}
		}

		/// <summary>Removes any parameters with the same name/index as a later parameter.</summary>
		/// <param name="template">The template to work on.</param>
		public static void RemoveDuplicates(this ITemplateNode template)
		{
			ThrowNull(template, nameof(template));
			var nameList = new HashSet<string>(StringComparer.Ordinal);
			var index = template.Parameters.Count;
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
			ThrowNull(template, nameof(template));
			var retval = false;
			var anonIndex = 0;
			var i = 0;
			while (i < template.Parameters.Count)
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
			var old = template.Title.ToValue();
			var leading = LeadingSpace.Match(old).Value;
			var trailing = TrailingSpace.Match(old).Value;

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
			ThrowNull(sortOrder, nameof(sortOrder));
			var indeces = new Dictionary<string, int>(StringComparer.Ordinal);
			var i = 0;
			foreach (var value in sortOrder)
			{
				indeces.Add(value, i);
				i++;
			}

			var sorted = new IParameterNode?[indeces.Count];
			var unsorted = new List<IParameterNode>();
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
			ThrowNull(text, nameof(text));
			ThrowNull(mask, nameof(mask));
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
			ThrowNull(text, nameof(text));
			ThrowNull(mask, nameof(mask));
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
		#endregion
	}
}