﻿namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

#region Public Enumerations

/// <summary>The format to use when adding new parameters or reformatting existing ones.</summary>
public enum ParameterFormat
{
	/// <summary>Parameter whitespace will not be changed.</summary>
	Verbatim,

	/// <summary>Remove all trailing whitespace.</summary>
	PackedTrail,

	/// <summary>Remove all leading and trailing whitespace.</summary>
	Packed,

	/// <summary>Change trailing space to a single carriage return.</summary>
	OnePerLine,

	/// <summary>Replace leading and trailing space with the existing spacing, if the parameter already exists, or the spacing from the previous parameter of the same type (anon or named) for parameters that don't yet exist.</summary>
	Copy,
}
#endregion

/// <summary>Parser extensions class.</summary>
public static class Extensions
{
	#region Static Fields
	private static readonly Regex AttribRegex = new(@"\b(?<key>\w+)\s*=?\s*(""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>\w+))", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region IBacklink Methods

	/// <summary>Parses the title and returns the trimmed value.</summary>
	/// <param name="backlink">The backlink to get the title for.</param>
	/// <returns>The title.</returns>
	public static string GetTitleText(this IBacklinkNode backlink)
	{
		ArgumentNullException.ThrowIfNull(backlink);
		var retval = backlink.TitleNodes.ToValue();
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
		ArgumentNullException.ThrowIfNull(parameters);
		return ToKeyValue(parameters);

		static IEnumerable<(string? Key, string Value)> ToKeyValue(IEnumerable<IParameterNode> parameters)
		{
			foreach (var param in parameters)
			{
				var value = param.Value.ToRaw();
				yield return param.Name is WikiNodeCollection name
					? (name.ToRaw().Trim(), value.Trim())
					: (null, value);
			}
		}
	}
	#endregion

	#region IHeaderNode Extensions

	/// <summary>Gets the text inside the heading delimiters.</summary>
	/// <param name="header">The header to get the title for.</param>
	/// <param name="trim">if set to <see langword="true"/>, trims the inner text before returning it.</param>
	/// <returns>The text inside the heading delimiters.</returns>
	/// <remarks>This is method is provided as a temporary measure. The intent is to alter the parser itself so as to make this method unnecessary.</remarks>
	public static string GetTitle(this IHeaderNode? header, bool trim)
	{
		if (header is null)
		{
			return string.Empty;
		}

		var text = WikiTextVisitor.Raw(header.Title);
		return trim ? text.Trim() : text;
	}
	#endregion

	#region IList<IWikiNode> Extensions

	/// <summary>Converts the <see cref="IWikiNode"/> to raw text.</summary>
	/// <param name="node">The node to convert.</param>
	/// <returns>The converted node.</returns>
	public static string ToRaw(this IWikiNode node) => WikiTextVisitor.Raw(node);

	/// <summary>Converts the node collection to raw text.</summary>
	/// <param name="nodes">The nodes to convert.</param>
	/// <returns>The converted nodes.</returns>
	public static string ToRaw(this IEnumerable<IWikiNode> nodes) => WikiTextVisitor.Raw(nodes);

	/// <summary>Converts the node to its value text.</summary>
	/// <param name="node">The node to convert.</param>
	/// <returns>The converted node.</returns>
	public static string ToValue(this IWikiNode node) => WikiTextVisitor.Value(node);

	/// <summary>Converts the node collection to its value text.</summary>
	/// <param name="nodes">The nodes to convert.</param>
	/// <returns>The converted nodes.</returns>
	public static string ToValue(this IList<IWikiNode> nodes) => WikiTextVisitor.Value(nodes);
	#endregion

	#region IParameterNode Extensions

	/// <summary>Gets the parameter number if it's numeric.</summary>
	/// <param name="parameter">The parameter to check.</param>
	/// <returns>The parameter number if it's an integer; otherwise 0.</returns>
	public static int GetNumberFromName(this IParameterNode? parameter)
	{
		int.TryParse(parameter?.Name?.ToValue(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var namedNumber);
		return namedNumber;
	}

	/// <summary>Get the trimmed raw value of the parameter.</summary>
	/// <param name="parameter">The parameter to work on.</param>
	/// <returns>The trimmed raw text of the parameter value.</returns>
	public static string GetRaw(this IParameterNode parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		return parameter.Value.ToRaw().Trim();
	}

	/// <summary>Get the trimmed value of the parameter.</summary>
	/// <param name="parameter">The parameter to work on.</param>
	/// <returns>The trimmed text of the parameter value.</returns>
	public static string GetValue(this IParameterNode parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		return parameter.Value.ToValue().Trim();
	}

	/// <summary>Determines whether the specified parameter node is null or whitespace.</summary>
	/// <param name="parameter">The parameter.</param>
	/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <see langword="false"/>.</returns>
	/// <remarks>For the purposes of this method, whitespace is considered to be a single text node with whitespace. Anything else, including HTML comment nodes and other unvalued nodes, will cause this to return <see langword="false"/>.</remarks>
	public static bool IsNullOrWhitespace(this IParameterNode? parameter) => parameter == null || parameter.Value.Count switch
	{
		0 => true,
		1 => parameter.Value[0] is ITextNode textNode && textNode.Text.TrimStart().Length == 0,
		_ => false,
	};

	/// <summary>Determines if a parameter is numeric.</summary>
	/// <param name="parameter">The parameter to check.</param>
	/// <returns><see langword="true"/> if the parameter is anonymous or if the parameter name is an integer; otherwise <see langword="false"/>.</returns>
	public static bool IsNumeric(this IParameterNode? parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		return parameter.Anonymous || parameter.GetNumberFromName() != 0;
	}

	/// <summary>Updates a parameter only if it's not loosely equal to the existing value, based on the comparer provided.</summary>
	/// <param name="parameter">The parameter to alter.</param>
	/// <param name="value">The value to update to.</param>
	/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
	/// <param name="comparer">The string comparer to define loose equality.</param>
	/// <remarks>This method can be used for things like case-insensitive checks or removing markup before determining whether the value should be updated.</remarks>
	public static void LooseUpdate(this IParameterNode parameter, string value, ParameterFormat paramFormat, IEqualityComparer<string> comparer)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		ArgumentNullException.ThrowIfNull(value);
		ArgumentNullException.ThrowIfNull(comparer);
		var oldText = parameter.GetRaw();
		value = parameter.Factory.EscapeParameterText(value, parameter.Name is null);
		if (!comparer.Equals(oldText, value.Trim()))
		{
			parameter.SetValueNoEscape(value, paramFormat);
		}
	}

	/// <summary>Sets the name to the specified text.</summary>
	/// <param name="parameter">The parameter to set the name of.</param>
	/// <param name="name">The name.</param>
	public static void SetName(this IParameterNode parameter, string name)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		ArgumentNullException.ThrowIfNull(name);
		parameter.SetName(parameter.Factory.Parse(name));
	}

