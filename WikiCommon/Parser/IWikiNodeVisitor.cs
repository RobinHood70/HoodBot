namespace RobinHood70.WikiCommon.Parser
{
	/// <summary>Represents an <see cref="IWikiNode"/> visitor.</summary>
	public interface IWikiNodeVisitor
	{
		/// <summary>Visits the specified <see cref="IArgumentNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(IArgumentNode node);

		/// <summary>Visits the specified <see cref="ICommentNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ICommentNode node);

		/// <summary>Visits the specified <see cref="IHeaderNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(IHeaderNode node);

		/// <summary>Visits the specified <see cref="IIgnoreNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(IIgnoreNode node);

		/// <summary>Visits the specified <see cref="ILinkNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ILinkNode node);

		/// <summary>Visits the specified <see cref="WikiNodeCollection"/>.</summary>
		/// <param name="nodes">The node collection.</param>
		void Visit(WikiNodeCollection nodes);

		/// <summary>Visits the specified <see cref="IParameterNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(IParameterNode node);

		/// <summary>Visits the specified <see cref="ITagNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ITagNode node);

		/// <summary>Visits the specified <see cref="ITemplateNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ITemplateNode node);

		/// <summary>Visits the specified <see cref="ITextNode"/>.</summary>
		/// <param name="node">The node.</param>
		void Visit(ITextNode node);
	}
}