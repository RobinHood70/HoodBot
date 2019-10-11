namespace RobinHood70.WikiClasses.Parser
{
	public class TagNode : IWikiNode
	{
		#region Constructors
		// TODO: Self-closed option was a quick addition to fix the immediate problem. Is this the best approach? Close = { null, empty, content } seems like a better approach and I think what I was doing before, but it wasn't quite working right.
		public TagNode(string name, string attributes, string innerText, string close, bool selfClosed)
		{
			this.Name = name;
			this.Attributes = attributes;
			this.InnerText = innerText;
			this.Close = close;
			this.SelfClosed = selfClosed;
		}
		#endregion

		#region Public Properties
		public string Attributes { get; set; }

		public string Close { get; set; } // Note that this is a full close tag, including the surrounding </...>.

		public string InnerText { get; set; }

		public string Name { get; set; } // Note that this is NOT a full open tag, it's just the name.

		public bool SelfClosed { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.SelfClosed ? $"<{this.Name}{this.Attributes}>" : $"<{this.Name}{this.Attributes}>{this.InnerText.Ellipsis(10)}{this.Close}";
		#endregion
	}
}
