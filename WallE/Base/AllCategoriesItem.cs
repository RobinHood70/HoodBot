﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class AllCategoriesItem : ITitle
	{
		#region Public Properties
		public int Files { get; set; }

		public bool Hidden { get; set; }

		public int? Namespace
		{
			get => MediaWikiNamespaces.Category;
			set => throw new InvalidOperationException(CurrentCulture(Messages.NotSettable));
		}

		public long PageId { get; set; }

		public int Pages { get; set; }

		public int Size { get; set; }

		public int Subcategories { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
