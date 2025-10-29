namespace RobinHood70.WikiCommon.Parser;

/// <summary>Interface for links and transclusions.</summary>
public interface IBacklinkNode : IWikiNode, IParentNode
{
	#region Properties

	/// <summary>Gets the title.</summary>
	/// <value>The title.</value>
	WikiNodeCollection TitleNodes { get; }
	#endregion
}