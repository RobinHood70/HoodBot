#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum AllLinksProperties
	{
		None = 0,
		Ids = 1,
		Title = 1 << 1,
		Fragment = 1 << 2,
		Interwiki = 1 << 3,
		All = Ids | Title | Fragment | Interwiki
	}

	[Flags]
	public enum AllLinksTypes
	{
		None = 0,
		Links = 1,
		FileUsages = 1 << 1,
		Redirects = 1 << 2,
		Transclusions = 1 << 3,
		All = Links | FileUsages | Redirects | Transclusions
	}
	#endregion

	public class AllLinksInput : IAllLinksInput
	{
		#region Public Properties
		public string? From { get; set; }

		public int Limit { get; set; }

		public AllLinksTypes LinkType { get; } = AllLinksTypes.Links;

		public int MaxItems { get; set; }

		public int? Namespace { get; set; }

		public string? Prefix { get; set; }

		public AllLinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string? To { get; set; }

		public bool Unique { get; set; }
		#endregion
	}
}