#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class MergeHistoryResult
	{
		#region Constructors
		internal MergeHistoryResult(string from, string reason, DateTime timestamp, string to)
		{
			this.From = from;
			this.Reason = reason;
			this.Timestamp = timestamp;
			this.To = to;
		}
		#endregion

		#region Public Properties
		public string From { get; }

		public string Reason { get; }

		public DateTime? Timestamp { get; }

		public string To { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.From + " => " + this.To;
		#endregion
	}
}
