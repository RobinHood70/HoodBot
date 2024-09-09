﻿namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;

	/// <summary>Stores all information related to a specific revision.</summary>
	/// <remarks>Revisions can apply to users or pages. Pages store title information at the parent level, thus they are not included in the base Revision object.</remarks>
	/// <exception cref="ChecksumException">Thrown when the SHA-1 checksum does not match the text of the revision.</exception>
	public class Revision
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Revision"/> class from a UserContributionsItem.</summary>
		/// <param name="contributionItem">The <see cref="UserContributionsItem"/>.</param>
		protected internal Revision(UserContributionsItem contributionItem)
		{
			ArgumentNullException.ThrowIfNull(contributionItem);
			this.Anonymous = contributionItem.UserId == 0;
			this.Comment = contributionItem.Comment;
			this.Id = contributionItem.RevisionId;
			this.Minor = contributionItem.Flags.HasAnyFlag(UserContributionFlags.Minor);
			this.ParentId = contributionItem.ParentId;
			this.Text = null;
			this.Timestamp = contributionItem.Timestamp;
			this.User = contributionItem.User;
		}

		/// <summary>Initializes a new instance of the <see cref="Revision"/> class from a UserContributionsItem.</summary>
		/// <param name="revisionItem">The <see cref="RevisionItem"/>.</param>
		protected internal Revision(RevisionItem revisionItem)
		{
			ArgumentNullException.ThrowIfNull(revisionItem);
			this.Anonymous = revisionItem.Flags.HasAnyFlag(RevisionFlags.Anonymous);
			this.Comment = revisionItem.Comment;
			this.Id = revisionItem.RevisionId;
			this.Minor = revisionItem.Flags.HasAnyFlag(RevisionFlags.Minor);
			this.ParentId = revisionItem.ParentId;
			this.Timestamp = revisionItem.Timestamp;
			this.User = revisionItem.User;

			if (revisionItem.Slots.TryGetValue("main", out var mainSlot))
			{
				if (
					mainSlot.Content is not null &&
					!string.IsNullOrEmpty(mainSlot.Sha1) &&
					!string.Equals(mainSlot.Content.GetHash(HashType.Sha1), mainSlot.Sha1, StringComparison.Ordinal))
				{
					throw new ChecksumException(Globals.CurrentCulture(Resources.RevisionSha1Failed, revisionItem.RevisionId));
				}

				this.Text = mainSlot.Content;
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="Revision" /> is anonymous.</summary>
		/// <value><see langword="true" /> if it's anonymous; otherwise, <see langword="false" />.</value>
		public bool Anonymous { get; }

		/// <summary>Gets the revision comment.</summary>
		/// <value>The comment.</value>
		public string? Comment { get; }

		/// <summary>Gets the revision ID.</summary>
		/// <value>The revision ID.</value>
		public long Id { get; }

		/// <summary>Gets a value indicating whether this <see cref="Revision" /> is minor.</summary>
		/// <value><see langword="true" /> if it's minor; otherwise, <see langword="false" />.</value>
		public bool Minor { get; }

		/// <summary>Gets the parent (previous) revision ID.</summary>
		/// <value>The parent identifier.</value>
		public long ParentId { get; }

		/// <summary>Gets the revision text.</summary>
		/// <value>The revision text.</value>
		public string? Text { get; }

		/// <summary>Gets the timestamp of the revision.</summary>
		/// <value>The timestamp of the revision.</value>
		public DateTime? Timestamp { get; }

		/// <summary>Gets the user who made the revision.</summary>
		/// <value>The user who made the revision.</value>
		public string? User { get; }
		#endregion
	}
}