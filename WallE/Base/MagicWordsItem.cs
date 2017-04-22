#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class MagicWordsItem
	{
		#region Public Properties
		public IReadOnlyList<string> Aliases { get; set; }

		public bool CaseSensitive { get; set; }

		public string Name { get; set; }
		#endregion
	}
}
