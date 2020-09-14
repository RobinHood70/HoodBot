namespace RobinHood70.WikiCommon.Parser
{
	using System.Collections.Generic;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Visitor to build wiki text, optionally ignoring text that has no value to the parser, such as comments or nowiki text.</summary>
	/// <seealso cref="IWikiNodeVisitor" />
	public class WikiTextVisitor : IWikiNodeVisitor
	{
		#region Static Fields
		private static readonly WikiTextVisitor RawVisitor = new WikiTextVisitor(false);
		private static readonly WikiTextVisitor ValueVisitor = new WikiTextVisitor(true);
		#endregion

		#region Fields
		private readonly StringBuilder builder = new StringBuilder();
		private readonly bool valuesOnly;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiTextVisitor"/> class.</summary>
		/// <param name="valuesOnly"><list type="bullet">
		/// <item>If set to <see langword="true"/>, returns only the value of each node, ignoring comments and the like. This is most useful for finding named items.</item>
		/// <item>If set to false, returns all nodes in the collection. This is most useful for editing the collection.</item>
		/// </list></param>
		public WikiTextVisitor(bool valuesOnly) => this.valuesOnly = valuesOnly;
		#endregion

		#region Public Static Methods

		/// <summary>Returns the raw text for a node or node collection.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The raw text for a node or node collection.</returns>
		public static string Raw(IWikiNode node) => RawVisitor.Build(node);

		/// <summary>Returns the raw text for a set of nodes.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The raw text for the specified nodes.</returns>
		public static string Raw(IEnumerable<IWikiNode> nodes) => RawVisitor.Build(nodes);

		/// <summary>Returns the value text for a node or node collection.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The value text for a node or node collection.</returns>
		public static string Value(IWikiNode node) => ValueVisitor.Build(node);

		/// <summary>Returns the value text for a node or node collection.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The value text for the specified nodes.</returns>
		public static string Value(IEnumerable<IWikiNode> nodes) => ValueVisitor.Build(nodes);
		#endregion

		#region Public Methods

		/// <summary>Builds the specified node or node collection into wiki text.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The wiki text of the collection.</returns>
		public string Build(IWikiNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Clear();
			node.Accept(this);
			return this.builder.ToString();
		}

		/// <summary>Builds the specified node or node collection into wiki text.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The wiki text of the collection.</returns>
		public string Build(IEnumerable<IWikiNode> nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			this.builder.Clear();
			foreach (var node in nodes)
			{
				node.Accept(this);
			}

			return this.builder.ToString();
		}
		#endregion

		#region IVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode node)
		{
			ThrowNull(node, nameof(node));
			if (this.valuesOnly && node.DefaultValue != null)
			{
				node.DefaultValue.Accept(this);
				return;
			}

			this.builder.Append("{{{");
			node.Name.Accept(this);
			if (node.DefaultValue != null)
			{
				this.builder.Append('|');
				node.DefaultValue.Accept(this);
			}

			if (!this.valuesOnly && node.ExtraValues != null)
			{
				foreach (var value in node.ExtraValues)
				{
					value.Accept(this);
				}
			}

			this.builder.Append("}}}");
		}

		/// <inheritdoc/>
		public void Visit(ICommentNode node)
		{
			if (!this.valuesOnly)
			{
				ThrowNull(node, nameof(node));
				this.builder.Append(node.Comment);
			}
		}

		/// <inheritdoc/>
		public void Visit(IHeaderNode node)
		{
			ThrowNull(node, nameof(node));
			node.Title.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(IIgnoreNode node)
		{
			if (!this.valuesOnly)
			{
				ThrowNull(node, nameof(node));
				this.builder.Append(node.Value);
			}
		}

		/// <inheritdoc/>
		public void Visit(ILinkNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append("[[");
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}

			this.builder.Append("]]");
		}

		/// <inheritdoc/>
		public void Visit(NodeCollection nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(IParameterNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append('|');
			if (node.Name != null)
			{
				node.Name.Accept(this);
				this.builder.Append('=');
			}

			node.Value.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(ITagNode node)
		{
			ThrowNull(node, nameof(node));
			if (!this.valuesOnly)
			{
				this.builder
					.Append('<')
					.Append(node.Name)
					.Append(node.Attributes);
				if (node.SelfClosed)
				{
					this.builder.Append("/>");
				}
				else
				{
					// TODO: Check what happens with <br> vs. <br/> vs. <br></br>. Do they all work properly?
					this.builder
						.Append('>')
						.Append(node.InnerText)
						.Append(node.Close);
				}
			}
		}

		/// <inheritdoc/>
		public void Visit(ITemplateNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append("{{");
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}

			this.builder.Append("}}");
		}

		/// <inheritdoc/>
		public void Visit(ITextNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append(node.Text);
		}
		#endregion
	}
}
