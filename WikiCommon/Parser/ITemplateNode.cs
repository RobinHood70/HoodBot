namespace RobinHood70.WikiCommon.Parser;

using System.Collections.Generic;

/// <summary>Represents a link, including embedded images.</summary>
public interface ITemplateNode : IBacklinkNode, IWikiNode
{
	/// <summary>Gets the parameters.</summary>
	/// <value>The parameters.</value>
	IList<IParameterNode> Parameters { get; }
}