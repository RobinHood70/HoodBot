namespace RobinHood70.WikiClasses.Parser
{
	/// <summary>Represents the separator between name and value.</summary>
	public class EqualsNode : IWikiNode
	{
		#region Constructors
		private EqualsNode()
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets an instance of the equals node.</summary>
		/// <value>The instance.</value>
		public static EqualsNode Instance { get; } = new EqualsNode();
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => "=";
		#endregion
	}
}
