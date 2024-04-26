#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Properties;
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

	// Flags are part of the base info provided with the page info, so are included here in addition to the title info.
	public class PageItem(int ns, string title, long pageId, PageFlags flags) : IApiTitle
	{
		#region Fields
		private List<ContributorsItem> contributors = [];
		private List<CategoriesItem> categories = [];
		private List<RevisionItem> deletedRevisions = [];
		private List<DuplicateFilesItem> duplicateFiles = [];
		private List<string> externalLinks = [];
		private List<FileUsageItem> fileUsages = [];
		private List<ImageInfoItem> imageInfoEntries = [];
		private List<IApiTitle> images = [];
		private List<InterwikiTitleItem> interwikiLinks = [];
		private List<LanguageLinksItem> languageLinks = [];
		private List<IApiTitle> links = [];
		private List<LinksHereItem> linksHere = [];
		private List<PagePropertiesItem> properties = [];
		private List<RedirectsItem> redirects = [];
		private List<RevisionItem> revisions = [];
		private List<IApiTitle> templates = [];
		private List<TranscludedInItem> transcludedIn = [];
		#endregion

		#region Public Properties
		public long AnonContributors { get; private set; }

		public IReadOnlyList<CategoriesItem> Categories => this.categories;

		public CategoryInfoResult? CategoryInfo { get; private set; }

		public IReadOnlyList<ContributorsItem> Contributors => this.contributors;

		public IReadOnlyList<RevisionItem> DeletedRevisions => this.deletedRevisions;

		public IReadOnlyList<DuplicateFilesItem> DuplicateFiles => this.duplicateFiles;

		public IReadOnlyList<string> ExternalLinks => this.externalLinks;

		public IReadOnlyList<FileUsageItem> FileUsages => this.fileUsages;

		public PageFlags Flags { get; } = flags;

		public IReadOnlyList<ImageInfoItem> ImageInfoEntries => this.imageInfoEntries;

		public IReadOnlyList<IApiTitle> Images => this.images;

		public string? ImageRepository { get; private set; }

		public PageInfo? Info { get; private set; }

		public IReadOnlyList<InterwikiTitleItem> InterwikiLinks => this.interwikiLinks;

		public IReadOnlyList<LanguageLinksItem> LanguageLinks => this.languageLinks;

		public IReadOnlyList<IApiTitle> Links => this.links;

		public IReadOnlyList<LinksHereItem> LinksHere => this.linksHere;

		public int Namespace { get; } = ns;

		public IReadOnlyList<PagePropertiesItem> Properties => this.properties;

		public long PageId { get; } = pageId;

		public IReadOnlyList<RedirectsItem> Redirects => this.redirects;

		public IReadOnlyList<RevisionItem> Revisions => this.revisions;

		public IReadOnlyList<IApiTitle> Templates => this.templates;

		public string Title { get; } = title;

		public IReadOnlyList<TranscludedInItem> TranscludedIn => this.transcludedIn;
		#endregion

		#region Public Methods
		public void ParseModuleOutput(object output)
		{
			// TODO: This is almost certainly not the best way to handle this, but it's better for separation of concerns than the previous method and allows read-only properties. Should re-examine later to see if there's some better method.
			switch (output)
			{
				case CategoriesResult result:
					this.categories = result;
					break;
				case CategoryInfoResult result:
					this.CategoryInfo = result;
					break;
				case ContributorsResult result:
					this.AnonContributors = result.AnonymousContributors;
					this.contributors = [.. result];
					break;
				case DuplicateFilesResult result:
					this.duplicateFiles = result;
					break;
				case ExternalLinksResult result:
					this.externalLinks = result;
					break;
				case FileUsageResult result:
					this.fileUsages = result;
					break;
				case ImageInfoResult result:
					this.ImageRepository = result.Repository;
					this.imageInfoEntries = result;
					break;
				case ImagesResult result:
					this.images = result;
					break;
				case InterwikiLinksResult result:
					this.interwikiLinks = result;
					break;
				case LanguageLinksResult result:
					this.languageLinks = result;
					break;
				case LinksHereResult result:
					this.linksHere = result;
					break;
				case LinksResult result:
					this.links = result;
					break;
				case PageInfo result:
					this.Info = result;
					break;
				case PagePropertiesResult result:
					this.properties = result;
					break;
				case PropDeletedRevisionsResult result:
					this.deletedRevisions = result;
					break;
				case RedirectsResult result:
					this.redirects = result;
					break;
				case RevisionsResult result:
					this.revisions = result;
					break;
				case TemplatesResult result:
					this.templates = result;
					break;
				case TranscludedInResult result:
					this.transcludedIn = result;
					break;
				default:
					this.ParseCustomResult(output);
					break;
			}
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion

		#region Public Virtual Methods
		protected virtual void ParseCustomResult(object output)
		{
			if (output is not null)
			{
				throw new NotSupportedException(Globals.CurrentCulture(EveMessages.OutputTypeNotHandled, output.GetType().Name));
			}
		}
		#endregion
	}
}