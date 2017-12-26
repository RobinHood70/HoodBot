#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum BlocksFlags
	{
		None = 0,
		AllowUserTalk = 1,
		AnonymousOnly = 1 << 1,
		AutoBlock = 1 << 2,
		Automatic = 1 << 3,
		Hidden = 1 << 4,
		NoCreate = 1 << 5,
		NoEmail = 1 << 6
	}
	#endregion

	public class BlocksResult
	{
		#region Public Properties
		public string By { get; set; }

		public long ById { get; set; }

		public DateTime? Expiry { get; set; }

		public BlocksFlags Flags { get; set; }

		public long Id { get; set; }

		public string RangeStart { get; set; }

		public string RangeEnd { get; set; }

		public string Reason { get; set; }

		public DateTime? Timestamp { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion
	}
}
