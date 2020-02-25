namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;

	/// <summary>Interface for links and transclusions.</summary>
	public interface IBacklinkNode : IWikiNode
	{
		#region Properties

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		IList<ParameterNode> Parameters { get; }

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		NodeCollection Title { get; }
		#endregion
	}
}