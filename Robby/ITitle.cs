namespace RobinHood70.Robby
{
	#region Public Enumerations

	/// <summary>The format to use for the link text.</summary>
	public enum LinkFormat
	{
		/// <summary>Plain link with no text.</summary>
		Plain,

		/// <summary>Link text should follow "pipe trick" rules.</summary>
		PipeTrick,

		/// <summary>Link text should strip paranthetical text only.</summary>
		LabelName
	}
	#endregion

	/// <summary>Identifies anything that represents a wiki title.</summary>
	public interface ITitle
	{
		#region Properties

		/// <summary>Gets the title.</summary>
		Title Title { get; }
		#endregion

		#region Methods

		/// <summary>Returns the current object as link text.</summary>
		/// <param name="linkFormat">The default text to use when adding a caption.</param>
		/// <returns>The current title, formatted as a link.</returns>
		string AsLink(LinkFormat linkFormat = LinkFormat.Plain);

		/// <summary>Returns the current object as bare link text with no caption or surrounding braces.</summary>
		/// <returns>The current title, formatted as a link.</returns>
		string LinkName();
		#endregion
	}
}