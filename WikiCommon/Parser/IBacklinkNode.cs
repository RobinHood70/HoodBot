namespace RobinHood70.WikiCommon.Parser;

using System.Collections.Generic;

/// <summary>Interface for links and transclusions.</summary>
public interface IBacklinkNode : IWikiNode, IParentNode
{
	#region Properties

	/// <summary>Gets the parameters.</summary>
	/// <value>The parameters.</value>
	IList<IParameterNode> Parameters { get; }

	/// <summary>Gets the title.</summary>
	/// <value>The title.</value>
	WikiNodeCollection TitleNodes { get; }
	#endregion
}