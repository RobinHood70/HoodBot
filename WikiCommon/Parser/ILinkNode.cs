namespace RobinHood70.WikiCommon.Parser;

/// <summary>Represents a link, including embedded images.</summary>
public interface ILinkNode : IBacklinkNode, IWikiNode
{
	// Deliberately empty, as there's no new functionality to add, but ILinkNode, ITemplateNode, and IBacklinkNode logically represent different things.
}