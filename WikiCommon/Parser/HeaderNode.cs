namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;
using System.ComponentModel;

/// <summary>Represents a header.</summary>
public class HeaderNode : IWikiNode, IParentNode
{
	// TODO: Rejig this so that header node is strictly the header with no trailing space. GetInnerText can then be removed since this will only ever be storing the interior text. Will need to look closely at HeaderElement, though, to make sure fallback unwikifying isn't affected.
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
	/// <param name="factory">The factory to use when creating new nodes.</param>
	/// <param name="level">The level.</param>
	/// <param name="text">The text of the header.</param>
	/// <param name="comment">Any text that came after the close ==.</param>
	public HeaderNode(IWikiNodeFactory factory, int level, [Localizable(false)] IEnumerable<IWikiNode> text, IEnumerable<IWikiNode> comment)
	{
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(text);
		this.Factory = factory;
		this.Level = level;
		this.Title = new WikiNodeCollection(factory, text);
		this.Comment = new WikiNodeCollection(factory, comment ?? []);
	}
	#endregion

	#region Public Properties

	/// <summary>Gets any text that appeared after the ==.</summary>
	public WikiNodeCollection Comment { get; }

	/// <summary>Gets or sets a value indicating whether this <see cref="HeaderNode"/> is confirmed (direct text) or possible (template or argument).</summary>
	/// <value><see langword="true"/> if confirmed; otherwise, <see langword="false"/>.</value>
	public bool Confirmed { get; set; }

	/// <inheritdoc/>
	public IWikiNodeFactory Factory { get; }

	/// <summary>Gets the level.</summary>
	/// <value>The level. This is equal to the number of visible equals signs.</value>
	public int Level { get; }

	/// <inheritdoc/>
	public IEnumerable<WikiNodeCollection> NodeCollections
	{
		get
		{
			yield return this.Title;
		}
	}

	/// <summary>Gets the title.</summary>
	/// <value>The title.</value>
	public WikiNodeCollection Title { get; }
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
	#endregion

	#region Public Override Methods

	/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
	/// <returns>A <see cref="string"/> that represents this instance.</returns>
	public override string ToString()
	{
		var retval = (this.Title.Count == 1 && this.Title[0] is TextNode text)
			? text.Text
			: "<Header>";
		var equalsSigns = new string('=', this.Level);
		return equalsSigns + retval + equalsSigns;
	}
	#endregion
}