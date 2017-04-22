#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class FeedWatchlistInput
	{
		#region Public Properties
		public bool AllRevisions { get; set; }

		public bool LinkToDiffs { get; set; }

		public string ExcludeUser { get; set; }

		public string FeedFormat { get; set; }

		public FilterOption FilterAnonymous { get; set; }

		public FilterOption FilterBot { get; set; }

		public FilterOption FilterMinor { get; set; }

		public FilterOption FilterPatrolled { get; set; }

		public FilterOption FilterUnread { get; set; }

		public int Hours { get; set; }

		public bool LinkToSections { get; set; }

		public string Owner { get; set; }

		public string Token { get; set; }

		public WatchlistTypes Types { get; set; }
		#endregion
	}
}
