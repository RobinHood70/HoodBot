namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using static CommonCode.Globals;

	/// <summary>Represents a header.</summary>
	public class HeaderNode : IWikiNode
	{
		// TODO: Rejig this so that header node is strictly the header with no trailing space. GetInnerText can then be removed since this will only ever be storing the interior text. Will need to look closely at HeaderElement, though, to make sure fallback unwikifying isn't affected.
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
		/// <param name="index">The index.</param>
		/// <param name="level">The level.</param>
		/// <param name="text">The text of the header.</param>
		public HeaderNode(int index, int level, [Localizable(false)] IEnumerable<IWikiNode> text)
		{
			this.Index = index;
			this.Level = level;
			this.Title = new NodeCollection(this, text ?? throw ArgumentNull(nameof(text)));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether this <see cref="HeaderNode"/> is confirmed (direct text) or possible (template or argument).</summary>
		/// <value><see langword="true"/> if confirmed; otherwise, <see langword="false"/>.</value>
		public bool Confirmed { get; set; }

		/// <summary>Gets the equals signs surrounding the title.</summary>
		/// <value>The equals signs which surround the title.</value>
		/// <remarks>This is a convenience method to produce the appropriate amount of equals signs instead of having to repeat the same code in numerous places.</remarks>
		public string EqualsSigns => new string('=', this.Level);

		/// <summary>Gets the index.</summary>
		/// <value>The index (count of headers to this point in the text).</value>
		public int Index { get; }

		/// <summary>Gets the level.</summary>
		/// <value>The level. This is equal to the number of visible equals signs.</value>
		public int Level { get; }

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				yield return this.Title;
			}
		}

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new HeaderNode from the provided text.</summary>
		/// <param name="level">The header level (number of equals signs).</param>
		/// <param name="text">The text of the argument.</param>
		/// <returns>A new HeaderNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single argument (<c>{{{abc|123}}}</c>).</exception>
		public static HeaderNode FromParts(int level, [Localizable(false)] string text) => new HeaderNode(0, level, WikiTextParser.Parse(text));

		/// <summary>Creates a new HeaderNode from the provided text.</summary>
		/// <param name="text">The text of the argument.</param>
		/// <returns>A new HeaderNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single argument (<c>{{{abc|123}}}</c>).</exception>
		public static HeaderNode FromText([Localizable(false)] string text) => WikiTextParser.SingleNode<HeaderNode>(text);
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Gets the text inside the heading delimiters.</summary>
		/// <param name="innerTrim">if set to <c>true</c> [inner trim].</param>
		/// <returns>The text inside the heading delimiters.</returns>
		/// <remarks>This is method is provided as a temporary measure. The intent is to alter the parser itself so as to make this method unnecessary.</remarks>
		public string GetInnerText(bool innerTrim)
		{
			var text = WikiTextVisitor.Value(this).TrimEnd();
			text = text.Substring(this.Level, text.Length - this.Level * 2);
			return innerTrim ? text.Trim() : text;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.EqualsSigns + "Header" + this.EqualsSigns;
		#endregion
	}
}
