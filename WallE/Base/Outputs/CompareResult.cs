#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class CompareResult
	{
		#region Constructors
		internal CompareResult(string? body, int fromId, int fromRevision, string? fromTitle, int toId, int toRevision, string? toTitle)
		{
			this.Body = body;
			this.FromId = fromId;
			this.FromRevision = fromRevision;
			this.FromTitle = fromTitle;
			this.ToId = toId;
			this.ToRevision = toRevision;
			this.ToTitle = toTitle;
		}
		#endregion

		#region Public Properties
		public string? Body { get; }

		public long FromId { get; }

		public long FromRevision { get; }

		public string? FromTitle { get; }

		public long ToId { get; }

		public long ToRevision { get; }

		public string? ToTitle { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.FromTitle ?? this.ToTitle ?? Globals.Unknown;
		#endregion
	}
}
