namespace RobinHood70.WikiCommon.Parser;

/// <summary>Represents a block of text.</summary>
public interface ITextNode : IWikiNode
{
	/// <summary>Gets or sets the text.</summary>
	/// <value>The text.</value>
	string Text { get; set; }
}