namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;

	public interface IBacklinkNode : IEnumerable<NodeCollection>
	{
		#region Properties
		IList<ParameterNode> Parameters { get; }

		NodeCollection Title { get; }
		#endregion
	}
}