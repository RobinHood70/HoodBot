#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using WikiCommon;

	public class CategoryMembersItem : ITitle
	{
		#region Public Properties
		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public string SortKey { get; set; }

		public string SortKeyPrefix { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; set; }

		public CategoryMemberTypes Type { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
