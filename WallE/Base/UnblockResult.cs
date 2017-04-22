#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class UnblockResult
	{
		#region Public Properties
		public long Id { get; set; }

		public string Reason { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion
	}
}
