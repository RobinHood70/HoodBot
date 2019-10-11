namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	public class NodeVisitor : IWikiNodeVisitor
	{
		#region Public Methods
		public virtual void Build(IEnumerable<IWikiNode> nodes)
		{
			foreach (var node in nodes ?? throw ArgumentNull(nameof(nodes)))
			{
				node.Accept(this);
			}
		}

		public virtual void Visit(ArgumentNode node)
		{
			ThrowNull(node, nameof(node));
			foreach (var value in node)
			{
				value.Accept(this);
			}
		}

		public virtual void Visit(CommentNode node)
		{
		}

		public virtual void Visit(EqualsNode node)
		{
		}

		public virtual void Visit(HeaderNode node)
		{
			ThrowNull(node, nameof(node));
			this.Visit(node.Title);
		}

		public virtual void Visit(IgnoreNode node)
		{
		}

		public virtual void Visit(LinkNode node)
		{
			ThrowNull(node, nameof(node));
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}
		}

		public virtual void Visit(NodeCollection nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		public virtual void Visit(ParameterNode node)
		{
			ThrowNull(node, nameof(node));
			node.Name?.Accept(this);
			node.Value?.Accept(this);
		}

		public virtual void Visit(TagNode node)
		{
		}

		public virtual void Visit(TemplateNode node)
		{
			ThrowNull(node, nameof(node));
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}
		}

		public virtual void Visit(TextNode node)
		{
		}
		#endregion
	}
}