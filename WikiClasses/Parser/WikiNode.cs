namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using RobinHood70.WikiClasses.Properties;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents common functions to all nodes in the wikitext parser.</summary>
	public abstract class WikiNode
	{
		#region Fields
		private WikiNode parent;
		#endregion

		#region Constructors
		public WikiNode()
		{
		}
		#endregion

		#region Public Properties
		public WikiNode Parent
		{
			get => this.parent;
			set
			{
				if (value == null || this.parent == null)
				{
					this.parent = value;
				}
				else
				{
					throw new InvalidOperationException(CurrentCulture(Resources.MultipleCalls, nameof(this.Parent)));
				}
			}
		}
		#endregion

		#region Public Abstract Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public abstract void Accept(INodeVisitor visitor);
		#endregion
	}
}