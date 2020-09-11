namespace RobinHood70.WikiCommon.BasicParser
{
	/// <summary>Represents an <see cref="IWikiNode"/> visitor.</summary>
	public interface IWikiNodeVisitor
	{
		/// <summary>Visits the specified <see cref="ArgumentNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ArgumentNode node);

		/// <summary>Visits the specified <see cref="CommentNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(CommentNode node);

		/// <summary>Visits the specified <see cref="HeaderNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(HeaderNode node);

		/// <summary>Visits the specified <see cref="IgnoreNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(IgnoreNode node);

		/// <summary>Visits the specified <see cref="LinkNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(LinkNode node);

		/// <summary>Visits the specified <see cref="NodeCollection"/>.</summary>
		/// <param name="nodes">The node collection.</param>
		void Visit(NodeCollection nodes);

		/// <summary>Visits the specified <see cref="ParameterNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ParameterNode node);

		/// <summary>Visits the specified <see cref="TagNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(TagNode node);

		/// <summary>Visits the specified <see cref="TemplateNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(TemplateNode node);

		/// <summary>Visits the specified <see cref="TextNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(TextNode node);
	}
}
