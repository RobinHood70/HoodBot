namespace RobinHood70.WikiClasses.Parser
{
	public class IgnoreNode : WikiNode
	{
		#region Constructors
		public IgnoreNode(string value) => this.Value = value;
		#endregion

		#region Public Properties
		public string Value { get; }
		#endregion

		#region Public Methods
		public override void Accept(INodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Value;
		#endregion
	}
}
