#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

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

	public class WatchlistItem : ILogEvents, ITitle
	{
		#region Constructors
		public WatchlistItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public string Comment { get; set; }

		public IReadOnlyDictionary<string, object> ExtraData { get; set; }

		public WatchlistFlags Flags { get; set; }

		public string LogAction { get; set; }

		public LogEventFlags LogEventFlags { get; set; }

		public long LogId { get; set; }

		public string LogType { get; set; }

		public int Namespace { get; }

		public int NewLength { get; set; }

		public DateTime? NotificationTimestamp { get; set; }

		public int OldLength { get; set; }

		public long OldRevisionId { get; set; }

		public long PageId { get; }

		public string ParsedComment { get; set; }

		public long RevisionId { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; }

		public string User { get; set; }

		public long UserId { get; set; }

		public string WatchlistType { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
