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

	public interface ILogEvents : ITitle
	{
		#region Public Properties
		string Comment { get; set; }

		IReadOnlyDictionary<string, object> ExtraData { get; set; }

		LogEventFlags LogEventFlags { get; set; }

		string LogAction { get; set; }

		long LogId { get; set; }

		string LogType { get; set; }

		string ParsedComment { get; set; }

		DateTime? Timestamp { get; set; }

		string User { get; set; }

		long UserId { get; set; }
		#endregion
	}
}
