#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class RollbackResult
	{
		#region Public Properties
		public long LastRevisionId { get; set; }

		public long OldRevisionId { get; set; }

		public long PageId { get; set; }

		public long RevisionId { get; set; }

		public string Summary { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
