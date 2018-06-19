namespace RobinHood70.Robby
{
	using System;
	using WallE.Base;
	using static WikiCommon.Globals;

	/// <summary>Stores all information related to a specific revision.</summary>
	/// <remarks>Revisions can apply to users or pages. Pages store title information at the parent level, thus they are not included in the base Revision object.</remarks>
	public class Revision
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Revision" /> class.</summary>
		/// <param name="anonymous">Whether the revision was made by an anonymous user.</param>
		/// <param name="comment">The revision comment.</param>
		/// <param name="id">The revision ID.</param>
		/// <param name="minor">Whether the revision is minor.</param>
		/// <param name="parentId">The parent revision ID.</param>
		/// <param name="text">The revision text.</param>
		/// <param name="timestamp">When the revision was made.</param>
		/// <param name="user">The user who made the revision.</param>
		protected internal Revision(bool anonymous, string comment, long id, bool minor, long parentId, string text, DateTime? timestamp, string user)
		{
			this.Anonymous = anonymous;
			this.Comment = comment;
			this.Id = id;
			this.Minor = minor;
			this.ParentId = parentId;
			this.Text = text;
			this.Timestamp = timestamp ?? DateTime.MinValue;
			this.User = user;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="Revision" /> is anonymous.</summary>
		/// <value><see langword="true" /> if it's anonymous; otherwise, <see langword="false" />.</value>
		public bool Anonymous { get; }

		/// <summary>Gets the revision comment.</summary>
		/// <value>The comment.</value>
		public string Comment { get; }

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
		public string Text { get; }

		/// <summary>Gets the timestamp of the revision.</summary>
		/// <value>The timestamp of the revision.</value>
		public DateTime Timestamp { get; }

		/// <summary>Gets the user who made the revision.</summary>
		/// <value>The user who made the revision.</value>
		public string User { get; }
		#endregion
	}
}