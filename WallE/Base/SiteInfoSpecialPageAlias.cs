#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class SiteInfoSpecialPageAlias
	{
		#region Constructors
		internal SiteInfoSpecialPageAlias(string realName, IReadOnlyList<string> aliases)
		{
			this.RealName = realName;
			this.Aliases = aliases;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Aliases { get; }

		public string RealName { get; }
		#endregion

		#region Public Override Properties
		public override string ToString() => this.RealName;
		#endregion
	}
}