namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using System.Linq;
	using RobinHood70.WikiCommon;

	/// <summary>  Represents an unparsed tag, such as pre or nowiki.</summary>
	public class TagNode : IWikiNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TagNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="attributes">The attributes.</param>
		/// <param name="innerText">The inner text.</param>
		/// <param name="close">The close.</param>
		public TagNode(string name, string? attributes, string? innerText, string? close)
		{
			this.Name = name;
			this.Attributes = attributes;
			this.InnerText = innerText;
			this.Close = close;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the tag's attributes.</summary>
		/// <value>The attributes.</value>
		public string? Attributes { get; set; }

		/// <summary>Gets or sets the close tag.</summary>
		/// <value>The close tag.</value>
		public string? Close { get; set; } // Note that this is a full close tag, including the surrounding </...>.

		/// <summary>Gets or sets the inner text.</summary>
		/// <value>The unparsed inner text.</value>
		public string? InnerText { get; set; }

		/// <summary>Gets or sets the tag name.</summary>
		/// <value>The tag name.</value>
		public string Name { get; set; } // Note that this is NOT a full open tag, it's just the name.

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections => Enumerable.Empty<NodeCollection>();

		/// <summary>Gets a value indicating whether the tag is self-closed.</summary>
		/// <value>
		///   <c>true</c> if this is a self-closed tag; otherwise, <c>false</c>.</value>
		public bool SelfClosed => this.Close == null;
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.SelfClosed ? $"<{this.Name}{this.Attributes}>" : $"<{this.Name}{this.Attributes}>{this.InnerText.Ellipsis(10)}{this.Close}";
		#endregion
	}
}
