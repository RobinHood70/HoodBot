namespace RobinHood70.WikiClasses.Parser.Nodes
{
	public class HeaderNode : INodeBase
	{
		#region Constructors
		public HeaderNode(int index, int level, NodeCollection title)
		{
			this.Index = index;
			this.Level = level;
			this.Title = title;
		}
		#endregion

		#region Public Properties
		public bool Confirmed { get; set; }

		public int Index { get; set; }

		public int Level { get; set; }

		public NodeCollection Title { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => "h" + this.Level.ToStringInvariant();
		#endregion
	}
}
