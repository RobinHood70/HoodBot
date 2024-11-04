namespace RobinHood70.WikiCommon.Parser
{
	using System;

	/// <summary>Houses the information for a page section.</summary>
	public class Section
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		/// <param name="header">The header node for the section (null for lead).</param>
		/// <param name="content">The <see cref="WikiNodeCollection"/> representing the content of the section.</param>
		public Section(IHeaderNode? header, WikiNodeCollection content)
		{
			ArgumentNullException.ThrowIfNull(content);
			this.Content = content;
			this.Header = header;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the <see cref="WikiNodeCollection"/> representing the content of the section.</summary>
		public WikiNodeCollection Content { get; }

		/// <summary>Gets or sets the header node for the section (null for lead).</summary>
		public IHeaderNode? Header { get; set; }
		#endregion

		#region Public Static Methods

		/// <summary>Formats the provided text into a <see cref="Section"/>.</summary>
		/// <param name="factory">The factory to use to parse the request.</param>
		/// <param name="header">The header text. If null, the section will be a lead section.</param>
		/// <param name="content">The body text.</param>
		/// <returns>The new Section.</returns>
		public static Section FromText(IWikiNodeFactory factory, string? header, string content) => FromText(factory, 2, header, content);

		/// <summary>Formats the provided text into a <see cref="Section"/>.</summary>
		/// <param name="factory">The factory to use to parse the request.</param>
		/// <param name="level">The level of the header. If set to 0, <paramref name="header"/> will be ignored and the section will be a lead section.</param>
		/// <param name="header">The header text. If null, <paramref name="level"/> will be ignored and the section will be a lead section.</param>
		/// <param name="content">The body text.</param>
		/// <returns>THe new Section.</returns>
		public static Section FromText(IWikiNodeFactory factory, int level, string? header, string content)
		{
			ArgumentNullException.ThrowIfNull(factory);
			var headerNode = (header is null || level == 0)
				? null
				: factory.HeaderNodeFromParts(level, header);
			if (headerNode is not null)
			{
				content = "\n" + content;
			}

			var bodyNodes = factory.Parse(content);
			var collection = new WikiNodeCollection(factory, bodyNodes);

			return new Section(headerNode, collection);
		}
		#endregion

		#region Public Methods

		/// <summary>This is a shortcut method to get the title of the section's header.</summary>
		/// <returns>The section's title.</returns>
		public string? GetFullTitle() => this.Header is null
			? null
			: WikiTextVisitor.Raw(this.Header);

		/// <summary>This is a shortcut method to get the title of the section's header.</summary>
		/// <returns>The section's title.</returns>
		public string? GetTitle() => this.Header is null
			? null
			: WikiTextVisitor.Raw(this.Header.Title).Trim();
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override string ToString()
		{
			var title = this.GetFullTitle();
			var text = this.Content?.Count > 0
				? WikiTextVisitor.Raw(this.Content[0])
				: string.Empty;
			if (text.Length > 15)
			{
				text = text[..15];
			}

			return title is null
				? text
				: title + "  " + text;
		}
		#endregion
	}
}