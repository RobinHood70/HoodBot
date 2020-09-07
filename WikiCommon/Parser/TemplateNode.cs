﻿namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Expand class to handle numbered parameters better (or at all, in cases like Remove).

	/// <summary>Represents a template call.</summary>
	public class TemplateNode : IWikiNode, IBacklinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TemplateNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public TemplateNode(IEnumerable<IWikiNode> title, IEnumerable<ParameterNode> parameters)
		{
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
			this.Parameters = new List<ParameterNode>(parameters ?? throw ArgumentNull(nameof(parameters)));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				yield return this.Title;
				foreach (var parameter in this.Parameters)
				{
					foreach (var nodeCollection in parameter.NodeCollections)
					{
						yield return nodeCollection;
					}
				}
			}
		}

		/// <summary>Gets a simple collection of all numbered parameters.</summary>
		/// <value>The numbered parameters.</value>
		/// <remarks>Parameters returned by this function include both fully anonymous and numerically named parameters. The index returned is not guaranteed to be unique or consecutive. For example, a template like <c>{{Test|anon1a|anon2|1=anon1b|anon3}}</c> would return, in order: 1=anon1a, 2=anon2, 1=anon1b, 3=anon3.</remarks>
		public IEnumerable<(int Index, ParameterNode Parameter)> NumberedParameters
		{
			get
			{
				var i = 0;
				foreach (var parameter in this.Parameters)
				{
					if (parameter.Name == null)
					{
						i++;
						yield return (i, parameter);
					}
					else if (int.TryParse(parameter.NameToText(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var namedNumber) && namedNumber > 0)
					{
						yield return (namedNumber, parameter);
					}
				}
			}
		}

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		public IList<ParameterNode> Parameters { get; }

		/// <summary>Gets the template name.</summary>
		/// <value>The template name.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new TemplateNode from the provided text.</summary>
		/// <param name="text">The text of the template, including surrounding braces (<c>{{...}}</c>).</param>
		/// <returns>A new TemplateNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		public static TemplateNode FromText([Localizable(false)] string text) => WikiTextParser.SingleNode<TemplateNode>(text);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new TemplateNode.</returns>
		public static TemplateNode FromParts(string title, bool onePerLine, params string[] parameters) => FromParts(title, onePerLine, parameters as IEnumerable<string>);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new TemplateNode.</returns>
		/// <remarks>Parameters added through this method will not be evaluated for equals signs in the value.</remarks>
		public static TemplateNode FromParts(string title, bool onePerLine, IEnumerable<string>? parameters)
		{
			ThrowNull(title, nameof(title));
			var sb = new StringBuilder();
			sb
				.Append("{{")
				.Append(title);
			if (parameters != null)
			{
				var addTrailingLine = false;
				foreach (var parameter in parameters)
				{
					if (onePerLine)
					{
						addTrailingLine = true;
						sb.Append('\n');
					}

					sb.Append('|');
					sb.Append(ParameterNode.EscapeNameValue(parameter));
				}

				if (addTrailingLine)
				{
					sb.Append('\n');
				}
			}

			sb.Append("}}");

			return FromText(sb.ToString());
		}

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new TemplateNode.</returns>
		/// <remarks>Parameters added through this method will not be evaluated for equals signs in the value.</remarks>
		public static TemplateNode FromParts(string title, params (string?, string)[] parameters) => FromParts(title, false, parameters as IEnumerable<(string?, string)>);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new TemplateNode.</returns>
		/// <remarks>Parameters added through this method will not be evaluated for equals signs in the value.</remarks>
		public static TemplateNode FromParts(string title, IEnumerable<(string? Name, string Value)> parameters) => FromParts(title, false, parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new TemplateNode.</returns>
		/// <remarks>Parameters added through this method will not be evaluated for equals signs in the value.</remarks>
		public static TemplateNode FromParts(string title, bool onePerLine, params (string?, string)[] parameters) => FromParts(title, onePerLine, parameters as IEnumerable<(string?, string)>);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new TemplateNode.</returns>
		/// <remarks>Parameters added through this method will not be evaluated for equals signs in the value.</remarks>
		public static TemplateNode FromParts(string title, bool onePerLine, IEnumerable<(string? Name, string Value)> parameters)
		{
			ThrowNull(title, nameof(title));
			var sb = new StringBuilder();
			sb
				.Append("{{")
				.Append(title);
			if (parameters != null)
			{
				var addTrailingLine = false;
				foreach (var (name, value) in parameters)
				{
					if (onePerLine)
					{
						addTrailingLine = true;
						sb.Append('\n');
					}

					sb.Append('|');
					if (name != null)
					{
						sb.Append(name);
						sb.Append('=');
					}

					sb.Append(ParameterNode.EscapeValue(value));
				}

				if (addTrailingLine)
				{
					sb.Append('\n');
				}
			}

			sb.Append("}}");

			return FromText(sb.ToString());
		}
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Adds a new parameter to the template. Copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		public void AddParameter(string name, string value) => this.AddParameter(name, value, true);

		/// <summary>Adds a new parameter to the template. Optionally, copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="copyFormat">Whether to copy the format of the previous parameter or use the values as provided.</param>
		public void AddParameter(string name, string value, bool copyFormat)
		{
			var index = copyFormat ? this.FindParameterIndex(false) : -1;
			if (index != -1)
			{
				if (this.FindParameter(name) != null)
				{
					throw new InvalidOperationException(CurrentCulture(Properties.Resources.ParameterExists, name));
				}

				var previous = this.Parameters[index];
				this.Parameters.Insert(index + 1, ParameterNode.CopyFormatFrom(previous, name, value));
			}
			else
			{
				this.Parameters.Add(ParameterNode.FromParts(name, value));
			}
		}

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="value">The value of the parameter to add.</param>
		public void AddParameter(string value) => this.AddParameter(value, true);

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="copyFormat">Whether to copy the format of the previous parameter or use the values as provided.</param>
		public void AddParameter(string value, bool copyFormat)
		{
			var index = copyFormat ? this.FindParameterIndex(true) : -1;
			if (index != -1)
			{
				var previous = this.Parameters[index];
				this.Parameters.Insert(index + 1, ParameterNode.CopyFormatFrom(previous, value));
			}
			else
			{
				this.Parameters.Add(ParameterNode.FromParts(value));
			}
		}

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public int FindNumberedParameterIndex(int number)
		{
			var retval = -1;
			var i = 0;
			for (var index = 0; index < this.Parameters.Count; index++)
			{
				var node = this.Parameters[index];
				if (node.Name == null)
				{
					if (++i == number)
					{
						retval = index;
					}
				}
				else if (int.TryParse(node.NameToText(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var foundNumber) && foundNumber == number)
				{
					retval = index;
				}
			}

			return retval;
		}

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public ParameterNode? FindNumberedParameter(int number)
		{
			var index = this.FindNumberedParameterIndex(number);
			return index == -1 ? null : this.Parameters[index];
		}

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public ParameterNode? FindParameter(params string[] parameterNames) => this.FindParameter(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public ParameterNode? FindParameter(bool ignoreCase, params string[] parameterNames)
		{
			foreach (var retval in this.FindParameters(ignoreCase, parameterNames))
			{
				return retval;
			}

			return null;
		}

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public IEnumerable<ParameterNode> FindParameters(params string[] parameterNames) => this.FindParameters(true, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public IEnumerable<ParameterNode> FindParameters(bool ignoreCase, params string[] parameterNames)
		{
			ThrowNull(parameterNames, nameof(parameterNames));
			var nameSet = new HashSet<string>(parameterNames, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			for (var i = this.Parameters.Count - 1; i >= 0; i--)
			{
				var parameter = this.Parameters[i];
				if (parameter.NameToText() is string name && nameSet.Contains(name))
				{
					yield return parameter;
				}
			}
		}

		/// <inheritdoc/>
		public string GetTitleValue() => WikiTextUtilities.DecodeAndNormalize(WikiTextVisitor.Value(this.Title)).Trim();

		/// <summary>Determines whether any parameters have numeric names.</summary>
		/// <returns><see langword="true"/> if the parameter collection has any names which are valid integers; otherwise, <see langword="false"/>.</returns>
		public bool HasNumericNames()
		{
			foreach (var param in this.Parameters)
			{
				if (!param.Anonymous && int.TryParse(param.NameToText(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Gets numeric parameters in order, resolving conflicts in the same manner as MediaWiki does.</summary>
		/// <param name="addMissing">Set to <see langword="true"/> if missing parameters (e.g., <c>{{Template|1=First|3=Missing2}}</c>) should be inserted as <see langword="null"/> values.</param>
		/// <returns>A read-only dictionary of the parameters.</returns>
		public IReadOnlyDictionary<int, ParameterNode?> OrderedNumericParameters(bool addMissing)
		{
			var retval = new SortedDictionary<int, ParameterNode?>();
			var highest = 0;
			foreach (var (index, parameter) in this.NumberedParameters)
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
		/// <param name="length">The length of each cluster.</param>
		/// <returns>Numeric and numerically-numbered parameters in groups of <paramref name="length"/>.</returns>
		/// <example>Using <c>ParameterCluster(2)</c> on <c>{{MyTemplate|A|1|B|2|C|2=0}}</c> would return three lists: { "A", "0" }, { "B", "2" }, and { "C", null }. In the first case, "0" is returned because of the overridden parameter <c>2=0</c>. In the last case, <see langword="null"/> is returned because the parameter has no pairing within the template call. </example>
		public IEnumerable<IList<ParameterNode?>> ParameterCluster(int length)
		{
			var parameters = this.OrderedNumericParameters(true);
			var i = 1;
			var retval = new List<ParameterNode?>();
			while (i < parameters.Count)
			{
				for (var j = 0; j < length; j++)
				{
					retval.Add(parameters[i + j]);
				}

				yield return retval;
				retval = new List<ParameterNode?>();
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

		/// <summary>Returns the wiki text of the last parameter with the specified name.</summary>
		/// <param name="parameterName">Name of the parameter.</param>
		/// <returns>The value of the last parameter with the specified name.</returns>
		public string? RawValueOf(string parameterName)
		{
			var param = this.FindParameter(parameterName);
			return param == null ? null : WikiTextVisitor.Raw(param.Value);
		}

		/// <summary>Finds the parameters with the given name and removes it.</summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
		public void RemoveParameter(string parameterName)
		{
			for (var i = this.Parameters.Count - 1; i >= 0; i--)
			{
				if (string.Equals(this.Parameters[i].NameToText(), parameterName, StringComparison.Ordinal))
				{
					this.Parameters.RemoveAt(i);
				}
			}
		}

		/// <summary>Returns the value of the last parameter with the specified name.</summary>
		/// <param name="parameterNumber">Number of the parameter.</param>
		/// <returns>The value of the last parameter with the specified name.</returns>
		public string? ValueOf(int parameterNumber)
		{
			var param = this.FindNumberedParameter(parameterNumber);
			return param == null ? null : WikiTextVisitor.Value(param.Value).Trim();
		}

		/// <summary>Returns the value of the last parameter with the specified name.</summary>
		/// <param name="parameterName">Name of the parameter.</param>
		/// <returns>The value of the last parameter with the specified name.</returns>
		public string? ValueOf(string parameterName)
		{
			var param = this.FindParameter(parameterName);
			return param == null ? null : WikiTextVisitor.Value(param.Value).Trim();
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "{{Template}}" : $"{{Template|Count = {this.Parameters.Count}}}";
		#endregion

		#region Private Methods
		private int FindParameterIndex(bool isAnon)
		{
			for (var i = this.Parameters.Count - 1; i >= 0; i--)
			{
				if (this.Parameters[i].Anonymous == isAnon)
				{
					return i;
				}
			}

			return -1;
		}
		#endregion
	}
}