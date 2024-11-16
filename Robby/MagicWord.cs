namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;

	/// <summary>Represents a MediaWiki magic word.</summary>
	public sealed class MagicWord
	{
		/// <summary>Initializes a new instance of the <see cref="MagicWord"/> class.</summary>
		/// <param name="word">The <see cref="SiteInfoMagicWord"/> to initialize from.</param>
		internal MagicWord(SiteInfoMagicWord word)
		{
			ArgumentNullException.ThrowIfNull(word);

			// Assumes dictionary will hold Id.
			this.CaseSensitive = word.CaseSensitive;
			this.Aliases = new HashSet<string>(word.Aliases, StringComparer.Ordinal);
		}

		/// <summary>Gets any aliases for the word.</summary>
		/// <value>The list of aliases.</value>
		public IReadOnlyCollection<string> Aliases { get; }

		/// <summary>Gets a value indicating whether the magic word is case-sensitive.</summary>
		/// <value><see langword="true" /> if the magic word is case-sensitive; otherwise, <see langword="false" />.</value>
		public bool CaseSensitive { get; }
	}
}