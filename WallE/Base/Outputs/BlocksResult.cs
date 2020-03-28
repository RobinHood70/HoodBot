#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class BlocksResult
	{
		#region Constructors
		internal BlocksResult(bool automatic, string? by, long byId, DateTime? expiry, BlockFlags flags, long id, string? rangeStart, string? rangeEnd, string? reason, DateTime? timestamp, string? user, long userId)
		{
			this.Automatic = automatic;
			this.By = by;
			this.ById = byId;
			this.Expiry = expiry;
			this.Flags = flags;
			this.Id = id;
			this.RangeStart = rangeStart;
			this.RangeEnd = rangeEnd;
			this.Reason = reason;
			this.Timestamp = timestamp;
			this.User = user;
			this.UserId = userId;
		}
		#endregion

		#region Public Properties
		public bool Automatic { get; }

		public string? By { get; }

		public long ById { get; }

		public DateTime? Expiry { get; }

		public BlockFlags Flags { get; }

		public long Id { get; }

		public string? RangeStart { get; }

		public string? RangeEnd { get; }

		public string? Reason { get; }

		public DateTime? Timestamp { get; }

		public string? User { get; }

		public long UserId { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.User ?? FallbackText.Unknown;
		#endregion
	}
}
