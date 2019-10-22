#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class FeedWatchlistInput
	{
		#region Public Properties
		public bool AllRevisions { get; set; }

		public bool LinkToDiffs { get; set; }

		public string? ExcludeUser { get; set; }

		public string? FeedFormat { get; set; }

		public Filter FilterAnonymous { get; set; }

		public Filter FilterBot { get; set; }

		public Filter FilterMinor { get; set; }

		public Filter FilterPatrolled { get; set; }

		public Filter FilterUnread { get; set; }

		public int Hours { get; set; }

		public bool LinkToSections { get; set; }

		public string? Owner { get; set; }

		public string? Token { get; set; }

		public WatchlistTypes Types { get; set; }
		#endregion
	}
}
