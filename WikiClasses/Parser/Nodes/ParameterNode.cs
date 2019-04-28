namespace RobinHood70.WikiClasses.Parser.Nodes
{
	public class ParameterNode : INodeBase
	{
		#region Constructors
		public ParameterNode(int index, NodeCollection value)
			: this(index, null, value)
		{
		}

		public ParameterNode(NodeCollection name, NodeCollection value)
			: this(0, name, value)
		{
		}

		private ParameterNode(int index, NodeCollection name, NodeCollection value)
		{
			this.Index = index;
			this.Name = name;
			this.Value = value;
		}
		#endregion

		#region Public Properties
		public int Index { get; set; }

		public NodeCollection Name { get; set; }

		public NodeCollection Value { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => "<part>";
		#endregion
	}
}
