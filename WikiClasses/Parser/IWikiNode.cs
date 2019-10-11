namespace RobinHood70.WikiClasses.Parser
{
	/// <summary>Represents common functions to all nodes in the wikitext parser.</summary>
	public interface IWikiNode
	{
		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		void Accept(IWikiNodeVisitor visitor);
		#endregion
	}
}