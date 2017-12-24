#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class BlockResult
	{
		#region Public Properties
		public DateTime? Expiry { get; set; }

		public BlockUserFlags Flags { get; set; }

		public long Id { get; set; }

		public string Reason { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion
	}
}
