#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class RevisionDeleteItem
	{
		#region Public Properties
		public IReadOnlyList<string> Errors { get; set; }

		public long Id { get; set; }

		public string Status { get; set; }

		public IReadOnlyList<string> Warnings { get; set; }
		#endregion
	}
}
