namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	public class ArgumentNode : WikiNode, IEnumerable<NodeCollection>
	{
		#region Constructors
		public ArgumentNode(bool atLineStart, NodeCollection title, IList<ParameterNode> allValues)
		{
			this.AtLineStart = atLineStart;
			this.Name = title;
			this.AllValues = new ReadOnlyCollection<ParameterNode>(allValues);
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<ParameterNode> AllValues { get; }

		public bool AtLineStart { get; }

		public NodeCollection DefaultValue => this.AllValues[0].Value;

		public NodeCollection Name { get; }
		#endregion

		#region Public Methods
		public override void Accept(INodeVisitor visitor) => visitor?.Visit(this);

		public IEnumerator<NodeCollection> GetEnumerator()
		{
			if (this.Name != null)
			{
				yield return this.Name;
			}

			foreach (var value in this.AllValues)
			{
				foreach (var valueNode in value)
				{
					yield return valueNode;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods
		public override string ToString() => "<tplarg>";
		#endregion
	}
}
