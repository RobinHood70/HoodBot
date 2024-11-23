namespace RobinHood70.WikiCommon.Parser;

// TODO: Switch to have Title as one WikiNodeCollection and TrailingText as another.
// CONSIDER: Reintroduce confirmed/possible status for constructs like:
//    ===Header===<includeonly>BreaksHeader</includeonly>
// Might be doable inline as part of TrailingText, so check this.

/// <summary>Represents a header.</summary>
public interface IHeaderNode : IWikiNode, IParentNode
{
	#region Properties

	/// <summary>Gets any text that appeared after the ==.</summary>
	WikiNodeCollection Comment { get; }

	/// <summary>Gets or sets a value indicating whether this <see cref="IHeaderNode"/> is confirmed (direct text) or possible (template or argument).</summary>
	/// <value><see langword="true"/> if confirmed; otherwise, <see langword="false"/>.</value>
	bool Confirmed { get; set; }

	/// <summary>Gets the level.</summary>
	/// <value>The level. This is equal to the number of visible equals signs.</value>
	int Level { get; }

	/// <summary>Gets the title.</summary>
	/// <value>The title.</value>
	WikiNodeCollection Title { get; }
	#endregion
}