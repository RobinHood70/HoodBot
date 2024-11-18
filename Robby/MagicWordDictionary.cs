namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	/// <summary>A magic word dictionary comparable to the MagicWordArray.php in MediaWiki. This should only ever be used to house a context-specific subset of magic words, as the full set has inherent conflicts between some words and aliases.</summary>
	public class MagicWordDictionary : MixedSensitivityDictionary<MagicWord>
	{
		/// <summary>Initializes a new instance of the <see cref="MagicWordDictionary"/> class.</summary>
		/// <param name="words">The words to add to the dictionary.</param>
		public MagicWordDictionary(IEnumerable<MagicWord> words)
		{
			ArgumentNullException.ThrowIfNull(words);
			foreach (var word in words)
			{
				foreach (var alias in word.Aliases)
				{
					this.Add(word.CaseSensitive, alias, word);
				}
			}
		}
	}
}
