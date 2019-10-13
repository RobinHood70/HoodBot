namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a generalized node visitor, which can be used as a base class for other visitors.</summary>
	/// <seealso cref="IWikiNodeVisitor" />
	public class NodeVisitor : IWikiNodeVisitor
	{
		#region Public Methods

		/// <summary>Builds the specified nodes into wiki text.</summary>
		/// <param name="nodes">The nodes to build.</param>
		public virtual void Build(IEnumerable<IWikiNode> nodes)
		{
			foreach (var node in nodes ?? throw ArgumentNull(nameof(nodes)))
			{
				node.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="ArgumentNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(ArgumentNode node)
		{
			ThrowNull(node, nameof(node));
			foreach (var value in node)
			{
				value.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="CommentNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(CommentNode node)
		{
		}

		/// <summary>Visits the specified <see cref="EqualsNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(EqualsNode node)
		{
		}

		/// <summary>Visits the specified <see cref="HeaderNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(HeaderNode node)
		{
			ThrowNull(node, nameof(node));
			this.Visit(node.Title);
		}

		/// <summary>Visits the specified <see cref="IgnoreNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(IgnoreNode node)
		{
		}

		/// <summary>Visits the specified <see cref="LinkNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(LinkNode node)
		{
			ThrowNull(node, nameof(node));
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="NodeCollection"/>.</summary>
		/// <param name="nodes">The node collection.</param>
		public virtual void Visit(NodeCollection nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="ParameterNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(ParameterNode node)
		{
			ThrowNull(node, nameof(node));
			node.Name?.Accept(this);
			node.Value?.Accept(this);
		}

		/// <summary>Visits the specified <see cref="TagNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(TagNode node)
		{
		}

		/// <summary>Visits the specified <see cref="TemplateNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(TemplateNode node)
		{
			ThrowNull(node, nameof(node));
			node.Title.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="TextNode"/>.</summary>
		/// <param name="node">The node.</param>
		public virtual void Visit(TextNode node)
		{
		}
		#endregion
	}
}