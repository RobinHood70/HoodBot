namespace RobinHood70.WikiCommon.Parser;

using System.Collections.Generic;

/// <summary>Represents an <see cref="IWikiNode"/> visitor.</summary>
public interface IWikiNodeVisitor
{
	/// <summary>Visits the specified <see cref="IArgumentNode"/>.</summary>
	/// <param name="argument">The argument node.</param>
	void Visit(IArgumentNode argument);

	/// <summary>Visits the specified <see cref="ICommentNode"/>.</summary>
	/// <param name="comment">The comment node.</param>
	void Visit(ICommentNode comment);

	/// <summary>Visits the specified <see cref="IHeaderNode"/>.</summary>
	/// <param name="header">The header node.</param>
	void Visit(IHeaderNode header);

	/// <summary>Visits the specified <see cref="IIgnoreNode"/>.</summary>
	/// <param name="ignore">The ignore node.</param>
	void Visit(IIgnoreNode ignore);

	/// <summary>Visits the specified <see cref="ILinkNode"/>.</summary>
	/// <param name="link">The link node.</param>
	void Visit(ILinkNode link);

	/// <summary>Visits the specified node collection.</summary>
	/// <param name="nodes">The node collection.</param>
	void Visit(IEnumerable<IWikiNode> nodes);

	/// <summary>Visits the specified <see cref="IParameterNode"/>.</summary>
	/// <param name="parameter">The parameter node.</param>
	void Visit(IParameterNode parameter);

	/// <summary>Visits the specified <see cref="ITagNode"/>.</summary>
	/// <param name="tag">The tag node.</param>
	void Visit(ITagNode tag);

	/// <summary>Visits the specified <see cref="ITemplateNode"/>.</summary>
	/// <param name="template">The template node.</param>
	void Visit(ITemplateNode template);

	/// <summary>Visits the specified <see cref="ITextNode"/>.</summary>
	/// <param name="text">The text node.</param>
	void Visit(ITextNode text);
}