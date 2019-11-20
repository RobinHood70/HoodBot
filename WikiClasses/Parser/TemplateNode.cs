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

		/// <summary>Returns the value of the last parameter with the specified name.</summary>
		/// <param name="parameterName">Name of the parameter.</param>
		/// <returns>The value of the last parameter with the specified name.</returns>
		public string? ValueOf(string parameterName)
		{
			var param = this.FindParameter(parameterName);
			return param == null ? null : WikiTextVisitor.Value(param.Value);
		}

		/// <summary>Converts all parameter names to their corresponding text with values remaining as <see cref="NodeCollection"/>s.</summary>
		/// <returns>A dictionary of names and values.</returns>
		public Dictionary<string, NodeCollection> ParameterDictionary()
		{
			// TODO: Parameter-based methods are very primitive for now, just to get the basics working. Needs more work.
			var retval = new Dictionary<string, NodeCollection>();
			foreach (ParameterNode parameter in this.Parameters)
			{
				retval.Add(parameter.NameToText(), parameter.Value);
			}

			return retval;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "{{Template}}" : $"{{Template|Count = {this.Parameters.Count}}}";
		#endregion
	}
}