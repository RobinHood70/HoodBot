namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

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
			this.Parameters = new NodeCollection(this, parameters ?? Array.Empty<ParameterNode>());
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
				yield return this.Parameters;
			}
		}

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		public NodeCollection Parameters { get; }

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
		public static TemplateNode FromParts(string title, IEnumerable<(string? Name, string Value)> parameters) => FromParts(title, false, parameters as IEnumerable<(string?, string)>);

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
			var previous = copyFormat ? this.Parameters.FindLastLinked<ParameterNode>(item => item.Name != null) : null;
			if (previous?.Value is ParameterNode prevValue)
			{
				if (this.FindParameter(name) != null)
				{
					throw new InvalidOperationException("Parameter exists");
				}

				this.Parameters.AddAfter(previous, ParameterNode.CopyFormatFrom(prevValue, name, value));
			}
			else
			{
				this.Parameters.AddLast(ParameterNode.FromParts(name, value));
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
			var previous = copyFormat ? this.Parameters.FindLastLinked<ParameterNode>(item => item.Name == null) : null;
			if (previous?.Value is ParameterNode prevValue)
			{
				this.Parameters.AddAfter(previous, ParameterNode.CopyFormatFrom(prevValue, value));
			}
			else
			{
				this.Parameters.AddLast(ParameterNode.FromParts(1, value));
			}
		}

		/// <summary>Finds the last parameter with the given name.</summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public ParameterNode? FindParameter(string parameterName) => this.FindParameterLinked(parameterName)?.Value as ParameterNode;

		/// <summary>Finds the last parameter with the given name.</summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns>The requested parameter or <see langword="null"/> if not found.</returns>
		public LinkedListNode<IWikiNode>? FindParameterLinked(string parameterName)
		{
			for (var node = this.Parameters.Last; node != null; node = node.Previous)
			{
				if (node.Value is ParameterNode parameter && parameter.NameToText() == parameterName)
				{
					return node;
				}
			}

			return null;
		}

		/// <summary>Parses the title and returns the trimmed value.</summary>
		/// <returns>The title.</returns>
		public string? GetTitleValue() => this.Title == null ? null : WikiTextVisitor.Value(this.Title).Trim();

		/// <summary>Returns the wiki text of the last parameter with the specified name.</summary>
		/// <param name="parameterName">Name of the parameter.</param>
		/// <returns>The value of the last parameter with the specified name.</returns>
		public string? RawValueOf(string parameterName)
		{
			var param = this.FindParameter(parameterName);
			return param == null ? null : WikiTextVisitor.Raw(param.Value);
		}

		/// <summary>Reindexes anonymous parameters.</summary>
		/// <returns>The count of anonymous parameters.</returns>
		/// <remarks>This method is temporary, until a more robust parameter collection is developed.</remarks>
		public int Reindex()
		{
			var i = 0;
			for (var current = this.Parameters.First; current != null; current = current.Next)
			{
				if (current.Value is ParameterNode param && param.Anonymous)
				{
					param.Index = ++i;
				}
			}

			return i;
		}

		/// <summary>Finds the parameters with the given name and removes it.</summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <remarks>In the event of a duplicate parameter, all parameters with the same name will be removed.</remarks>
		public void RemoveParameter(string parameterName)
		{
			for (var node = this.Parameters.Last; node != null; node = node.Previous)
			{
				if (node.Value is ParameterNode parameter && parameter.NameToText() == parameterName)
				{
					this.Parameters.Remove(node);
				}
			}
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
	}
}