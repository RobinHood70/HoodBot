#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum WatchlistFlags
	{
		None = 0,
		Bot = 1,
		Minor = 1 << 1,
		New = 1 << 2,
		Patrolled = 1 << 3,
		Unpatrolled = 1 << 4
	}
	#endregion

	public class WatchlistItem : LogEvent, IApiTitleOptional
	{
		#region Constructors
		internal WatchlistItem(string watchlistType, int? ns, string? title, WatchlistFlags flags, int newLength, int oldLength, long oldRevisionId, long revisionId)
		{
			this.WatchlistType = watchlistType;
			this.Namespace = ns;
			this.Title = title;
			this.Flags = flags;
			this.NewLength = newLength;
			this.OldLength = oldLength;
			this.OldRevisionId = oldRevisionId;
			this.RevisionId = revisionId;
		}
		#endregion

		#region Public Properties
		public WatchlistFlags Flags { get; }

		public int? Namespace { get; }

		public int NewLength { get; }

		public DateTime? NotificationTimestamp { get; }

		public int OldLength { get; }

		public long OldRevisionId { get; }

		public long RevisionId { get; }

		public string? Title { get; }

		public string WatchlistType { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? FallbackText.NoTitle;
		#endregion
	}
}
