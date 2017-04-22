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

	public class UserContributionsItem : ITitle
	{
		#region Public Properties
		public string Comment { get; set; }

		public UserContributionFlags Flags { get; set; }

		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public long ParentId { get; set; }

		public string ParsedComment { get; set; }

		public long RevisionId { get; set; }

		public int Size { get; set; }

		public int SizeDifference { get; set; }

		public IReadOnlyList<string> Tags { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
