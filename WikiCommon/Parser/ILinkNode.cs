namespace RobinHood70.WikiCommon.Parser;

/// <summary>Represents a link, including embedded images.</summary>
public interface ILinkNode : IBacklinkNode, IWikiNode
{
	/// <summary>Gets the parameter value.</summary>
	/// <value>The value.</value>
	/// <remarks>A node count of 0 should be interpreted as no display text section at all, since an empty display text would be the pipe trick, which is invalid on its own.</remarks>
	WikiNodeCollection Text { get; }
}