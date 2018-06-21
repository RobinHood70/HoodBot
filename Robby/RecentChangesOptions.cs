namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using WallE.Base;
	using WikiCommon;

	public class RecentChangesOptions
	{
		#region Public Properties

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they were made by an anonymous (IP) user or a logged-in user.</summary>
		public Filter Anonymous { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they were made by a bot.</summary>
		public Filter Bots { get; set; }

		public int Count { get; internal set; }

		public DateTime? End { get; set; }

		public bool ExcludeUser { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they're minor.</summary>
		public Filter Minor { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public bool Newer { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on patrol status.</summary>
		public Filter Patrolled { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they're redirects.</summary>
		public Filter Redirects { get; set; }

		public DateTime? Start { get; set; }

		public string Tag { get; set; }

		public RecentChangesTypes Types { get; set; }

		public string User { get; set; }
		#endregion

		#region Protected Internal Methods

		/// <summary>Gets a RecentChangesInput object based on the current object.</summary>
		/// <value>The equivalent RecentChangesInput object.</value>
		protected internal RecentChangesInput ToWallEInput => new RecentChangesInput()
		{
			Start = this.Start,
			End = this.End,
			SortAscending = this.Newer,
			User = this.User,
			ExcludeUser = this.ExcludeUser,
			Namespaces = this.Namespaces,
			Tag = this.Tag,
			Types = this.Types,
			FilterAnonymous = this.Anonymous,
			FilterBots = this.Bots,
			FilterMinor = this.Minor,
			FilterPatrolled = this.Patrolled,
			FilterRedirects = this.Redirects,
			MaxItems = this.Count,
		};
		#endregion
	}
}