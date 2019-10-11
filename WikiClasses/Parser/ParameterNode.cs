namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	public class ParameterNode : IWikiNode, IEnumerable<NodeCollection>
	{
		#region Constructors
		public ParameterNode(int index, IEnumerable<IWikiNode> value)
		{
			this.Index = index;
			this.Value = new NodeCollection(this, value ?? throw ArgumentNull(nameof(value)));
		}

		public ParameterNode(IEnumerable<IWikiNode> name, IEnumerable<IWikiNode> value)
		{
			this.Name = new NodeCollection(this, name ?? throw ArgumentNull(nameof(name)));
			this.Value = new NodeCollection(this, value ?? throw ArgumentNull(nameof(value)));
		}
		#endregion

		#region Public Properties
		public int Index { get; set; }

		public NodeCollection Name { get; }

		public NodeCollection Value { get; }
		#endregion

		#region Public Methods
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		public IEnumerator<NodeCollection> GetEnumerator()
		{
			if (this.Name != null)
			{
				yield return this.Name;
			}

			yield return this.Value;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods
		public override string ToString() => "|" + (this.Index == 0 ? "name=" : string.Empty) + "value";
		#endregion
	}
}
