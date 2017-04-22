namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using WallE.Base;

	public class MagicWord
	{
		internal MagicWord(MagicWordsItem word)
		{
			// Assumes dictionary will hold Id.
			this.CaseSensitive = word.CaseSensitive;
			this.Aliases = new HashSet<string>(word.Aliases);
		}

		public IReadOnlyCollection<string> Aliases { get; }

		public bool CaseSensitive { get; }
	}
}
