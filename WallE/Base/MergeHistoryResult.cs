#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class MergeHistoryResult
	{
		#region Public Properties
		public string From { get; set; }

		public string Reason { get; set; }

		public DateTime? Timestamp { get; set; }

		public string To { get; set; }
		#endregion
	}
}
