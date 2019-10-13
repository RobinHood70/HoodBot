namespace RobinHood70.WikiClasses.Parser
{
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

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
		/// <item>If set to <see langword="true"/> , returns only the value of each node, ignoring comments and the like. This is most useful for finding named items.</item>
		/// <item>If set to false, returns all nodes in the collection. This is most useful for editing the collection.</item>
		/// </list></param>
		public WikiTextVisitor(bool valuesOnly) => this.valuesOnly = valuesOnly;
		#endregion

		#region Public Static Methods

		/// <summary>Returns the raw text for a node or node collection.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The raw text for a node or node collection.</returns>
		public static string Raw(IWikiNode node) => RawVisitor.Build(node);

		/// <summary>Returns the value text for a node or node collection.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The value text for a node or node collection.</returns>
		public static string Value(IWikiNode node) => ValueVisitor.Build(node);
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
		#endregion

		#region IVisitor Methods

		/// <summary>Visits the specified <see cref="ArgumentNode"/>.</summary>
		/// <param name="node">The node.</param>
		/// <remarks>In values-only mode, this returns the default value (<c>{{{...|default}}}</c>) if one is available; otherwise, the full text of the argument itself (<c>{{{argument}}}</c>).</remarks>
		public void Visit(ArgumentNode node)
		{
			ThrowNull(node, nameof(node));
			if (this.valuesOnly && node.DefaultValue != null)
			{
				node.DefaultValue.Accept(this);
				return;
			}

			this.builder.Append("{{{");
			node.Name.Accept(this);
			node.DefaultValue?.Accept(this);
			if (!this.valuesOnly)
			{
				foreach (var value in node.ExtraValues)
				{
					value.Accept(this);
				}
			}

			this.builder.Append("}}}");
		}

		/// <summary>Visits the specified <see cref="CommentNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(CommentNode node)
		{
			if (!this.valuesOnly)
			{
				ThrowNull(node, nameof(node));
				this.builder.Append(node.Comment);
			}
		}

		/// <summary>Visits the specified <see cref="EqualsNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(EqualsNode node) => this.builder.Append('=');

		/// <summary>Visits the specified <see cref="HeaderNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(HeaderNode node) => node?.Title.Accept(this);

		/// <summary>Visits the specified <see cref="IgnoreNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(IgnoreNode node)
		{
			if (!this.valuesOnly)
			{
				ThrowNull(node, nameof(node));
				this.builder.Append(node.Value);
			}
		}

		/// <summary>Visits the specified <see cref="LinkNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(LinkNode node)
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

		/// <summary>Visits the specified <see cref="NodeCollection"/>.</summary>
		/// <param name="nodes">The node collection.</param>
		public void Visit(NodeCollection nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="ParameterNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(ParameterNode node)
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

		/// <summary>Visits the specified <see cref="TagNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(TagNode node)
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

		/// <summary>Visits the specified <see cref="TemplateNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(TemplateNode node)
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

		/// <summary>Visits the specified <see cref="TextNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(TextNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append(node.Text);
		}
		#endregion
	}
}
