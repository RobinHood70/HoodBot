namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using WallE.Base;
	using static WikiCommon.Globals;

	public class RecentChange
	{
		/// <summary>Initializes a new instance of the <see cref="RecentChange"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="recentChange">The <see cref="RecentChangesItem"/> to initialize from.</param>
		protected internal RecentChange(Site site, RecentChangesItem recentChange)
		{
			ThrowNull(recentChange, nameof(recentChange));
			this.Title = new Title(site, recentChange.Title);
			this.Anonymous = recentChange.UserId == 0;
			this.Comment = recentChange.Comment;
			this.Id = recentChange.Id;
			this.Flags = recentChange.Flags;
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

		public RecentChangesFlags Flags { get; }

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
