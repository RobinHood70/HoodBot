namespace RobinHood70.WikiClasses.Parser
{
	public class TextNode : IWikiNode
	{
		#region Constructors
		public TextNode(string text) => this.Text = text;
		#endregion

		#region Public Properties
		public string Text { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Text.Ellipsis(20);
		#endregion
	}
}