	/// <summary>Sets the name from a list of nodes.</summary>
	/// <param name="parameter">The parameter to set the name of.</param>
	/// <param name="name">The name.</param>
	public static void SetName(this IParameterNode parameter, IEnumerable<IWikiNode> name)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		ArgumentNullException.ThrowIfNull(name);
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
	/// <param name="value">The value. May not be null.</param>
	/// <param name="paramFormat">The desired parameter format.</param>
	public static void SetValue(this IParameterNode parameter, string? value, ParameterFormat paramFormat) => SetValueNoEscape(parameter, parameter?.Factory.EscapeParameterText(value, parameter.Name is null), paramFormat);

	/// <summary>Special-purpose version of SetValue that sets the value without escaping it.</summary>
	/// <param name="parameter">The parameter to set the value of.</param>
	/// <param name="value">The value. May not be null.</param>
	/// <param name="paramFormat">The desired parameter format.</param>
	public static void SetValueNoEscape(this IParameterNode parameter, string? value, ParameterFormat paramFormat)
	{
		// This method uses a different name rather than a boolean choice, since it will be very rare that you wouldn't want to escape the value.
		ArgumentNullException.ThrowIfNull(parameter);
		ArgumentNullException.ThrowIfNull(value);
		var paramValue = parameter.Value;
		value ??= string.Empty;
		if (paramFormat != ParameterFormat.Verbatim)
		{
			var oldValue = paramValue.ToRaw();
			value = value.Trim();
			if (paramFormat != ParameterFormat.Copy)
			{
				oldValue = TrimValue(oldValue, paramFormat);
			}

			value = EmbeddedValue.CopyWhitespace(oldValue, value);
		}

		paramValue.Clear();
		paramValue.AddParsed(value);
	}

