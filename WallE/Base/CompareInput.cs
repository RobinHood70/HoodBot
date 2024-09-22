#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class CompareInput
	{
		#region Public Properties
		public long FromId { get; set; }

		public long FromRevision { get; set; }

		public string? FromTitle { get; set; }

		public long ToId { get; set; }

		public long ToRevision { get; set; }

		public string? ToTitle { get; set; }
		#endregion
	}
}