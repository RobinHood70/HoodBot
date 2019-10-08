namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;

	public enum TemplateNodeType
	{
		Template,
		Argument
	}

	public class TemplateNode : IBacklinkNode
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
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => "<template>";
		#endregion
	}
}
