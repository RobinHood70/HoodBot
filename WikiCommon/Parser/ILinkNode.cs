namespace RobinHood70.WikiCommon.Parser;

/// <summary>Represents a link, including embedded images.</summary>
public interface ILinkNode : ITitleNode, IWikiNode
{
	/// <summary>Gets the display text or image parameters for the link.</summary>
	/// <value>The display text or image parameters.</value>
	/// <remarks>
	/// A node count of 0 should be interpreted as no display text section at all, since an empty display text would be the pipe trick, which is invalid on its own.
	///
	/// Image parameters should be a single, pipe-separated value.
	/// </remarks>
	WikiNodeCollection Text { get; }
}