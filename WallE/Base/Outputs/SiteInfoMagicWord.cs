#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class SiteInfoMagicWord
	{
		#region Constructors
		internal SiteInfoMagicWord(string name, List<string> aliases, bool caseSensitive)
		{
			this.Name = name;
			this.Aliases = aliases;
			this.CaseSensitive = caseSensitive;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Aliases { get; }

		public bool CaseSensitive { get; }

		public string Name { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}
