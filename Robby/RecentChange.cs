namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;

	public enum RecentChangeFlags
	{
		None = RecentChangesFlags.None,
		Bot = RecentChangesFlags.Bot,
		Minor = RecentChangesFlags.Minor,
		New = RecentChangesFlags.None,
		Redirect = RecentChangesFlags.Redirect
	}

	public class RecentChange
	{
		public RecentChange(Site site, RecentChangesItem recentChange)
		{
			this.Title = new Title(site, recentChange.Title);
			this.Anonymous = recentChange.UserId == 0;
			this.Comment = recentChange.Comment;
			this.Id = recentChange.Id;
			this.Flags = (RecentChangeFlags)recentChange.Flags;
			this.LogAction = recentChange.LogAction;
			this.LogType = recentChange.LogType;
			this.NewSize = recentChange.NewLength;
			this.OldSize = recentChange.OldLength;
			this.OldRevisionId = recentChange.OldRevisionId;
			this.RecentChangeType = recentChange.RecentChangeType;
			this.RevisionId = recentChange.RevisionId;
			this.Tags = recentChange.Tags;
			this.Timestamp = recentChange.Timestamp ?? DateTime.MinValue;
			this.User = recentChange.User;
		}

		#region Public Properties
		public bool Anonymous { get; }

		public string Comment { get; }

		public RecentChangeFlags Flags { get; }

		public long Id { get; }

		public string LogAction { get; }

		public string LogType { get; }

		public int NewSize { get; }

		public int OldSize { get; }

		public long OldRevisionId { get; }

		public string RecentChangeType { get; }

		public IReadOnlyList<string> Tags { get; }

		public long RevisionId { get; }

		public string Text { get; }

		public DateTime Timestamp { get; }

		public Title Title { get; }

		public string User { get; }
		#endregion
	}
}
