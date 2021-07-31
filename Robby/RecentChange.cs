﻿namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

	/// <summary>Stores information about a Recent Change entry.</summary>
	public class RecentChange
	{
		/// <summary>Initializes a new instance of the <see cref="RecentChange"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="recentChange">The <see cref="RecentChangesItem"/> to initialize from.</param>
		protected internal RecentChange(Site site, RecentChangesItem recentChange)
		{
			this.Title = TitleFactory.FromApi(site, recentChange).ToTitle();
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
			this.User = recentChange.User == null ? null : new User(site, recentChange.User);
		}

		#region Public Properties

		/// <summary>Gets a value indicating whether the change was performed by an IP editor.</summary>
		/// <value><see langword="true"/> if the change was performed by an IP editor; otherwise, <see langword="false"/>.</value>
		public bool Anonymous { get; }

		/// <summary>Gets the edit summary/change comment.</summary>
		/// <value>The edit summary/change comment.</value>
		public string? Comment { get; }

		/// <summary>Gets the Recent Change flags.</summary>
		/// <value>The Recent Change flags.</value>
		public RecentChangesFlags Flags { get; }

		/// <summary>Gets the Recent Change ID (rcid).</summary>
		/// <value>The Recent Change ID.</value>
		public long Id { get; }

		/// <summary>Gets the log action.</summary>
		/// <value>The log action.</value>
		public string? LogAction { get; }

		/// <summary>Gets the log entry type.</summary>
		/// <value>The log entry type.</value>
		public string? LogType { get; }

		/// <summary>Gets the new size.</summary>
		/// <value>The new size.</value>
		public int NewSize { get; }

		/// <summary>Gets the size before the change.</summary>
		/// <value>The size before the change.</value>
		public int OldSize { get; }

		/// <summary>Gets the revision ID before the change.</summary>
		/// <value>The revision ID before the change.</value>
		public long OldRevisionId { get; }

		/// <summary>Gets the type of the change.</summary>
		/// <value>The type of the change.</value>
		public string? RecentChangeType { get; }

		/// <summary>Gets the revision ID of the change.</summary>
		/// <value>The revision ID of the change.</value>
		public long RevisionId { get; }

		/// <summary>Gets the revision tags.</summary>
		/// <value>The revision tags.</value>
		public IReadOnlyList<string> Tags { get; }

		/// <summary>Gets the date and time of the change.</summary>
		/// <value>The date and time of the change.</value>
		public DateTime Timestamp { get; }

		/// <summary>Gets the title of the change.</summary>
		/// <value>The title of the change.</value>
		public Title Title { get; }

		/// <summary>Gets the user who made the change.</summary>
		/// <value>The user who made the change.</value>
		public User? User { get; }
		#endregion
	}
}
