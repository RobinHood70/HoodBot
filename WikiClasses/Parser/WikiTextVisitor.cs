namespace RobinHood70.WikiClasses.Parser
{
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

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
		public WikiTextVisitor(bool valuesOnly) => this.valuesOnly = valuesOnly;
		#endregion

		#region Public Static Methods
		public static string Raw(IWikiNode node) => RawVisitor.Build(node);

		public static string Value(IWikiNode node) => ValueVisitor.Build(node);
		#endregion

		#region Public Methods
		public string Build(IWikiNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Clear();
			node.Accept(this);
			return this.builder.ToString();
		}
		#endregion

		#region IVisitor Methods
		public void Visit(ArgumentNode node)
		{
			ThrowNull(node, nameof(node));
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

		public virtual void Visit(CommentNode node)
		{
			ThrowNull(node, nameof(node));
			if (!this.valuesOnly)
			{
				this.builder.Append(node.Comment);
			}
		}

		public void Visit(EqualsNode node) => this.builder.Append('=');

		public void Visit(HeaderNode node) => node?.Title.Accept(this);

		public virtual void Visit(IgnoreNode node)
		{
			ThrowNull(node, nameof(node));
			if (!this.valuesOnly)
			{
				this.builder.Append(node.Value);
			}
		}

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

		public void Visit(NodeCollection nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

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

		public virtual void Visit(TagNode node)
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

		public void Visit(TextNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append(node.Text);
		}
		#endregion
	}
}