	/// <summary>Converts a parameter to its raw key=value format without a leading pipe.</summary>
	/// <param name="parameter">The parameter to convert.</param>
	public static string ToKeyValue(this IParameterNode parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		return parameter.Name is WikiNodeCollection name
			? name.ToRaw() + '=' + parameter.GetRaw()
			: parameter.GetRaw();
	}
	#endregion

	#region ITagNode Extensions

	/// <summary>Gets the attributes from a tag.</summary>
	/// <param name="tag">The tag to examine.</param>
	/// <returns>The list of attributes on the specified tag. Name-only tags will be returned as [name] = null.</returns>
	/// <remarks>This is a very simple Regex-based solution that should cover the vast majority of tags. For complete HTML compliance, you'll need to use another method.</remarks>
	public static IReadOnlyList<KeyValuePair<string, string?>> GetAttributeList(this ITagNode tag)
	{
		ArgumentNullException.ThrowIfNull(tag);
		if (string.IsNullOrEmpty(tag.Attributes))
		{
			return [];
		}

		var attribs = new List<KeyValuePair<string, string?>>();
		foreach (var match in AttribRegex.Matches(tag.Attributes) as IReadOnlyList<Match>)
		{
			var valueGroup = match.Groups["value"];
			var value = valueGroup.Success ? valueGroup.Value : null;
			attribs.Add(new KeyValuePair<string, string?>(match.Groups["key"].Value, value));
		}

		return attribs;
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
	public static IParameterNode Add(this ITemplateNode template, string? name, string value, ParameterFormat paramFormat)
	{
		// TODO: Needs rewrite using IParameterNode.SetValue
		ArgumentNullException.ThrowIfNull(template);
		IParameterNode retval;
		if (name is not null && template.Find(name) is not null)
		{
			throw new InvalidOperationException(Globals.CurrentCulture(Properties.Resources.ParameterExists, name));
		}

		value = TrimValue(value, paramFormat);
		if (paramFormat == ParameterFormat.Copy &&
			template.FindCopyParameter(name is null) is var index &&
			index != -1)
		{
			var previous = template.Parameters[index];
			retval = template.Factory.ParameterNodeFromOther(previous, name, value);
			template.Parameters.Insert(index + 1, retval);
			var copyNode = index == 0
				? template.TitleNodes
				: template.Parameters[index - 1].Value;
			var embedded = EmbeddedValue.FindWhitespace(copyNode.ToValue());
			if (embedded.After.Length > 0)
			{
				// In the event that parameters are indented but trailing }} isn't, this will ensure that the formatting is as expected instead of added parameters being flush with the }}.
				previous.Value.TrimEnd();
				previous.Value.AddText(embedded.After);
			}

			return retval;
		}

		if (paramFormat is ParameterFormat.OnePerLine or ParameterFormat.PackedTrail)
		{
			if (template.Parameters.Count == 0)
			{
				template.TitleNodes.TrimEnd();
				template.TitleNodes.AddText("\n");
			}
			else
			{
				var previous = template.Parameters[^1];
				if (!previous.Anonymous)
				{
					var previousValue = previous.Value.ToRaw();
					if (previousValue.Length == 0 || previousValue[^1] != '\n')
					{
						previous.Value.AddText("\n");
					}
				}
			}
		}

		retval = template.Factory.ParameterNodeFromParts(name, value);
		template.Parameters.Add(retval);
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
	public static IParameterNode Add(this ITemplateNode template, string value, ParameterFormat paramFormat) => Add(template, null, value, paramFormat);

	/// <summary>Adds a parameter with the specified value if it does not already exist.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="name">The name of the parameter to add.</param>
	/// <param name="value">The value of the parameter to add.</param>
	/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
	/// <remarks>If the value already exists, even if blank, it will remain unchanged.</remarks>
	/// <returns>The parameter that was altered.</returns>
	public static IParameterNode AddIfNotExists(this ITemplateNode template, string name, string value, ParameterFormat paramFormat) => template.Find(name) is IParameterNode parameter
		? parameter
		: template.Add(name, value, paramFormat);

	/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="number">The numbered parameter to search for.</param>
	/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
	public static int FindNumberedIndex(this ITemplateNode template, int number)
	{
		ArgumentNullException.ThrowIfNull(template);
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
			else if (node.Name.ToValue().OrdinalEquals(name))
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
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(parameterNames);
		return FindAll(template, ignoreCase, parameterNames);

		static IEnumerable<IParameterNode> FindAll(ITemplateNode template, bool ignoreCase, IEnumerable<string> parameterNames)
		{
			HashSet<string> nameSet = new(parameterNames, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			foreach (var (name, parameter) in GetResolvedParameters(template))
			{
				if (nameSet.Contains(name))
				{
					yield return parameter;
				}
			}
		}
	}

	/// <summary>Finds the index of the named or numbered parameter.</summary>
	/// <param name="template">The template.</param>
	/// <param name="name">The name.</param>
	/// <returns>The index of the requested parameter or -1 if not found.</returns>
	public static int FindIndex(this ITemplateNode template, string name)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(name);
		var retval = -1;
		var anonIndex = 0;
		for (var i = 0; i < template.Parameters.Count; i++)
		{
			var paramName = template.Parameters[i].Name?.ToValue() ?? (++anonIndex).ToStringInvariant();
			if (paramName.OrdinalEquals(name))
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
		ArgumentNullException.ThrowIfNull(template);
		return GetNumericParameters(template);

		static IEnumerable<(int Index, IParameterNode Parameter)> GetNumericParameters(ITemplateNode template)
		{
			var i = 0;
			foreach (var parameter in template.Parameters)
			{
				if (parameter.Anonymous)
				{
					yield return (++i, parameter);
				}
				else if (parameter.GetNumberFromName() is var namedNumber && namedNumber > 0)
				{
					yield return (namedNumber, parameter);
				}
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
		SortedDictionary<int, IParameterNode> retval = [];
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

	/// <summary>Get the trimmed raw value of a parameter or <see langword="null"/> if not found.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="number">The numbered parameter to search for.</param>
	/// <returns>The trimmed raw text of the parameter value or <see langword="null"/> if not found.</returns>
	public static string? GetRaw(this ITemplateNode template, int number)
	{
		ArgumentNullException.ThrowIfNull(template);
		return template.Find(number)?.GetRaw();
	}

	/// <summary>Get the trimmed raw value of a parameter or <see langword="null"/> if not found.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="name">The name of the parameter to search for.</param>
	/// <returns>The trimmed raw text of the parameter value or <see langword="null"/> if not found.</returns>
	public static string? GetRaw(this ITemplateNode template, string name)
	{
		ArgumentNullException.ThrowIfNull(template);
		return template.Find(name)?.GetRaw();
	}

	/// <summary>Gets the parameters with the indexed named for anonymous parameters.</summary>
	/// <param name="template">The template to work on.</param>
	/// <returns>A tuple containing the parameter name as well as the parameter itself.</returns>
	public static IEnumerable<(string Name, IParameterNode Parameter)> GetResolvedParameters(this ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(template);
		return GetResolvedParameters(template);

		static IEnumerable<(string Name, IParameterNode Parameter)> GetResolvedParameters(ITemplateNode template)
		{
			var anonIndex = 0;
			foreach (var parameter in template.Parameters)
			{
				var name = parameter.Name?.ToValue().Trim() ?? (++anonIndex).ToStringInvariant();
				yield return (name, parameter);
			}
		}
	}

	/// <summary>Get the trimmed value of a parameter or <see langword="null"/> if not found.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="number">The numbered parameter to search for.</param>
	/// <returns>The trimmed text of the parameter value or <see langword="null"/> if not found.</returns>
	public static string? GetValue(this ITemplateNode template, int number)
	{
		ArgumentNullException.ThrowIfNull(template);
		return template.Find(number)?.GetValue();
	}

	/// <summary>Get the trimmed value of a parameter or null.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="name">The name of the parameter to search for.</param>
	/// <returns>The trimmed text of the parameter value or <see langword="null"/> if not found.</returns>
	public static string? GetValue(this ITemplateNode template, string name)
	{
		ArgumentNullException.ThrowIfNull(template);
		return template.Find(name)?.GetValue();
	}

	/// <summary>Determines whether any parameters have numeric names.</summary>
	/// <param name="template">The template to work on.</param>
	/// <returns><see langword="true"/> if the parameter collection has any names which are valid integers; otherwise, <see langword="false"/>.</returns>
	public static bool HasNumericNames(this ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(template);
		foreach (var param in template.Parameters)
		{
			if (!param.Anonymous && int.TryParse(param.Name?.ToValue(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>Updates a parameter only if it's not loosely equal to the existing value, based on the comparer provided.</summary>
	/// <param name="template">The template to alter.</param>
	/// <param name="name">The name of the parameter to update or remove.</param>
	/// <param name="value">The value to update to.</param>
	/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
	/// <param name="comparer">The string comparer to define loose equality.</param>
	/// <returns>The parameter affected, regardless of whether it was changed.</returns>
	/// <remarks>This method can be used for things like case-insensitive checks or removing markup before determining whether the value should be updated.</remarks>
	public static IParameterNode? LooseUpdate(this ITemplateNode template, string name, string value, ParameterFormat paramFormat, IEqualityComparer<string> comparer)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(value);
		if (template.Find(name) is IParameterNode parameter)
		{
			parameter.LooseUpdate(value, paramFormat, comparer);
			return parameter;
		}

		return template.Add(name, value, paramFormat);
	}

	/// <summary>Gets template parameters grouped into clusters based on their index.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="length">The length of each cluster.</param>
	/// <returns>Numeric and numerically-numbered parameters in groups of <paramref name="length"/>.</returns>
	/// <example>Using <c>ParameterCluster(2)</c> on <c>{{MyTemplate|A|1|B|2|C|2=0}}</c> would return three lists: { "A", "0" }, { "B", "2" }, and { "C", null }. In the first case, "0" is returned because of the overridden parameter <c>2=0</c>. In the last case, <see langword="null"/> is returned because the parameter has no pairing within the template call. </example>
	public static IEnumerable<IList<IParameterNode>> ParameterCluster(this ITemplateNode template, int length)
	{
		ArgumentNullException.ThrowIfNull(template);
		return ParameterCluster(template, length);

		static IEnumerable<IList<IParameterNode>> ParameterCluster(ITemplateNode template, int length)
		{
			var parameters = template.GetNumericParametersSorted(true);
			var i = 1;
			List<IParameterNode> retval = [];
			while (i < parameters.Count)
			{
				for (var j = 0; j < length; j++)
				{
					retval.Add(parameters[i + j]);
				}

				yield return retval;
				retval = [];
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
	}

	/// <summary>Gets the highest-priority match based on the order of <paramref name="parameterNames"/> and returns that value or <see langword="null"/>.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="parameterNames">The case-sensitive names of the parameters to search for.</param>
	/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
	public static IParameterNode? PrioritizedFind(this ITemplateNode template, params string[] parameterNames) => PrioritizedFind(template, false, parameterNames);

	/// <summary>Gets the highest-priority match based on the order of <paramref name="parameterNames"/> and returns that value or <see langword="null"/>.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
	/// <param name="parameterNames">The names of the parameters to search for.</param>
	/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
	public static IParameterNode? PrioritizedFind(this ITemplateNode template, bool ignoreCase, params string[] parameterNames)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(parameterNames);

		var comparison = ignoreCase
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		var paramList = new List<(string Name, IParameterNode Value)>(template.GetResolvedParameters());
		foreach (var param in parameterNames)
		{
			for (var i = paramList.Count - 1; i >= 0; i--)
			{
				if (paramList[i].Name.Equals(param, comparison))
				{
					return paramList[i].Value;
				}
			}
		}

		return null;
	}

	/// <summary>Removes any parameters with the same name as a later parameter.</summary>
	/// <param name="template">The template to work on.</param>
	/// <remarks>Anonymous parameters that are replaced with numbered parameters will be blanked but not removed. This is to prevent the issues associated with constructs like <c>{{Template|abc|def|ghi|2=def=xyz}}</c> and other edge cases that will likely require human intervention.</remarks>
	public static void RemoveDuplicates(this ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(template);
		Dictionary<string, int> nameList = new(StringComparer.Ordinal);
		var removals = new List<int>();
		var anonIndex = 0;
		for (var index = 0; index < template.Parameters.Count; index++)
		{
			var name = template.Parameters[index].Name?.ToValue()
				?? (++anonIndex).ToStringInvariant();
			if (nameList.TryGetValue(name, out var offset))
			{
				// MediaWiki uses is_int() to process what's an integer, and this does not seem to allow for variant numeral systems like Hebrew, only Arabic digits, so InvariantCulture is used here.
				if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
				{
					template.Parameters[offset].Value.Clear();
				}
				else
				{
					removals.Add(offset);
				}
			}

			nameList[name] = index;
		}

		for (var i = removals.Count - 1; i >= 0; i--)
		{
			template.Parameters.RemoveAt(removals[i]);
		}
	}

	/// <summary>Finds the parameters with the given name and removes it.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="parameterName">The name of the parameter.</param>
	/// <returns><see langword="true"/>if any parameters were removed.</returns>
	/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
	public static bool Remove(this ITemplateNode template, string parameterName)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(parameterName);
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

			if (name.OrdinalEquals(parameterName))
			{
				if (i == template.Parameters.Count - 1)
				{
					var lastParam = template.Parameters[^1];
					var embedded = EmbeddedValue.FindWhitespace(lastParam.Value.ToValue());
					if (template.Parameters.Count > 1)
					{
						var newLast = template.Parameters[^2];
						newLast.Value.Trim();
						if (embedded.After.Length > 0)
						{
							newLast.Value.AddText(embedded.After);
						}
					}
				}

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

	/// <summary>Finds the parameters with the given name and removes it.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="parameterName">The name of the parameter.</param>
	/// <param name="condition">The condition for the parameter to be removed.</param>
	/// <returns><see langword="true"/>if any parameters were removed.</returns>
	/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
	public static bool RemoveIfValue(this ITemplateNode template, string parameterName, Predicate<WikiNodeCollection?> condition)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(parameterName);
		ArgumentNullException.ThrowIfNull(condition);
		return condition(template.Find(parameterName)?.Value) && Remove(template, parameterName);
	}

	/// <summary>Finds the parameters with the given name and removes it.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="parameterName">The name of the parameter.</param>
	/// <param name="condition">The condition for the parameter to be removed.</param>
	/// <returns><see langword="true"/>if any parameters were removed.</returns>
	/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
	public static bool RemoveIf(this ITemplateNode template, string parameterName, bool condition) => condition && Remove(template, parameterName);

	/// <summary>Finds the parameters with the given name and removes it.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="from">The current name of the parameter to rename.</param>
	/// <param name="to">What to rename the parameter to.</param>
	/// <returns><see langword="true"/>if any parameters were removed.</returns>
	/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
	public static bool RenameParameter(this ITemplateNode template, string from, string to)
	{
		var retval = false;
		foreach (var parameter in template.FindAll(from))
		{
			parameter.Name?.Clear();
			parameter.SetName(to);
			retval = true;
		}

		return retval;
	}

	/// <summary>Sets a new Title value. preserving whitespace.</summary>
	/// <param name="template">The template.</param>
	/// <param name="newTitle">The new title.</param>
	public static void SetTitle(this ITemplateNode template, string newTitle)
	{
		ArgumentNullException.ThrowIfNull(template);
		newTitle ??= string.Empty;
		var embedded = EmbeddedValue.FindWhitespace(template.TitleNodes.ToValue());
		template.TitleNodes.Clear();
		template.TitleNodes.AddText(embedded.Before + newTitle + embedded.After);
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
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(sortOrder);
		Dictionary<string, int> indeces = new(StringComparer.Ordinal);
		var i = 0;
		foreach (var value in sortOrder)
		{
			indeces.Add(value, i);
			i++;
		}

		var sorted = new IParameterNode?[indeces.Count];
		List<IParameterNode> unsorted = [];
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
	/// <returns><see langword="true"/> if the parameter is null or consists entirely of whitespace; otherwise, <see langword="false"/>.</returns>
	public static bool TrueOrFalse(this ITemplateNode? template, string parameterName) =>
		template?.GetRaw(parameterName)?.Length != 0;

	/// <summary>Changes the value of a parameter to the specified value, or adds the parameter if it doesn't exist.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="name">The name of the parameter to add.</param>
	/// <param name="value">The value of the parameter to add.</param>
	/// <returns>The parameter that was altered.</returns>
	public static IParameterNode? Update(this ITemplateNode template, string name, string? value) => Update(template, name, value, ParameterFormat.Copy, false);

	/// <summary>Changes the value of a parameter to the specified value, or adds the parameter if it doesn't exist. Also applies the selected formatting.</summary>
	/// <param name="template">The template to work on.</param>
	/// <param name="name">The name of the parameter to add.</param>
	/// <param name="value">The value of the parameter to add.</param>
	/// <param name="paramFormat">The type of formatting to apply to the parameter value if being added. For existing parameters, the existing format will be retained.</param>
	/// <param name="removeIfEmpty">If set to <see langword="true"/> and <paramref name="value"/> is an empty string, remove the parameter. Otherwise, the parameter will only be removed if <paramref name="value"/> is <see langword="null"/>.</param>
	/// <returns>The added parameter.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the parameter is not found.</exception>
	public static IParameterNode? Update(this ITemplateNode template, string name, string? value, ParameterFormat paramFormat, bool removeIfEmpty) => template.UpdateOrRemove(name, value, paramFormat, value is null || (removeIfEmpty && value.Length == 0));

	/// <summary>Updates a parameter value if the current value is entirely whitespace or the parameter is missing.</summary>
	/// <param name="template">The template to update.</param>
	/// <param name="name">The name of the parameter to update.</param>
	/// <param name="value">The value to update the parameter to.</param>
	/// <returns>The parameter affected, regardless of whether it was changed.</returns>
	public static IParameterNode UpdateIfEmpty(this ITemplateNode template, string name, string value) => UpdateIfEmpty(template, name, value, ParameterFormat.Verbatim);

	/// <summary>Updates a parameter with the specified value if it is blank or does not exist.</summary>
	/// <param name="template">The template to update.</param>
	/// <param name="name">The name of the parameter to update.</param>
	/// <param name="value">The value to update the parameter to.</param>
	/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
	/// <returns>The parameter affected, regardless of whether it was changed.</returns>
	public static IParameterNode UpdateIfEmpty(this ITemplateNode template, string name, string value, ParameterFormat paramFormat)
	{
		ArgumentNullException.ThrowIfNull(template);
		var param = template.Find(name);
		if (param is IParameterNode parameter)
		{
			if (parameter.GetValue().Length == 0)
			{
				parameter.SetValue(value, paramFormat);
			}

			return parameter;
		}

		return template.Add(name, value, paramFormat);
	}

	/// <summary>Updates a parameter, or removes it if <paramref name="removeCondition"/> is <see langword="true"/>.</summary>
	/// <param name="template">The template to alter.</param>
	/// <param name="name">The name of the parameter to update or remove.</param>
	/// <param name="value">The value to update to.</param>
	/// <param name="paramFormat">The type of formatting to apply to the parameter value.</param>
	/// <param name="removeCondition">Whether the parameter should be removed.</param>
	/// <returns>The parameter affected, regardless of whether it was changed.</returns>
	public static IParameterNode? UpdateOrRemove(this ITemplateNode template, string name, string? value, ParameterFormat paramFormat, bool removeCondition)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(name);
		if (removeCondition)
		{
			template.Remove(name);
			return null;
		}

		// "value is null" is a valid removeCondition, so we only throw after the remove condition has been checked.
		ArgumentNullException.ThrowIfNull(value);
		if (template.Find(name) is IParameterNode retval)
		{
			retval.SetValue(value, paramFormat);
			return retval;
		}

		var index = paramFormat == ParameterFormat.Copy
			? template.FindCopyParameter(false)
			: -1;
		if (index == -1)
		{
			value = TrimValue(value, paramFormat);
			retval = template.Factory.ParameterNodeFromParts(name, value);
			template.Parameters.Add(retval);
			return retval;
		}

		var previous = template.Parameters[index];
		retval = template.Factory.ParameterNodeFromOther(previous, name, value);
		template.Parameters.Insert(index + 1, retval);
		return retval;
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
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(mask);
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
		while (offset < limit && mask.Contains(text[offset], StringComparison.Ordinal))
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
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(mask);
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
		while (offset >= limit && mask.Contains(text[offset], StringComparison.Ordinal))
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

	private static string TrimValue(string? value, ParameterFormat paramFormat)
	{
		value ??= string.Empty;
		return paramFormat switch
		{
			ParameterFormat.Verbatim => value,
			ParameterFormat.PackedTrail => value.TrimEnd(),
			ParameterFormat.Packed => value.Trim(),
			ParameterFormat.OnePerLine => value.TrimEnd() + '\n',
			ParameterFormat.Copy => value.Trim(),
			_ => value,
		};
	}
	#endregion
}