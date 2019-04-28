namespace RobinHood70.WikiClasses.Parser
{
	using System.Text;
	using RobinHood70.WikiClasses.Parser.Nodes;
	using static RobinHood70.WikiCommon.Globals;

	public class WikiTextVisitor : IVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new StringBuilder();
		private readonly bool valuesOnly;
		#endregion

		#region Constructors
		public WikiTextVisitor(bool valuesOnly) => this.valuesOnly = valuesOnly;
		#endregion

		#region Public Methods
		public string Build(INodeBase node)
		{
			this.builder.Clear();
			node.Accept(this);
			return this.builder.ToString();
		}
		#endregion

		#region IVisitor Methods
		public virtual void Visit(CommentNode node)
		{
			if (!this.valuesOnly)
			{
				this.builder.Append(node?.Comment);
			}
		}

		public void Visit(EqualsNode node) => this.builder.Append('=');

		public void Visit(HeaderNode node)
		{
			ThrowNull(node, nameof(node));
			this.builder.Append(node.EqualsSigns);
			node.Title.Accept(this);
			this.builder.Append(node.EqualsSigns);
		}

		public virtual void Visit(IgnoreNode node)
		{
			if (!this.valuesOnly)
			{
				this.builder.Append(node.Value);

			}
		}

		public void Visit(LinkNode node)
		{
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
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		public void Visit(ParameterNode node)
		{
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
			if (!this.valuesOnly)
			{
				this.builder
					.Append('<')
					.Append(node.Name)
					.Append(node.Attributes)
					.Append('>')
					.Append(node.InnerText)
					.Append(node.Close);
			}
		}

		public void Visit(TemplateNode node)
		{
			this.builder.Append("{{");
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}

			this.builder.Append("}}");
		}

		public void Visit(TextNode node) => this.builder.Append(node?.Text);
		#endregion
	}
}
