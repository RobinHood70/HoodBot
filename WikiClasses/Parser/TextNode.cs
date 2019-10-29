﻿namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using RobinHood70.WikiCommon;

	/// <summary>Represents a block of text.</summary>
	public class TextNode : IWikiNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TextNode"/> class.</summary>
		/// <param name="text">The text.</param>
		public TextNode([Localizable(false)]string text) => this.Text = text;
		#endregion

		#region Public Properties

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections => Enumerable.Empty<NodeCollection>();

		/// <summary>Gets or sets the text.</summary>
		/// <value>The text.</value>
		public string Text { get; set; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Text.Ellipsis(20) ?? "<Empty>";
		#endregion
	}
}
