namespace RobinHood70.Robby.Pages
{
	using System;
	using WallE.Base;
	using static WikiCommon.Globals;

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

		protected internal Revision()
		{
		}
		#endregion

		#region Public Properties
		public bool Anonymous { get; protected set; }

		public string Comment { get; protected set; }

		public long Id { get; protected set; }

		public bool Minor { get; protected set; }

		public long ParentId { get; protected set; }

		public string Text { get; protected set; }

		public DateTime Timestamp { get; protected set; }

		public string User { get; protected set; }
		#endregion
	}
}
