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
	#region Fields
	private string? attributes = attributes;
	private string? close = close;
	private string? innerText = innerText;
	private string name = name;
	#endregion

	#region Public Properties

	/// <summary>Gets or sets the tag's attributes.</summary>
	/// <value>The attributes.</value>
	public string? Attributes
	{
		get => this.attributes;
		set
		{
			if (!value.OrdinalEquals(this.attributes))
			{
				this.attributes = value;
			}
		}
	}

	/// <summary>Gets or sets the close tag.</summary>
	/// <value>The close tag.</value>
	public string? Close // Note that this is a full close tag, including the surrounding </...>.
	{
		get => this.close;
		set
		{
			if (!value.OrdinalEquals(this.close))
			{
				this.close = value;
			}
		}
	}

	/// <summary>Gets or sets the inner text.</summary>
	/// <value>The unparsed inner text.</value>
	public string? InnerText
	{
		get => this.innerText;
		set
		{
			if (!value.OrdinalEquals(this.innerText))
			{
				this.innerText = value;
			}
		}
	}

	/// <summary>Gets or sets the tag name.</summary>
	/// <value>The tag name.</value>
	public string Name // Note that this is NOT a full open tag, it's just the name.
	{
		get => this.name;
		set
		{
			if (!value.OrdinalEquals(this.name))
			{
				this.name = value;
			}
		}
	}

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