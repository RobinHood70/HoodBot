namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;

	public class TemplateNode : WikiNode, IBacklinkNode
	{
		#region Constructors
		public TemplateNode(bool atLineStart, NodeCollection title, IList<ParameterNode> parameters)
		{
			this.AtLineStart = atLineStart;
			this.Title = title;
			this.Parameters = parameters;
		}
		#endregion

		#region Public Properties
		public bool AtLineStart { get; }

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

		public Dictionary<string, NodeCollection> ParameterDictionary()
		{
			// TODO: Parameter-based methods are very primitive for now, just to get the basics working. Needs more work.
			var retval = new Dictionary<string, NodeCollection>();
			foreach (var parameter in this.Parameters)
			{
				retval.Add(parameter.Index > 0 ? parameter.Index.ToString() : WikiTextVisitor.Value(parameter.Name), parameter.Value);
			}

			return retval;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => "<template>";
		#endregion
	}
}