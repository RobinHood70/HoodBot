﻿namespace RobinHood70.Robby.Parser
{
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>Houses the information for a page section.</summary>
	public class Section
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		/// <param name="header">The header node for the section (null for lead).</param>
		/// <param name="content">The <see cref="NodeCollection"/> representing the content of the section.</param>
		public Section(IHeaderNode? header, NodeCollection content)
		{
			this.Content = content.NotNull(nameof(content));
			this.Header = header;
		}

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		/// <param name="header">The header node for the section (null for lead).</param>
		/// <param name="factory">The factory for the content nodes.</param>
		public Section(IHeaderNode? header, IWikiNodeFactory factory)
			: this(header, new NodeCollection(factory))
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the <see cref="NodeCollection"/> representing the content of the section.</summary>
		public NodeCollection Content { get; }

		/// <summary>Gets or sets the header node for the section (null for lead).</summary>
		public IHeaderNode? Header { get; set; }
		#endregion
	}
}