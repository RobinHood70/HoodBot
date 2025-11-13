namespace RobinHood70.WikiCommon.Parser.Basic;

using RobinHood70.CommonCode;
using RobinHood70.WikiCommon.Parser;

/// <summary>Represents an unparsed tag, such as pre or nowiki.</summary>
/// <remarks>Initializes a new instance of the <see cref="TagNode"/> class.</remarks>
/// <param name="name">The name.</param>
/// <param name="attributes">The attributes.</param>
/// <param name="innerText">The inner text.</param>
/// <param name="close">The close.</param>
public class TagNode(string name, string? attributes, string? innerText, string? close) : ITagNode
{
	#region Public Properties

	/// <summary>Gets or sets the tag's attributes.</summary>
	/// <value>The attributes.</value>
	public string? Attributes { get; set; } = attributes;

	/// <summary>Gets or sets the close tag.</summary>
	/// <value>The close tag.</value>
	/// <remarks>
	/// Note that this is a full close tag, including the surrounding &lt;/...&gt;
	/// If this is <see langword="null"/>, the tag is self-closed.
	/// </remarks>
	public string? Close { get; set; } = close;

	/// <summary>Gets or sets the inner text.</summary>
	/// <value>The unparsed inner text.</value>
	public string? InnerText { get; set; } = innerText;

	/// <summary>Gets or sets the tag name.</summary>
	/// <value>The tag name.</value>
	/// <remarks>Note that this is NOT a full open tag, it's just the name.</remarks>
	public string Name { get; set; } = name;

	/// <summary>Gets a value indicating whether the tag is self-closed.</summary>
	/// <value><see langword="true"/> if this is a self-closed tag; otherwise, <see langword="false"/>.</value>
	public bool SelfClosed => this.Close == null;
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
	#endregion

	#region Public Override Methods

	/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
	/// <returns>A <see cref="string"/> that represents this instance.</returns>
	public override string ToString() => this.SelfClosed ? $"<{this.Name}{this.Attributes}>" : $"<{this.Name}{this.Attributes}>{this.InnerText.Ellipsis(10)}{this.Close}";
	#endregion
}