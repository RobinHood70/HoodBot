#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class AllRevisionsItem : ITitle
	{
		#region Public Properties
		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public IReadOnlyList<RevisionsItem> Revisions { get; set; } = Array.Empty<RevisionsItem>();

		public string Title { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
