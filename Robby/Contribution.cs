namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using static WikiCommon.Globals;

	public class Contribution : Revision
	{
		protected internal Contribution(Site site, UserContributionsItem contribution)
			: base()
		{
			ThrowNull(contribution, nameof(contribution));
			this.Title = new Title(site, contribution.Title);

			this.Anonymous = contribution.UserId == 0;
			this.Comment = contribution.Comment;
			this.Id = contribution.RevisionId;
			this.Minor = contribution.Flags.HasFlag(UserContributionFlags.Minor);
			this.ParentId = 0;
			this.Text = null;
			this.Timestamp = contribution.Timestamp ?? DateTime.MinValue;
			this.User = contribution.User;

			this.New = contribution.Flags.HasFlag(UserContributionFlags.New);
			this.Patrolled = contribution.Flags.HasFlag(UserContributionFlags.Patrolled);
			this.NewSize = contribution.Size;
			this.OldSize = contribution.Size - contribution.SizeDifference;
			this.Tags = contribution.Tags;
		}

		public Namespace Namespace { get; }

		public bool New { get; }

		public bool Patrolled { get; }

		public int NewSize { get; }

		public int OldSize { get; }

		public IReadOnlyList<string> Tags { get; }

		public Title Title { get; }
	}
}
