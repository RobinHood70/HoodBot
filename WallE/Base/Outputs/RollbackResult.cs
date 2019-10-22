#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class RollbackResult : ITitleOptional
	{
		#region Constructors
		internal RollbackResult(string title, long pageId, string summary, long revisionId, long oldRevisionId, long lastRevisionId)
		{
			this.Title = title;
			this.PageId = pageId;
			this.Summary = summary;
			this.RevisionId = revisionId;
			this.OldRevisionId = oldRevisionId;
			this.LastRevisionId = lastRevisionId;
		}
		#endregion

		#region Public Properties
		public long LastRevisionId { get; }

		public long OldRevisionId { get; }

		public long PageId { get; }

		public long RevisionId { get; }

		public string Summary { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
