#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using static RobinHood70.CommonCode.Globals;

	public class MergeHistoryInput
	{
		#region Constructors
		public MergeHistoryInput(string from, string to)
		{
			ThrowNullOrWhiteSpace(from, nameof(from));
			ThrowNullOrWhiteSpace(to, nameof(to));
			this.From = from;
			this.To = to;
		}

		public MergeHistoryInput(string from, long toId)
		{
			ThrowNullOrWhiteSpace(from, nameof(from));
			this.From = from;
			this.ToId = toId;
		}

		public MergeHistoryInput(long fromId, string to)
		{
			ThrowNullOrWhiteSpace(to, nameof(to));
			this.FromId = fromId;
			this.To = to;
		}

		public MergeHistoryInput(long fromId, long toId)
		{
			this.FromId = fromId;
			this.ToId = toId;
		}
		#endregion

		#region Public Properties
		public string? From { get; }

		public long FromId { get; }

		public string? Reason { get; set; }

		public DateTime? Timestamp { get; set; }

		public string? To { get; }

		public long ToId { get; }

		public string? Token { get; set; }
		#endregion
	}
}
