namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;

	public enum TemplateNodeType
	{
		Template,
		Argument
	}

	public class TemplateNode : WikiNode, IBacklinkNode
	{
		#region Constructors
		public TemplateNode(TemplateNodeType type, bool atLineStart, NodeCollection title, IList<ParameterNode> parameters)
		{
			this.NodeType = type;
			this.AtLineStart = atLineStart;
			this.Title = title;
			this.Parameters = parameters;
		}
		#endregion

		#region Public Properties
		public bool AtLineStart { get; }

		public TemplateNodeType NodeType { get; }

		public IList<ParameterNode> Parameters { get; }

		public NodeCollection Title { get; }
		#endregion

		#region Public Methods
		public override void Accept(INodeVisitor visitor) => visitor?.Visit(this);

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
		public override string ToString() => "<template>";
		#endregion
	}
}
