namespace RobinHood70.WikiClasses.Parser.Nodes
{
	/// <summary>Represents common functions to all nodes in the wikitext parser.</summary>
	public interface INodeBase
	{
		#region Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		void Accept(IVisitor visitor);
		#endregion
	}
}