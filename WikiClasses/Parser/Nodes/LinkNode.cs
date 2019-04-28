using System.Collections.Generic;

namespace RobinHood70.WikiClasses.Parser.Nodes
{
	public class LinkNode : INodeBase
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
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => "<link>";
		#endregion
	}
}
