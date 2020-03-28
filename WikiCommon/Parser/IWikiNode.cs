namespace RobinHood70.WikiCommon.Parser
{
	using System.Collections.Generic;

	/// <summary>Represents common functions to all nodes in the wikitext parser.</summary>
	public interface IWikiNode
	{
		#region Properties

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		IEnumerable<NodeCollection> NodeCollections { get; }
		#endregion

		#region Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		void Accept(IWikiNodeVisitor visitor);
		#endregion
	}
}