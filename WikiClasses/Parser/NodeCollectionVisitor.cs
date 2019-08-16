namespace RobinHood70.WikiClasses.Parser
{
	using RobinHood70.WikiClasses.Parser.Nodes;

	public class NodeCollectionVisitor : IVisitor
	{
		public NodeCollectionVisitor(NodeCollection nodes) => this.Nodes = nodes;

		public NodeCollection Nodes { get; }

		public virtual void Visit() => this.Visit(this.Nodes);

		public virtual void Visit(CommentNode node)
		{
		}

		public virtual void Visit(EqualsNode node)
		{
		}

		public virtual void Visit(HeaderNode node) => this.Visit(node.Title);

		public virtual void Visit(IgnoreNode node)
		{
		}

		public virtual void Visit(LinkNode node)
		{
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}
		}

		public virtual void Visit(NodeCollection nodes)
		{
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		public virtual void Visit(ParameterNode node)
		{
			node.Name?.Accept(this);
			node.Value?.Accept(this);
		}

		public virtual void Visit(TagNode node)
		{
		}

		public virtual void Visit(TemplateNode node)
		{
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}
		}

		public virtual void Visit(TextNode node)
		{
		}
	}
}