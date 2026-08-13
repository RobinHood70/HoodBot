namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RobinHood70.CommonCode;

/// <summary>Represents a link, including embedded images.</summary>
public class LinkNode : ITitleNode, IWikiNode
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
	/// <param name="factory">The factory to use when creating new nodes.</param>
	/// <param name="title">The title.</param>
	/// <param name="text">The display text (with pipes included as text).</param>
	public LinkNode(
		[NotNull, ValidatedNotNull] IWikiNodeFactory factory,
		[NotNull, ValidatedNotNull] IEnumerable<IWikiNode> title,
		[NotNull, ValidatedNotNull] IEnumerable<IWikiNode> text)
	{
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(title);
		ArgumentNullException.ThrowIfNull(text);
		this.TitleNodes = new WikiNodeCollection(factory, title);
		this.Text = new WikiNodeCollection(factory, text);
	}
	#endregion

	#region Public Properties

	/// <inheritdoc/>
	public IWikiNodeFactory Factory => this.TitleNodes.Factory;

	/// <inheritdoc/>
	public IEnumerable<WikiNodeCollection> NodeCollections
	{
		get
		{
			yield return this.TitleNodes;
			yield return this.Text;
		}
	}

	/// <summary>Gets the display text or image parameters for the link.</summary>
	/// <value>The display text or image parameters.</value>
	/// <remarks>
	/// A node count of 0 should be interpreted as no display text section at all, since an empty display text would be the pipe trick, which is invalid on its own.
	///
	/// Image parameters should be a single, pipe-separated value.
	/// </remarks>
	public WikiNodeCollection Text { get; }

	/// <inheritdoc/>
	public WikiNodeCollection TitleNodes { get; }
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
	#endregion

	#region Public Override Methods

	/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
	/// <returns>A <see cref="string"/> that represents this instance.</returns>
	public override string ToString() => this.Text.Count == 0 ? "[[Link]]" : $"[[Link|Text]]";
	#endregion
}