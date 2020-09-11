namespace RobinHood70.WikiCommon.BasicParser
{
	using System;
	using System.Collections.Generic;

	/// <summary>Represents a blob of text that should be ignored. Depending on the parser's configuration, this can be the entire text of an <c>include</c>/<c>noinclude</c> block, the text outside of an <c>onlyinclude</c>, or just the tags themselves.</summary>
	public class IgnoreNode : IWikiNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="IgnoreNode"/> class.</summary>
		/// <param name="value">The value.</param>
		public IgnoreNode(string value) => this.Value = value;
		#endregion

		#region Public Properties

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections => Array.Empty<NodeCollection>();

		/// <summary>Gets the value.</summary>
		/// <value>The value.</value>
		public string Value { get; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Value;
		#endregion
	}
}
