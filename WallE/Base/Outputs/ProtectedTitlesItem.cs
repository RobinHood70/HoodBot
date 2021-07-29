#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class ProtectedTitlesItem : IApiTitle
	{
		#region Constructors
		public ProtectedTitlesItem(int ns, string title, string? comment, DateTime? expiry, string? level, string? parsedComment, DateTime? timestamp, string? user, long userId)
		{
			this.Namespace = ns;
			this.FullPageName = title;
			this.Comment = comment;
			this.Expiry = expiry;
			this.Level = level;
			this.ParsedComment = parsedComment;
			this.Timestamp = timestamp;
			this.User = user;
			this.UserId = userId;
		}
		#endregion

		#region Public Properties
		public string? Comment { get; }

		public DateTime? Expiry { get; }

		public string? Level { get; }

		public int Namespace { get; }

		public string? ParsedComment { get; }

		public DateTime? Timestamp { get; }

		public string FullPageName { get; }

		public string? User { get; }

		public long UserId { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.FullPageName;
		#endregion
	}
}
