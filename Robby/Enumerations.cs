namespace RobinHood70.Robby
{
	using System;
	using WallE;
	using WallE.Base;

	#region Public Enumerations
	[Flags]
	public enum BacklinkTypes
	{
		None = 0,
		Links = AllLinksTypes.Links,
		FileUsages = AllLinksTypes.FileUsages,
		Redirects = AllLinksTypes.Redirects,
		Transclusions = AllLinksTypes.Transclusions,
		All = AllLinksTypes.All,
	}

	[Flags]
	public enum CategoryMemberTypes
	{
		None = CategoryTypes.None,
		Pages = CategoryTypes.Page,
		Subcategories = CategoryTypes.Subcat,
		Files = CategoryTypes.File,
		All = CategoryTypes.All,
	}

	public enum Filter
	{
		All = FilterOption.All,
		Filter = FilterOption.Filter,
		Only = FilterOption.Only,
	}

	[Flags]
	public enum RecentChangesFilters
	{
		None = 0,
		Anonymous = 1,
		Bot = 1 << 1,
		Minor = 1 << 2,
		Patrolled = 1 << 3,
		Redirect = 1 << 4,
		All,
	}

	[Flags]
	public enum RecentChangesTypes
	{
		None = ChangeTypes.None,
		Edit = ChangeTypes.Edit,
		External = ChangeTypes.External,
		New = ChangeTypes.New,
		Log = ChangeTypes.Log,
		All = Edit | External | New | Log
	}

	public enum WhatToSearch
	{
		Title = SearchWhat.Title,
		Text = SearchWhat.Text,
		NearMatch = SearchWhat.NearMatch,
	}
	#endregion
}
