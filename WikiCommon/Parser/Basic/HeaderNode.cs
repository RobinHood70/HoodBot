﻿namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Represents a header.</summary>
	public class HeaderNode : IHeaderNode
	{
		// TODO: Rejig this so that header node is strictly the header with no trailing space. GetInnerText can then be removed since this will only ever be storing the interior text. Will need to look closely at HeaderElement, though, to make sure fallback unwikifying isn't affected.
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes.</param>
		/// <param name="level">The level.</param>
		/// <param name="text">The text of the header.</param>
		public HeaderNode(IWikiNodeFactory factory, int level, [Localizable(false)] IEnumerable<IWikiNode> text)
		{
			this.Factory = factory ?? throw ArgumentNull(nameof(factory));
			this.Level = level;
			this.Title = factory.NodeCollectionFromNodes(text ?? throw ArgumentNull(nameof(text)));
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public bool Confirmed { get; set; }

		/// <inheritdoc/>
		public IWikiNodeFactory Factory { get; }

		/// <inheritdoc/>
		public int Level { get; }

		/// <inheritdoc/>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				yield return this.Title;
			}
		}

		/// <inheritdoc/>
		public NodeCollection Title { get; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			var equalsSigns = new string('=', this.Level);
			return equalsSigns + "Header" + equalsSigns;
		}
		#endregion
	}
}
