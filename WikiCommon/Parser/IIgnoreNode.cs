namespace RobinHood70.WikiCommon.Parser;

/// <summary>Represents a blob of text that should be ignored. Depending on the parser's configuration, this can be the entire text of an <c>include</c>/<c>noinclude</c> block, the text outside of an <c>onlyinclude</c>, or just the tags themselves.</summary>
public interface IIgnoreNode : IWikiNode
{
	/// <summary>Gets the value.</summary>
	/// <value>The value.</value>
	string Value { get; }
}