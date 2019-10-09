namespace RobinHood70.WikiClasses.Parser
{
	/// <summary>Represents common functions to all nodes in the wikitext parser.</summary>
	public abstract class WikiNode
	{
		#region Constructors
		public WikiNode()
		{
		}
		#endregion

		#region Properties
		public WikiNode Parent { get; private set; }
		#endregion

		#region Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public abstract void Accept(INodeVisitor visitor);

		public void SetParent(WikiNode parent) => this.Parent = parent;
		#endregion
	}
}