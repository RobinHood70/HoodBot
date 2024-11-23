namespace RobinHood70.WikiCommon.Parser.Basic;

using System.ComponentModel;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon.Parser;

/// <summary>Represents a block of text.</summary>
/// <remarks>Initializes a new instance of the <see cref="TextNode"/> class.</remarks>
/// <param name="text">The text.</param>
public class TextNode([Localizable(false)] string text) : ITextNode
{
	#region Public Properties

	/// <inheritdoc/>
	public string Text { get; set; } = text;
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
	#endregion

	#region Public Override Methods

	/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
	/// <returns>A <see cref="string"/> that represents this instance.</returns>
	public override string ToString() => this.Text.Ellipsis(20) ?? "<Empty>";
	#endregion
}