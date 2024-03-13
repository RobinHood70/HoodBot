namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using RobinHood70.CommonCode;

	/// <summary>Houses the information for a page section.</summary>
	public class Section
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		/// <param name="header">The header node for the section (null for lead).</param>
		/// <param name="content">The <see cref="NodeCollection"/> representing the content of the section.</param>
		public Section(IHeaderNode? header, NodeCollection content)
		{
			ArgumentNullException.ThrowIfNull(content);
			this.Content = content;
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

		#region Public Static Methods

		/// <summary>Formats the provided text into a <see cref="Section"/>.</summary>
		/// <param name="factory">The factory to use to parse the request.</param>
		/// <param name="header">The header text.</param>
		/// <param name="content">The body text.</param>
		/// <returns>THe new Section.</returns>
		public static Section FromText(IWikiNodeFactory factory, string header, string content) => FromText(factory, 2, header, content);

		/// <summary>Formats the provided text into a <see cref="Section"/>.</summary>
		/// <param name="factory">The factory to use to parse the request.</param>
		/// <param name="level">The level of the header if not 2.</param>
		/// <param name="header">The header text.</param>
		/// <param name="content">The body text.</param>
		/// <returns>THe new Section.</returns>
		public static Section FromText(IWikiNodeFactory factory, int level, string header, string content)
		{
			var headerNode = factory.NotNull().HeaderNodeFromParts(level, header);
			var bodyNodes = factory.Parse('\n' + content, factory.InclusionType, factory.StrictInclusion);

			return new Section(headerNode, bodyNodes);
		}
		#endregion
	}
}