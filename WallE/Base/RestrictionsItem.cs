#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class RestrictionsItem
	{
		#region Public Properties
		public IReadOnlyList<string> CascadingLevels { get; set; }

		public IReadOnlyList<string> Levels { get; set; }

		public IReadOnlyList<string> SemiProtectedLevels { get; set; }

		public IReadOnlyList<string> Types { get; set; }
		#endregion
	}
}
