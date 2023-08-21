#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class CategoryMembersItem : IApiTitleOptional
	{
		#region Constructors
		internal CategoryMembersItem(int? ns, string? title, long pageId, string? sortKey, string? sortKeyPrfix, DateTime? timestamp, CategoryMemberTypes type)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.SortKey = sortKey;
			this.SortKeyPrefix = sortKeyPrfix;
			this.Timestamp = timestamp;
			this.Type = type;
		}
		#endregion

		#region Public Properties
		public int? Namespace { get; }

		public long PageId { get; }

		public string? SortKey { get; }

		public string? SortKeyPrefix { get; }

		public DateTime? Timestamp { get; }

		public string? Title { get; }

		public CategoryMemberTypes Type { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? FallbackText.NoTitle;
		#endregion
	}
}
