#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class TagItem
	{
		#region Public Properties
		public long ActionLogId { get; set; }

		public IReadOnlyList<string> Added { get; set; }

		public ErrorItem Error { get; set; }

		// IMPNOTE: Commented out for now, as it couldn't be tested. There doesn't seem to be anything that can produce this - all errors emitted are fatal.
		//// public IReadOnlyDictionary<string, string> Errors { get; set; }

		public long LogId { get; set; }

		public bool NoOperation { get; set; }

		public long RecentChangesId { get; set; }

		public IReadOnlyList<string> Removed { get; set; }

		public long RevisionId { get; set; }

		public string Status { get; set; }
		#endregion
	}
}
