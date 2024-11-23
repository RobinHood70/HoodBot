namespace RobinHood70.WikiCommon.Parser;

/// <summary>Represents a wikitext (HTML) comment.</summary>
public interface ICommentNode : IWikiNode
{
	/// <summary>Gets or sets the comment text.</summary>
	/// <value>The comment text.</value>
	string Comment { get; set; }
}