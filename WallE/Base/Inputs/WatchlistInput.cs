#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	#region Public Enumerations
	[Flags]
	public enum WatchlistProperties
	{
		None = 0,
		Ids = 1,
		Title = 1 << 1,
		Flags = 1 << 2,
		User = 1 << 3,
		UserId = 1 << 4,
		Comment = 1 << 5,
		ParsedComment = 1 << 6,
		Timestamp = 1 << 7,
		Patrol = 1 << 8,
		Sizes = 1 << 9,
		NotificationTimestamp = 1 << 10,
		LogInfo = 1 << 11,
		All = Ids | Title | Flags | User | UserId | Comment | ParsedComment | Timestamp | Patrol | Sizes | NotificationTimestamp | LogInfo,
		AllButPatrol = All & ~Patrol
	}
	#endregion

	public class WatchlistInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public bool AllRevisions { get; set; }

		public DateTime? End { get; set; }

		public bool ExcludeUser { get; set; }

		public Filter FilterAnonymous { get; set; }

		public Filter FilterBot { get; set; }

		public Filter FilterMinor { get; set; }

		public Filter FilterPatrolled { get; set; }

		public Filter FilterUnread { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int>? Namespaces { get; set; }

		public string? Owner { get; set; }

		public WatchlistProperties Properties { get; set; }

		public bool SortAscending { get; set; }

		public DateTime? Start { get; set; }

		public string? Token { get; set; }

		public WatchlistTypes Type { get; set; }

		public string? User { get; set; }
		#endregion
	}
}
