#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum RevisionFlags
	{
		None = 0,
		Anonymous = 1,
		CommentHidden = 1 << 1,
		Minor = 1 << 2,
		Sha1Hidden = 1 << 3,
		Suppressed = 1 << 4,
		TextHidden = 1 << 5,
		UserHidden = 1 << 6
	}
	#endregion

	public class RevisionsItem
	{
		#region Public Properties
		public string? Comment { get; set; }

		public string? ContentFormat { get; set; }

		public string? ContentModel { get; set; }

		public RevisionFlags Flags { get; set; }

		public string? ParsedComment { get; set; }

		public string? ParseTree { get; set; }

		public long ParentId { get; set; }

		public long RevisionId { get; set; }

		public string? RollbackToken { get; set; }

		public string? Sha1 { get; set; }

		public long Size { get; set; }

		public IReadOnlyList<string>? Tags { get; set; }

		public string? Content { get; set; }

		public DateTime? Timestamp { get; set; }

		public long UserId { get; set; }

		public string? User { get; set; }
		#endregion
	}
}
