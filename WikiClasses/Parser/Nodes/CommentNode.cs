namespace RobinHood70.WikiClasses.Parser.Nodes
{
	/// <summary>Represents a wikitext (HTML) comment.</summary>
	public class CommentNode : INodeBase
	{
		#region Constructors
		public CommentNode(string comment) => this.Comment = comment;
		#endregion

		#region Public Properties
		public string Comment { get; set; }
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Comment;
		#endregion
	}
}
