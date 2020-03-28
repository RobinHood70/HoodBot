namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	/// <summary>Represents a wikitext (HTML) comment.</summary>
	public class CommentNode : IWikiNode
	{
		#region Fields
		private string comment = string.Empty;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="CommentNode"/> class.</summary>
		/// <param name="comment">The comment.</param>
		public CommentNode(string comment) => this.Comment = comment;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the comment text.</summary>
		/// <value>The comment text.</value>
		[AllowNull]
		public string Comment
		{
			get => this.comment;
			set
			{
				value ??= string.Empty;
				if (this.comment != value)
				{
					this.comment = value;
				}
			}
		}

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections => Array.Empty<NodeCollection>();
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
