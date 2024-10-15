namespace RobinHood70.WikiCommon.Parser
{
	using System.Collections.Generic;

	/// <summary>Methods for wiki nodes that have wiki nodes.</summary>
	public interface IParentNode
	{
		#region Properties

		/// <summary>Gets a factory for creating new child nodes in this node.</summary>
		/// <returns>The <see cref="IWikiNodeFactory"/> that created this instance and should be used to create child nodes.</returns>
		IWikiNodeFactory Factory { get; }

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		IEnumerable<WikiNodeCollection> NodeCollections { get; }
		#endregion
	}
}