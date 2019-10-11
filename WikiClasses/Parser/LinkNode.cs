namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	public class LinkNode : IWikiNode, IBacklinkNode
	{
		#region Constructors
		public LinkNode(IEnumerable<IWikiNode> title, IList<ParameterNode> parameters)
		{
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
			this.Parameters = parameters ?? new List<ParameterNode>();
		}
		#endregion

		#region Public Properties
		public IList<ParameterNode> Parameters { get; }

		public NodeCollection Title { get; }
		#endregion

		#region Public Methods
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		public IEnumerator<NodeCollection> GetEnumerator()
		{
			if (this.Title != null)
			{
				yield return this.Title;
			}

			foreach (var param in this.Parameters)
			{
				foreach (var paramNode in param)
				{
					yield return paramNode;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Parameters.Count == 0 ? "[[Link]]" : $"[[Link|Count = {this.Parameters.Count}]]";
		#endregion
	}
}
