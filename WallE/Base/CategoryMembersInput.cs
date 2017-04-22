﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum CategoryMembersProperties
	{
		None = 0,
		Ids = 1,
		Title = 1 << 1,
		SortKey = 1 << 2,
		SortKeyPrefix = 1 << 3,
		Type = 1 << 4,
		Timestamp = 1 << 5,
		All = Ids | Title | SortKey | SortKeyPrefix | Type | Timestamp
	}

	public enum CategoryMembersSort
	{
		SortKey,
		Timestamp
	}

	[Flags]
	public enum CategoryTypes
	{
		None = 0,
		Page = 1,
		Subcat = 1 << 1,
		File = 1 << 2,
		All = Page | Subcat | File
	}
	#endregion

	public class CategoryMembersInput : ILimitableInput, IGeneratorInput
	{
		#region Constructors
		public CategoryMembersInput(string title) => this.Title = title;

		public CategoryMembersInput(long pageId) => this.PageId = pageId;
		#endregion

		#region Public Properties
		public DateTime? End { get; set; }

		public string EndHexSortKey { get; set; }

		public string EndSortKeyPrefix { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public long PageId { get; }

		public CategoryMembersProperties Properties { get; set; }

		public CategoryMembersSort Sort { get; set; }

		public bool SortDescending { get; set; }

		public DateTime? Start { get; set; }

		public string StartHexSortKey { get; set; }

		public string StartSortKeyPrefix { get; set; }

		public string Title { get; }

		public CategoryTypes Type { get; set; }
		#endregion
	}
}
