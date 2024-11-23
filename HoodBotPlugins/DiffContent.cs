namespace RobinHood70.HoodBotPlugins;

using System;

/// <summary>  Encapsulates all the data needed for a variety of different diff options.</summary>
/// <remarks>Initializes a new instance of the <see cref="DiffContent"/> class.</remarks>
/// <param name="fullPageName">Full name of the page.</param>
/// <param name="text">The text.</param>
/// <param name="editSummary">The edit summary.</param>
/// <param name="isMinor">if set to <see langword="true"/> [is minor].</param>
public class DiffContent(string fullPageName, string text, string editSummary, bool isMinor)
{
	#region Public Properties

	/// <summary>Gets or sets the URI to edit the article in a browser.</summary>
	public Uri? EditPath { get; set; }

	/// <summary>Gets the edit summary for the edit (for browser-based diff viewers where a save may be desirable).</summary>
	public string EditSummary { get; } = editSummary;

	/// <summary>Gets or sets an edit token (for browser-based diff viewers where a save may be desirable). May be <see langword="null"/>for non-browser diff viewers.</summary>
	public string? EditToken { get; set; }

	/// <summary>Gets the full name of the page.</summary>
	/// <value>The full name of the page.</value>
	public string FullPageName { get; } = fullPageName;

	/// <summary>Gets a value indicating whether the edit should be marked as minor.</summary>
	public bool IsMinor { get; } = isMinor;

	/// <summary>Gets or sets the last revision text.</summary>
	/// <value>The last revision text.</value>
	public string? LastRevisionText { get; set; }

	/// <summary>Gets or sets the last revision timestamp.</summary>
	/// <value>The last revision timestamp.</value>
	public DateTime? LastRevisionTimestamp { get; set; }

	/// <summary>Gets or sets the start timestamp.</summary>
	/// <value>The start timestamp (the time at which the page was last loaded).</value>
	public DateTime? StartTimestamp { get; set; }

	/// <summary>Gets the article text.</summary>
	/// <value>The current article text.</value>
	public string Text { get; } = text;
	#endregion
}