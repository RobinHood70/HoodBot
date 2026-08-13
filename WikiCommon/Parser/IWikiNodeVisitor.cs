namespace RobinHood70.WikiCommon.Parser;

using System.Collections.Generic;

/// <summary>Represents an <see cref="IWikiNode"/> visitor.</summary>
public interface IWikiNodeVisitor
{
	/// <summary>Visits the specified <see cref="ArgumentNode"/>.</summary>
	/// <param name="argument">The argument node.</param>
	void Visit(ArgumentNode argument);

	/// <summary>Visits the specified <see cref="CommentNode"/>.</summary>
	/// <param name="comment">The comment node.</param>
	void Visit(CommentNode comment);

	/// <summary>Visits the specified <see cref="HeaderNode"/>.</summary>
	/// <param name="header">The header node.</param>
	void Visit(HeaderNode header);

	/// <summary>Visits the specified <see cref="IgnoreNode"/>.</summary>
	/// <param name="ignore">The ignore node.</param>
	void Visit(IgnoreNode ignore);

	/// <summary>Visits the specified <see cref="LinkNode"/>.</summary>
	/// <param name="link">The link node.</param>
	void Visit(LinkNode link);

	/// <summary>Visits the specified node collection.</summary>
	/// <param name="nodes">The node collection.</param>
	void Visit(IEnumerable<IWikiNode> nodes);

	/// <summary>Visits the specified <see cref="ParameterNode"/>.</summary>
	/// <param name="parameter">The parameter node.</param>
	void Visit(ParameterNode parameter);

	/// <summary>Visits the specified <see cref="TagNode"/>.</summary>
	/// <param name="tag">The tag node.</param>
	void Visit(TagNode tag);

	/// <summary>Visits the specified <see cref="TemplateNode"/>.</summary>
	/// <param name="template">The template node.</param>
	void Visit(TemplateNode template);

	/// <summary>Visits the specified <see cref="TextNode"/>.</summary>
	/// <param name="text">The text node.</param>
	void Visit(TextNode text);
}