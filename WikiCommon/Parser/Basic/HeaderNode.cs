namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>Represents a header.</summary>
	public class HeaderNode : IHeaderNode
	{
		// TODO: Rejig this so that header node is strictly the header with no trailing space. GetInnerText can then be removed since this will only ever be storing the interior text. Will need to look closely at HeaderElement, though, to make sure fallback unwikifying isn't affected.
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes.</param>
		/// <param name="level">The level.</param>
		/// <param name="text">The text of the header.</param>
		/// <param name="comment">Any text that came after the close ==.</param>
		public HeaderNode(IWikiNodeFactory factory, int level, [Localizable(false)] IEnumerable<IWikiNode> text, IEnumerable<IWikiNode>? comment)
		{
			ArgumentNullException.ThrowIfNull(factory);
			ArgumentNullException.ThrowIfNull(text);
			this.Factory = factory;
			this.Level = level;
			this.Title = new WikiNodeCollection(factory, text);
			this.Comment = comment is null ? null : new WikiNodeCollection(factory, comment);
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public WikiNodeCollection? Comment { get; }

		/// <inheritdoc/>
		public bool Confirmed { get; set; }

		/// <inheritdoc/>
		public IWikiNodeFactory Factory { get; }

		/// <inheritdoc/>
		public int Level { get; }

		/// <inheritdoc/>
		public IEnumerable<WikiNodeCollection> NodeCollections
		{
			get
			{
				yield return this.Title;
			}
		}

		/// <inheritdoc/>
		public WikiNodeCollection Title { get; }
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
			var retval = (this.Title.Count == 1 && this.Title[0] is ITextNode text)
				? text.Text
				: "<Header>";
			var equalsSigns = new string('=', this.Level);
			return equalsSigns + retval + equalsSigns;
		}
		#endregion
	}
}