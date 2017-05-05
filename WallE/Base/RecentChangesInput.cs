#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum RecentChangesProperties
	{
		None = 0,
		User = 1,
		UserId = 1 << 1,
		Comment = 1 << 2,
		ParsedComment = 1 << 3,
		Timestamp = 1 << 4,
		Title = 1 << 5,
		Ids = 1 << 6,
		//// Sha1 = 1 << 7, Not implemented because I see no bot use for it here TODO: Re-examine if this needs to be enabled.
		Sizes = 1 << 8,
		Redirect = 1 << 9,
		Patrolled = 1 << 10,
		LogInfo = 1 << 11,
		Tags = 1 << 12,
		Flags = 1 << 13,
		All = User | UserId | Comment | ParsedComment | Timestamp | Title | Ids | Sizes | Redirect | Patrolled | LogInfo | Tags | Flags
	}
	#endregion

	public class RecentChangesInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public DateTime? End { get; set; }

		public bool ExcludeUser { get; set; }

		public Filter FilterAnonymous { get; set; }

		public Filter FilterBot { get; set; }

		public Filter FilterMinor { get; set; }

		public Filter FilterPatrolled { get; set; }

		public Filter FilterRedirects { get; set; }

		public bool GetPatrolToken { get; set; }

		public int ItemsRemaining { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public int? Namespace { get; set; }

		public RecentChangesProperties Properties { get; set; }

		public bool SortAscending { get; set; }

		public DateTime? Start { get; set; }

		public string Tag { get; set; }

		public bool TopOnly { get; set; }

		public RecentChangesTypes Types { get; set; }

		public string User { get; set; }
		#endregion
	}
}
