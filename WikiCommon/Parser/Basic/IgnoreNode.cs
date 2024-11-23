namespace RobinHood70.WikiCommon.Parser.Basic;

using RobinHood70.WikiCommon.Parser;

/// <summary>Represents a blob of text that should be ignored. Depending on the parser's configuration, this can be the entire text of an <c>include</c>/<c>noinclude</c> block, the text outside of an <c>onlyinclude</c>, or just the tags themselves.</summary>
/// <remarks>Initializes a new instance of the <see cref="IgnoreNode"/> class.</remarks>
/// <param name="value">The value.</param>
public class IgnoreNode(string value) : IIgnoreNode
{
	#region Public Properties

	/// <inheritdoc/>
	public string Value { get; } = value;
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
	#endregion

	#region Public Override Methods

	/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
	/// <returns>A <see cref="string"/> that represents this instance.</returns>
	public override string ToString() => this.Value;
	#endregion
}