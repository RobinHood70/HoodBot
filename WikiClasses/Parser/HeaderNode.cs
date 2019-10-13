namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	/// <summary>Represents a header.</summary>
	public class HeaderNode : IWikiNode, IEnumerable<NodeCollection>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
		/// <param name="level">The level.</param>
		/// <param name="text">The text.</param>
		public HeaderNode(int level, string text)
		{
			this.Level = level;
			this.Title = WikiTextParser.Parse(text);
		}

		/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
		/// <param name="index">The index.</param>
		/// <param name="level">The level.</param>
		/// <param name="title">The title.</param>
		public HeaderNode(int index, int level, IEnumerable<IWikiNode> title)
		{
			this.Index = index;
			this.Level = level;
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether this <see cref="HeaderNode"/> is confirmed (direct text) or possible (template or argument).</summary>
		/// <value><see langword="true"/> if confirmed; otherwise, <see langword="false"/>.</value>
		public bool Confirmed { get; set; }

		/// <summary>Gets or sets the index.</summary>
		/// <value>The index (count of headers to this point in the text).</value>
		public int Index { get; set; }

		/// <summary>Gets or sets the level.</summary>
		/// <value>The level. This is equal to the number of visible equals signs.</value>
		public int Level { get; set; }

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new ArgumentNode from the provided text.</summary>
		/// <param name="text">The text of the argument.</param>
		/// <returns>A new ArgumentNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single argument (<c>{{{abc|123}}}</c>).</exception>
		public static HeaderNode FromText(string text) => WikiTextParser.SingleNode<HeaderNode>(text);
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<NodeCollection> GetEnumerator()
		{
			yield return this.Title;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			var equals = new string('=', this.Level);
			return equals + "Header" + equals;
		}
		#endregion
	}
}
