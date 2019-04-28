namespace RobinHood70.WikiClasses.Parser.Nodes
{
	public class HeaderNode : INodeBase
	{
		public HeaderNode(int level, int index, NodeCollection title)
		{
			this.Level = level;
			this.Index = index;
			this.Title = title;
		}

		public bool Confirmed { get; set; }

		public string EqualsSigns => new string('=', this.Level);

		public int Index { get; set; }

		public int Level { get; set; }

		public NodeCollection Title { get; set; }

		#region Public Override Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => "h" + this.EqualsSigns;
		#endregion
	}
}
