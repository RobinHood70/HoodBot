#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class AllTransclusionsInput : IAllLinksInput
	{
		#region Public Properties
		public string? From { get; set; }

		public int Limit { get; set; }

		public AllLinksTypes LinkType { get; } = AllLinksTypes.Transclusions;

		public int MaxItems { get; set; }

		public int? Namespace { get; set; } = MediaWikiNamespaces.Template;

		public string? Prefix { get; set; }

		public AllLinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string? To { get; set; }

		public bool Unique { get; set; }
		#endregion
	}
}