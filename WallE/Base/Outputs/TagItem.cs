#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class TagItem
	{
		#region Constructors
		internal TagItem(string status, long actionLogId, IReadOnlyList<string> added, ErrorItem? error, long logId, bool noOperation, long recentChangesId, IReadOnlyList<string> removed, long revisionId)
		{
			this.Status = status;
			this.ActionLogId = actionLogId;
			this.Added = added;
			this.Error = error;
			this.LogId = logId;
			this.NoOperation = noOperation;
			this.RecentChangesId = recentChangesId;
			this.Removed = removed;
			this.RevisionId = revisionId;
		}
		#endregion

		#region Public Properties
		public long ActionLogId { get; }

		public IReadOnlyList<string> Added { get; }

		public ErrorItem? Error { get; }

		// IMPNOTE: Commented out for now, as it couldn't be tested. There doesn't seem to be anything that can produce this - all errors emitted are fatal.
		//// public IReadOnlyDictionary<string, string> Errors { get; }

		public long LogId { get; }

		public bool NoOperation { get; }

		public long RecentChangesId { get; }

		public IReadOnlyList<string> Removed { get; }

		public long RevisionId { get; }

		public string Status { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Status;
		#endregion
	}
}
