namespace RobinHood70.WikiClasses.Parser.Nodes
{
	using System.Collections.Generic;

	public interface IBacklinkNode : INodeBase
	{
		#region Properties
		IList<ParameterNode> Parameters { get; }

		NodeCollection Title { get; }
		#endregion
	}
}