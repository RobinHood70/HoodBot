#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations
	[Flags]
	public enum PageFlags
	{
		None = 0,
		Invalid = 1,
		Missing = 1 << 1
	}
	#endregion

	public class PageItem : ITitle
	{
		#region Public Properties
		public long AnonContributors { get; set; }

		public IReadOnlyList<CategoriesItem> Categories { get; set; } = Array.Empty<CategoriesItem>();

		public CategoryInfoResult CategoryInfo { get; set; }

		public IReadOnlyList<ContributorItem> Contributors { get; set; } = Array.Empty<ContributorItem>();

		/// <summary>Gets custom module results. This collection is unused by the framework and exists for any custom property modules the user might implement.</summary>
		/// <value>The custom page information.</value>
		/// <remarks>Module results can be added here as needed, then unboxed again when they reach the caller. Pages can, of course, also be inherited with custom property module results added, but this will likely be a far easier method in the long run.</remarks>
		public IList<object> CustomPageInfo { get; } = new List<object>();

		public IReadOnlyList<RevisionsItem> DeletedRevisions { get; set; } = Array.Empty<RevisionsItem>();

		public IReadOnlyList<DuplicateFileItem> DuplicateFiles { get; set; } = Array.Empty<DuplicateFileItem>();

		public IReadOnlyList<string> ExternalLinks { get; set; } = Array.Empty<string>();

		public IReadOnlyList<FileUsageItem> FileUsages { get; set; } = Array.Empty<FileUsageItem>();

		public PageFlags Flags { get; set; }

		public IReadOnlyList<ImageInfoItem> ImageInfoEntries { get; set; } = Array.Empty<ImageInfoItem>();

		public IReadOnlyList<ITitle> Images { get; set; } = Array.Empty<ITitle>();

		public string ImageRepository { get; set; }

		public PageInfo Info { get; set; }

		public IReadOnlyList<InterwikiTitleItem> InterwikiLinks { get; set; } = Array.Empty<InterwikiTitleItem>();

		public IReadOnlyList<LanguageLinksItem> LanguageLinks { get; set; } = Array.Empty<LanguageLinksItem>();

		public IReadOnlyList<ITitle> Links { get; set; } = Array.Empty<ITitle>();

		public IReadOnlyList<LinksHereItem> LinksHere { get; set; } = Array.Empty<LinksHereItem>();

		public int? Namespace { get; set; }

		public IReadOnlyDictionary<string, string> Properties { get; set; } = EmptyReadOnlyDictionary<string, string>();

		public long PageId { get; set; }

		public IReadOnlyList<RedirectsItem> Redirects { get; set; } = Array.Empty<RedirectsItem>();

		public IReadOnlyList<RevisionsItem> Revisions { get; set; } = Array.Empty<RevisionsItem>();

		public IReadOnlyList<ITitle> Templates { get; set; } = Array.Empty<ITitle>();

		public string Title { get; set; }

		public IReadOnlyList<TranscludedInItem> TranscludedIn { get; set; } = Array.Empty<TranscludedInItem>();
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
