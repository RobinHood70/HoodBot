namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	public class HeaderNode : IWikiNode, IEnumerable<NodeCollection>
	{
		#region Constructors
		public HeaderNode(int index, int level, IEnumerable<IWikiNode> title)
		{
			this.Index = index;
			this.Level = level;
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
		}
		#endregion

		#region Public Properties
		public bool Confirmed { get; set; }

		public int Index { get; set; }

		public int Level { get; set; }

		public NodeCollection Title { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		public IEnumerator<NodeCollection> GetEnumerator()
		{
			yield return this.Title;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods
		public override string ToString()
		{
			var equals = new string('=', this.Level);
			return equals + "Header" + equals;
		}
		#endregion
	}
}
