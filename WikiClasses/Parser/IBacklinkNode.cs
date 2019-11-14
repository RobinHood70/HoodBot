namespace RobinHood70.WikiClasses.Parser
{
	/// <summary>Interface for links and transclusions.</summary>
	public interface IBacklinkNode : IWikiNode
	{
		#region Properties

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		NodeCollection Parameters { get; }

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		NodeCollection Title { get; }
		#endregion
	}
}