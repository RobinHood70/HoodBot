#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum UserContributionFlags
	{
		None = 0,
		CommentHidden = 1,
		Minor = 1 << 1,
		New = 1 << 2,
		Patrolled = 1 << 3,
		Suppressed = 1 << 4,
		TextHidden = 1 << 5,
		Top = 1 << 6,
		UserHidden = 1 << 7
	}
	#endregion

	public class UserContributionsItem
	{
		#region Constructors
		internal UserContributionsItem(string user, long userId, int? ns, string? title, long pageId, string? comment, UserContributionFlags flags, long parentId, string? parsedComment, long revId, int size, int sizeDiff, IReadOnlyList<string> tags, DateTime? timestamp)
		{
			this.User = user;
			this.UserId = userId;
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Comment = comment;
			this.Flags = flags;
			this.ParentId = parentId;
			this.ParsedComment = parsedComment;
			this.RevisionId = revId;
			this.Size = size;
			this.SizeDifference = sizeDiff;
			this.Tags = tags;
			this.Timestamp = timestamp;
		}
		#endregion

		#region Public Properties
		public string? Comment { get; }

		public UserContributionFlags Flags { get; }

		public int? Namespace { get; }

		public long PageId { get; }

		public long ParentId { get; }

		public string? ParsedComment { get; }

		public long RevisionId { get; }

		public int Size { get; }

		public int SizeDifference { get; }

		public IReadOnlyList<string> Tags { get; }

		public DateTime? Timestamp { get; }

		public string? Title { get; }

		public string User { get; }

		public long UserId { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? this.User ?? ProjectGlobals.NoTitle;
		#endregion
	}
}
