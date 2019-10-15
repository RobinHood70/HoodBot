#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum RecentChangesFlags
	{
		None = 0,
		Bot = 1,
		Minor = 1 << 1,
		New = 1 << 2,
		Redirect = 1 << 3
	}
	#endregion

	/// <summary>Holds all data for an entry from Special:RecentChanges. Note that a Recent Change is, in essence, a log entry with a few extra properties and is therefore modeled that way. Since log entries can be derived types, themselves, the LogEvent property holds the specific LogEvent derivative, when appropriate, or a base LogEvent object for normal edits.</summary>
	public class RecentChangesItem : ILogEvents
	{
		#region Constructors
		public RecentChangesItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public string Comment { get; set; }

		public IReadOnlyDictionary<string, object> ExtraData { get; set; }

		public RecentChangesFlags Flags { get; set; }

		public long Id { get; set; }

		public string LogAction { get; set; }

		public LogEventFlags LogEventFlags { get; set; }

		public long LogId { get; set; }

		public string LogType { get; set; }

		public int? Namespace { get; }

		public int NewLength { get; set; }

		public int OldLength { get; set; }

		public long OldRevisionId { get; set; }

		public long PageId { get; }

		public string ParsedComment { get; set; }

		public string PatrolToken { get; set; }

		public string RecentChangeType { get; set; }

		public long RevisionId { get; set; }

		public IReadOnlyList<string> Tags { get; set; }

		public DateTime? Timestamp { get; set; }

		public string? Title { get; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
