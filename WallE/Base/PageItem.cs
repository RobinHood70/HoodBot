#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using static RobinHood70.Globals;

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

		public IReadOnlyList<CategoriesItem> Categories { get; set; } = new CategoriesItem[0];

		public CategoryInfoResult CategoryInfo { get; set; }

		public IReadOnlyList<ContributorItem> Contributors { get; set; } = new ContributorItem[0];

		/// <summary>Gets custom module results. This collection is unused by the framework and exists for any custom property modules the user might implement.</summary>
		/// <value>The custom page information.</value>
		/// <remarks>Module results can be added here as needed, then unboxed again when they reach the caller. Pages can, of course, also be inherited with custom property module results added, but this will likely be a far easier method in the long run.</remarks>
		public IList<object> CustomPageInfo { get; } = new List<object>();

		public IReadOnlyList<RevisionsItem> DeletedRevisions { get; set; } = new RevisionsItem[0];

		public IReadOnlyList<DuplicateFileItem> DuplicateFiles { get; set; } = new DuplicateFileItem[0];

		public IReadOnlyList<string> ExternalLinks { get; set; } = new string[0];

		public IReadOnlyList<FileUsageItem> FileUsages { get; set; } = new FileUsageItem[0];

		public PageFlags Flags { get; set; }

		public IReadOnlyList<ImageInfoItem> ImageInfoEntries { get; set; } = new ImageInfoItem[0];

		public IReadOnlyList<ITitle> Images { get; set; } = new ITitle[0];

		public string ImageRepository { get; set; }

		public PageInfo Info { get; set; }

		public IReadOnlyList<InterwikiTitleItem> InterwikiLinks { get; set; } = new InterwikiTitleItem[0];

		public IReadOnlyList<LanguageLinksItem> LanguageLinks { get; set; } = new LanguageLinksItem[0];

		public IReadOnlyList<ITitle> Links { get; set; } = new ITitle[0];

		public IReadOnlyList<LinksHereItem> LinksHere { get; set; } = new LinksHereItem[0];

		public int? Namespace { get; set; }

		public IReadOnlyDictionary<string, string> Properties { get; set; } = EmptyReadOnlyDictionary<string, string>();

		public long PageId { get; set; }

		public IReadOnlyList<RedirectsItem> Redirects { get; set; } = new RedirectsItem[0];

		public IReadOnlyList<RevisionsItem> Revisions { get; set; } = new RevisionsItem[0];

		public IReadOnlyList<ITitle> Templates { get; set; } = new ITitle[0];

		public string Title { get; set; }

		public IReadOnlyList<TranscludedInItem> TranscludedIn { get; set; } = new TranscludedInItem[0];
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
