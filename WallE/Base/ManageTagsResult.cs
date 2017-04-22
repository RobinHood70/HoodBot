#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class ManageTagsResult
	{
		#region Public Properties
		public long LogId { get; set; }

		public string Operation { get; set; }

		public bool Success { get; set; }

		public string Tag { get; set; }

		public IReadOnlyList<string> Warnings { get; set; }
		#endregion
	}
}
