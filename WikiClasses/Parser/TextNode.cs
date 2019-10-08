namespace RobinHood70.WikiClasses.Parser
{
	public class TextNode : INodeBase
	{
		#region Constructors
		public TextNode(string text) => this.Text = text;
		#endregion

		#region Public Properties
		public string Text { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Text;
		#endregion
	}
}
