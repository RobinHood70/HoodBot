namespace RobinHood70.WikiCommon.Parser
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
					if (parameter.Anonymous)
					{
						yield return (++i, parameter);
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

		/// <summary>Gets the parameters with the indexed named for anonymous parameters.</summary>
		public IEnumerable<(string Name, ParameterNode Node)> ResolvedParameters
		{
			get
			{
				var anonIndex = 0;
				foreach (var parameter in this.Parameters)
				{
					var name = NameOrIndex(parameter, ref anonIndex);
					yield return (name, parameter);
				}
			}
		}

		/// <summary>Gets the template name.</summary>
		/// <value>The template name.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new TemplateNode from the provided text.</summary>
		/// <param name="text">The text of the template, including surrounding braces (<c>{{...}}</c>).</param>
		/// <returns>A new TemplateNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		public static TemplateNode FromText([Localizable(false)] string text) => NodeCollection.SingleNode<TemplateNode>(text);

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
		/// <returns>The added parameter.</returns>
		public ParameterNode Add(string name, string value) => this.Add(name, value, true);

		/// <summary>Adds a new parameter to the template. Optionally, copies the format of the previous named parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="copyFormat">Whether to copy the format of the previous parameter or use the values as provided.</param>
		/// <returns>The added parameter.</returns>
		public ParameterNode Add(string name, string value, bool copyFormat)
		{
			ParameterNode retval;
			var index = copyFormat ? this.FindCopyParameter(false) : -1;
			if (index != -1)
			{
				if (this.Find(name) != null)
				{
					throw new InvalidOperationException(CurrentCulture(Properties.Resources.ParameterExists, name));
				}

				var previous = this.Parameters[index];
				retval = ParameterNode.CopyFormatFrom(previous, name, value);
				this.Parameters.Insert(index + 1, retval);
			}
			else
			{
				retval = ParameterNode.FromParts(name, value);
				this.Parameters.Add(retval);
			}

			return retval;
		}

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The added parameter.</returns>
		public ParameterNode Add(string value) => this.Add(value, true);

		/// <summary>Adds a new anonymous parameter to the template. Copies the format of the last anonymous parameter, if there is one, then adds the parameter after it.</summary>
		/// <param name="value">The value of the parameter to add.</param>
		/// <param name="copyFormat">Whether to copy the format of the previous parameter or use the values as provided.</param>
		/// <returns>The added parameter.</returns>
		public ParameterNode Add(string value, bool copyFormat)
		{
			ParameterNode retval;
			var index = copyFormat ? this.FindCopyParameter(true) : -1;
			if (index != -1)
			{
				var previous = this.Parameters[index];
				retval = ParameterNode.CopyFormatFrom(previous, value);
				this.Parameters.Insert(index + 1, retval);
			}
			else
			{
				retval = ParameterNode.FromParts(value);
				this.Parameters.Add(retval);
			}

			return retval;
		}

		/// <summary>Changes the value of a parameter to the specified value, or adds the parameter if it doesn't exist.</summary>
		/// <param name="name">The name of the parameter to add.</param>
		/// <param name="value">The value of the parameter to add.</param>
		/// <returns>The parameter that was altered.</returns>
		public ParameterNode AddOrChange(string name, string value)
		{
			if (!(this.Find(name) is ParameterNode parameter))
			{
				return this.Add(name, value);
			}

			parameter.SetValue(value);
			return parameter;
		}

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public int FindNumberedIndex(int number)
		{
			var retval = -1;
			var i = 0;
			var name = number.ToStringInvariant();
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
				else if (string.Equals(node.NameToText(), name, StringComparison.Ordinal))
				{
					retval = index;
				}
			}

			return retval;
		}

		/// <summary>Finds a numbered parameter, whether it's anonymous or a numerically named parameter.</summary>
		/// <param name="number">The numbered parameter to search for.</param>
		/// <returns>The parameter, if found; otherwise, <see langword="null"/>.</returns>
		public ParameterNode? Find(int number)
		{
			var index = this.FindNumberedIndex(number);
			return index == -1 ? null : this.Parameters[index];
		}

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public ParameterNode? Find(params string[] parameterNames) => this.Find(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public ParameterNode? Find(bool ignoreCase, params string[] parameterNames)
		{
			ParameterNode? retval = null;
			foreach (var parameter in this.FindAll(ignoreCase, parameterNames))
			{
				retval = parameter;
			}

			return retval;
		}

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public IEnumerable<ParameterNode> FindAll(params string[] parameterNames) => this.FindAll(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public IEnumerable<ParameterNode> FindAll(IEnumerable<string> parameterNames) => this.FindAll(false, parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public IEnumerable<ParameterNode> FindAll(bool ignoreCase, params string[] parameterNames) => this.FindAll(ignoreCase, (IEnumerable<string>)parameterNames);

		/// <summary>Finds the last parameter with any of the provided names.</summary>
		/// <param name="ignoreCase">Whether to ignore case when checking parameter names.</param>
		/// <param name="parameterNames">The names of the parameters to search for.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public IEnumerable<ParameterNode> FindAll(bool ignoreCase, IEnumerable<string> parameterNames)
		{
			ThrowNull(parameterNames, nameof(parameterNames));
			var nameSet = new HashSet<string>(parameterNames, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			foreach (var (name, node) in this.ResolvedParameters)
			{
				if (nameSet.Contains(name))
				{
					yield return node;
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
		public string? RawValue(string parameterName)
		{
			var param = this.Find(parameterName);
			return param == null ? null : WikiTextVisitor.Raw(param.Value);
		}

		/// <summary>Removes any parameters with the same name/index as a later parameter.</summary>
		public void RemoveDuplicates()
		{
			var nameList = new HashSet<string>(StringComparer.Ordinal);
			var index = this.Parameters.Count;
			var anonIndex = 0;
			while (index >= 0 && index < this.Parameters.Count)
			{
				var name = NameOrIndex(this.Parameters[index], ref anonIndex);
				if (nameList.Contains(name))
				{
					this.Parameters.RemoveAt(index);
				}
				else
				{
					nameList.Add(name);
				}

				index--;
			}
		}

		/// <summary>Finds the parameters with the given name and removes it.</summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns><see langword="true"/>if any parameters were removed.</returns>
		/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
		public bool Remove(string parameterName)
		{
			var retval = false;
			var anonIndex = 0;
			for (var i = 0; i < this.Parameters.Count; i++)
			{
				if (string.Equals(NameOrIndex(this.Parameters[i], ref anonIndex), parameterName, StringComparison.Ordinal))
				{
					this.Parameters.RemoveAt(i);
					retval = true;
				}
			}

			return retval;
		}

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
			var indeces = new Dictionary<string, int>(StringComparer.Ordinal);
			var i = 0;
			foreach (var value in sortOrder)
			{
				indeces.Add(value, i);
				i++;
			}

			var sorted = new ParameterNode?[indeces.Count];
			var unsorted = new List<ParameterNode>();
			foreach (var (name, node) in this.ResolvedParameters)
			{
				var index = indeces.GetValueOrDefault(name, -1);
				if (index == -1)
				{
					unsorted.Add(node);
				}
				else
				{
					sorted[index] = node;
				}
			}

			this.Parameters.Clear();
			foreach (var parameter in sorted)
			{
				if (parameter != null)
				{
					this.Parameters.Add(parameter);
				}
			}

			foreach (var parameter in unsorted)
			{
				this.Parameters.Add(parameter);
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "{{Template}}" : $"{{Template|Count = {this.Parameters.Count}}}";
		#endregion

		#region Private STatic Methods
		private static string NameOrIndex(ParameterNode node, ref int index) => node.NameToText() ?? (++index).ToStringInvariant();
		#endregion

		#region Private Methods
		private int FindCopyParameter(bool isAnon)
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