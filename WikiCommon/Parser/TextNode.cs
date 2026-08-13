namespace RobinHood70.WikiCommon.Parser;

using System.ComponentModel;
using RobinHood70.CommonCode;

/// <summary>Represents a block of text.</summary>
/// <remarks>Initializes a new instance of the <see cref="TextNode"/> class.</remarks>
/// <param name="text">The text.</param>
public class TextNode([Localizable(false)] string text) : IWikiNode
{
	#region Public Properties

	/// <summary>Gets or sets the text.</summary>
	/// <value>The text.</value>
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