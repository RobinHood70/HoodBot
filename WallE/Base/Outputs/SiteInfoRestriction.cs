#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class SiteInfoRestriction
	{
		#region Constructors
		internal SiteInfoRestriction(IReadOnlyList<string> cascadingLevels, IReadOnlyList<string> levels, IReadOnlyList<string> semiProtectedLevels, IReadOnlyList<string> types)
		{
			this.CascadingLevels = cascadingLevels;
			this.Levels = levels;
			this.SemiProtectedLevels = semiProtectedLevels;
			this.Types = types;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> CascadingLevels { get; }

		public IReadOnlyList<string> Levels { get; }

		public IReadOnlyList<string> SemiProtectedLevels { get; }

		public IReadOnlyList<string> Types { get; }
		#endregion
	}
}
