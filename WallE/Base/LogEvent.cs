#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum LogEventFlags
	{
		None = 0,
		ActionHidden = 1,
		CommentHidden = 1 << 1,
		Suppressed = 1 << 2,
		UserAnonymous = 1 << 3,
		UserHidden = 1 << 4
	}

	// Buried in ExtraData property for revision deletion events.
	[Flags]
	public enum RevisionDeleteTypes
	{
		None = 0,
		Text = 1,
		Comment = 1 << 1,
		User = 1 << 2,
		Restricted = 1 << 3,
		SuppressedUser = User | Restricted,
		All = Text | Comment | User | Restricted
	}
	#endregion

	public class LogEvent
	{
		#region Constructors
		internal LogEvent()
		{
		}
		#endregion

		#region Public Properties
		public string? Comment { get; internal set; }

		public IReadOnlyDictionary<string, object?>? ExtraData { get; internal set; }

		public string? LogAction { get; internal set; }

		public LogEventFlags LogEventFlags { get; internal set; }

		public long LogId { get; internal set; }

		public string? LogType { get; internal set; }

		public long PageId { get; internal set; }

		public string? ParsedComment { get; internal set; }

		public DateTime? Timestamp { get; internal set; }

		public string? User { get; internal set; }

		public long UserId { get; internal set; }
		#endregion
	}
}