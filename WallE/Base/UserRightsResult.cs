#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class UserRightsResult
	{
		#region Public Properties
		public IReadOnlyList<string> Added { get; set; }

		public IReadOnlyList<string> Removed { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion
	}
}
