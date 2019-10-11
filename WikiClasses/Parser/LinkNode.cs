namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;

	public class LinkNode : IWikiNode, IBacklinkNode
	{
		#region Constructors
		public LinkNode(NodeCollection title, IList<ParameterNode> parameters)
		{
			this.Title = title;
			this.Parameters = parameters;
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
