namespace RobinHood70.WikiClasses.Parser
{
	// Since we've eliminated TreeNode, we need something that'll break up Name and Value during AddLiteral, so this is it. It is, in essence, a null node, since it stores no data.
	public class EqualsNode : INodeBase
	{
		#region Constructors
		public EqualsNode()
		{
		}
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => "=";
		#endregion
	}
}
