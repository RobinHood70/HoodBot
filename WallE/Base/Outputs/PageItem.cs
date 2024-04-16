#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum PageFlags
	{
		None = 0,
		Invalid = 1,
		Missing = 1 << 1
	}
	#endregion

	public class PageItem : IApiTitle
	{
		#region Constructors
		public PageItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public long AnonContributors { get; internal set; }

		public IReadOnlyList<CategoriesItem> Categories { get; } = [];

		public CategoryInfoResult? CategoryInfo { get; internal set; }

		public IReadOnlyList<ContributorItem> Contributors { get; } = [];

		/// <summary>Gets custom module results. This collection is unused by the framework and exists for any custom property modules the user might implement.</summary>
		/// <value>The custom page information.</value>
		/// <remarks>Module results can be added here as needed, then unboxed again when they reach the caller. Pages can, of course, also be inherited with custom property module results added, but this will likely be a far easier method in the long run.</remarks>
		public IList<object> CustomPageInfo { get; } = [];

		public IReadOnlyList<RevisionItem> DeletedRevisions { get; } = [];

		public IReadOnlyList<DuplicateFileItem> DuplicateFiles { get; } = [];

		public IReadOnlyList<string> ExternalLinks { get; } = [];

		public IReadOnlyList<FileUsageItem> FileUsages { get; } = [];

		public PageFlags Flags { get; internal set; }

		public IReadOnlyList<ImageInfoItem> ImageInfoEntries { get; } = [];

		public IReadOnlyList<IApiTitle> Images { get; } = [];

		public string? ImageRepository { get; internal set; }

		public PageInfo? Info { get; internal set; }

		public IReadOnlyList<InterwikiTitleItem> InterwikiLinks { get; } = [];

		public IReadOnlyList<LanguageLinksItem> LanguageLinks { get; } = [];

		public IReadOnlyList<IApiTitle> Links { get; } = [];

		public IReadOnlyList<LinksHereItem> LinksHere { get; } = [];

		public int Namespace { get; }

		public IReadOnlyDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

		public long PageId { get; }

		public IReadOnlyList<RedirectItem> Redirects { get; } = [];

		public IReadOnlyList<RevisionItem> Revisions { get; } = [];

		public IReadOnlyList<IApiTitle> Templates { get; } = [];

		public string Title { get; }

		public IReadOnlyList<TranscludedInItem> TranscludedIn { get; } = [];
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}