namespace RobinHood70.WikiCommon.Parser
{
	/// <summary>Represents an unparsed tag, such as pre or nowiki.</summary>
	public interface ITagNode : IWikiNode
	{
		/// <summary>Gets or sets the tag's attributes.</summary>
		/// <value>The attributes.</value>
		string? Attributes { get; set; }

		/// <summary>Gets or sets the close tag.</summary>
		/// <value>The close tag.</value>
		string? Close { get; set; }

		/// <summary>Gets or sets the inner text.</summary>
		/// <value>The unparsed inner text.</value>
		string? InnerText { get; set; }

		/// <summary>Gets or sets the tag name.</summary>
		/// <value>The tag name.</value>
		string Name { get; set; }

		/// <summary>Gets a value indicating whether the tag is self-closed.</summary>
		/// <value>
		///   <c>true</c> if this is a self-closed tag; otherwise, <c>false</c>.</value>
		bool SelfClosed { get; }
	}
}