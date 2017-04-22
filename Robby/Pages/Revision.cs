namespace RobinHood70.Robby.Pages
{
	using System;
	using WallE.Base;
	using static Globals;

	public class Revision
	{
		#region Constructors
		public Revision(RevisionsItem revisionData)
		{
			ThrowNull(revisionData, nameof(revisionData));
			this.Anonymous = revisionData.Flags.HasFlag(RevisionFlags.Anonymous);
			this.Comment = revisionData.Comment;
			this.Id = revisionData.RevisionId;
			this.Minor = revisionData.Flags.HasFlag(RevisionFlags.Minor);
			this.ParentId = revisionData.ParentId;
			this.Text = revisionData.Content;
			this.Timestamp = revisionData.Timestamp.Value;
			this.User = revisionData.User;
		}
		#endregion

		#region Public Properties
		public bool Anonymous { get; }

		public string Comment { get; }

		public long Id { get; }

		public bool Minor { get; }

		public long ParentId { get; }

		public string Text { get; }

		public DateTime Timestamp { get; }

		public string User { get; }
		#endregion
	}
}
