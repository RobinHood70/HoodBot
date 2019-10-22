namespace RobinHood70.WikiClasses.Parser
{
	using RobinHood70.WikiCommon;

	/// <summary>Represents a wikitext (HTML) comment.</summary>
	public class CommentNode : IWikiNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="CommentNode"/> class.</summary>
		/// <param name="comment">The comment.</param>
		public CommentNode(string comment) => this.Comment = comment;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the comment text.</summary>
		/// <value>The comment text.</value>
		public string Comment { get; set; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => "<!--" + this.Comment.Ellipsis(20) + "-->";
		#endregion
	}
}
