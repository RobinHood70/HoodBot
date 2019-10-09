namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	public class ParameterNode : WikiNode, IEnumerable<NodeCollection>
	{
		#region Constructors
		public ParameterNode(int index, NodeCollection value)
		{
			this.Index = index;
			this.Value = value ?? throw ArgumentNull(nameof(value));
			value.SetParent(this);
		}

		public ParameterNode(NodeCollection name, NodeCollection value)
		{
			this.Name = name ?? throw ArgumentNull(nameof(name));
			this.Value = value ?? throw ArgumentNull(nameof(value));
			name.SetParent(this);
			value.SetParent(this);
		}
		#endregion

		#region Public Properties
		public int Index { get; set; }

		public NodeCollection Name { get; }

		public NodeCollection Value { get; }
		#endregion

		#region Public Methods
		public override void Accept(INodeVisitor visitor) => visitor?.Visit(this);

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
		public override string ToString() => "<part>";
		#endregion
	}
}
