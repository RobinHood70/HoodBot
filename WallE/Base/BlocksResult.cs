#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using WikiCommon;

	public class BlocksResult
	{
		#region Public Properties
		public bool Automatic { get; set; }

		public string By { get; set; }

		public long ById { get; set; }

		public DateTime? Expiry { get; set; }

		public BlockFlags Flags { get; set; }

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
