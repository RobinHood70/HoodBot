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

	public class RevisionItem
	{
		#region Constructors
		internal RevisionItem(string? comment, string? content, string? contentFormat, string? contentModel, RevisionFlags flags, long parentId, string? parsedComment, string? parseTree, long revisionId, string? rollbackToken, string? sha1, long size, IReadOnlyList<string> tags, DateTime? timestamp, string? user, long userId)
		{
			this.Comment = comment;
			this.Content = content;
			this.ContentFormat = contentFormat;
			this.ContentModel = contentModel;
			this.Flags = flags;
			this.ParentId = parentId;
			this.ParsedComment = parsedComment;
			this.ParseTree = parseTree;
			this.RevisionId = revisionId;
			this.RollbackToken = rollbackToken;
			this.Sha1 = sha1;
			this.Size = size;
			this.Tags = tags;
			this.Timestamp = timestamp;
			this.User = user;
			this.UserId = userId;
		}
		#endregion

		#region Public Properties
		public string? Comment { get; }

		public string? Content { get; }

		public string? ContentFormat { get; }

		public string? ContentModel { get; }

		public RevisionFlags Flags { get; }

		public long ParentId { get; }

		public string? ParsedComment { get; }

		public string? ParseTree { get; }

		public long RevisionId { get; }

		public string? RollbackToken { get; }

		public string? Sha1 { get; }

		public long Size { get; }

		public IReadOnlyList<string>? Tags { get; }

		public DateTime? Timestamp { get; }

		public long UserId { get; }

		public string? User { get; }
		#endregion
	}
}