namespace RobinHood70.HoodBotPlugins;

using System;

// Doing the optional data this way is not ideal, since it expands to handle every possible viewer. I don't see an easy way around this, since instantiator has to be viewer-agnostic.
// CONSIDER: Take a "Page" object instead of individual parameters, and have the viewer extract most of its data from that. This would be more extensible, but makes the diff viewer dependent on Robby, which it isn't currently.

/// <summary>Encapsulates all the data needed for a variety of different diff options.</summary>
/// <param name="fullPageName">Full name of the page.</param>
/// <param name="text">The text.</param>
/// <param name="editSummary">The edit summary.</param>
/// <param name="isMinor">if set to <see langword="true"/> [is minor].</param>
/// <remarks>Constructor takes required data, while init parameters provide viewer-specific data.</remarks>
public class DiffContent(string fullPageName, string text, string editSummary, bool isMinor)
{
	#region Public Properties

	/// <summary>Gets the URI to edit the article in a browser.</summary>
	public Uri? EditPath { get; init; }

	/// <summary>Gets the edit summary for the edit (for browser-based diff viewers where a save may be desirable).</summary>
	public string EditSummary { get; } = editSummary;

	/// <summary>Gets an edit token (for browser-based diff viewers where a save may be desirable). May be <see langword="null"/>for non-browser diff viewers.</summary>
	public string? EditToken { get; init; }

	/// <summary>Gets the full name of the page.</summary>
	/// <value>The full name of the page.</value>
	public string FullPageName { get; } = fullPageName;

	/// <summary>Gets a value indicating whether the edit should be marked as minor.</summary>
	public bool IsMinor { get; } = isMinor;

	/// <summary>Gets the last revision text.</summary>
	/// <value>The last revision text.</value>
	public string? LastRevisionText { get; init; }

	/// <summary>Gets the last revision timestamp.</summary>
	/// <value>The last revision timestamp.</value>
	public DateTime? LastRevisionTimestamp { get; init; }

	/// <summary>Gets the start timestamp.</summary>
	/// <value>The start timestamp (the time at which the page was last loaded).</value>
	public DateTime? StartTimestamp { get; init; }

	/// <summary>Gets the article text.</summary>
	/// <value>The current article text.</value>
	public string Text { get; } = text;

	/// <summary>Gets the user agent string.</summary>
	/// <remarks>May be null for diff viewers that aren't HTML based, or to use the browser's default user agent.</remarks>
	public string? UserAgent { get; init; }

	/// <summary>Gets the user folder, presumably for cookie storage. This should be the base path, with the diff viewer appending a subpath for its specific data.</summary>
	public string? UserFolder { get; init; }
	#endregion
}