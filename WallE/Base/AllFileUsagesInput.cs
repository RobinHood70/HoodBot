#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class AllFileUsagesInput : IAllLinksInput
	{
		#region Constructors
		public AllFileUsagesInput()
		{
		}
		#endregion

		#region Public Properties
		public string? From { get; set; }

		public int Limit { get; set; }

		public AllLinksTypes LinkType { get; } = AllLinksTypes.FileUsages;

		public int MaxItems { get; set; }

		public int? Namespace { get; } = MediaWikiNamespaces.File;

		public string? Prefix { get; set; }

		public AllLinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string? To { get; set; }

		public bool Unique { get; set; }
		#endregion
	}
}