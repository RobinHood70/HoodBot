namespace RobinHood70.WikiCommon
{
	using System.Collections.Generic;

	/// <summary>Represents a MediaWiki magic word.</summary>
	/// <remarks>Initializes a new instance of the <see cref="MagicWord"/> class.</remarks>
	/// <param name="name">The name of the magic word.</param>
	/// <param name="aliases">The magic word aliases.</param>
	/// <param name="caseSensitive">The case-sensitivity of the magic word.</param>
	public sealed class MagicWord(string name, IReadOnlyList<string> aliases, bool caseSensitive)
	{

		/// <summary>Gets any aliases for the word.</summary>
		/// <value>The list of aliases.</value>
		public IReadOnlyCollection<string> Aliases { get; } = aliases;

		/// <summary>Gets a value indicating whether the magic word is case-sensitive.</summary>
		/// <value><see langword="true" /> if the magic word is case-sensitive; otherwise, <see langword="false" />.</value>
		public bool CaseSensitive { get; } = caseSensitive;

		/// <summary>Gets the name of the magic word.</summary>
		/// <value>A unique string identifying the magic word.</value>
		public string Name { get; } = name;
	}
}