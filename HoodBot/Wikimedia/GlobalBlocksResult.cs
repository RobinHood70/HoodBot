namespace RobinHood70.HoodBot.Wikimedia
{
	using System;
	using RobinHood70.CommonCode;

	public class GlobalBlocksResult
	{
		#region Constructors
		internal GlobalBlocksResult(string? address, bool anononly, string? by, string? byWiki, DateTime? expiry, long id, string? rangeStart, string? rangeEnd, string? reason, DateTime? timestamp)
		{
			this.Address = address;
			this.AnonymousOnly = anononly;
			this.By = by;
			this.ByWiki = byWiki;
			this.Expiry = expiry;
			this.Id = id;
			this.RangeStart = rangeStart;
			this.RangeEnd = rangeEnd;
			this.Reason = reason;
			this.Timestamp = timestamp;
		}
		#endregion

		#region Public Properties
		public string? Address { get; }

		public bool AnonymousOnly { get; }

		public string? By { get; }

		public string? ByWiki { get; }

		public DateTime? Expiry { get; }

		public long Id { get; }

		public string? RangeStart { get; }

		public string? RangeEnd { get; }

		public string? Reason { get; }

		public DateTime? Timestamp { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Address ?? Globals.Unknown;
		#endregion
	}
}