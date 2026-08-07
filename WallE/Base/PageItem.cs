#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections;
using System.Collections.Generic;
using RobinHood70.WikiCommon;

#region Public Delegates
public delegate void PageItemResultMethod(PageItem page, object result);
#endregion

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
	#region Static Fields
	private static readonly Dictionary<Type, PageItemResultMethod> ResultHandlers = [];
	#endregion

	#region Fields
	private readonly List<CategoriesItem> categories = [];
	private readonly List<ContributorsItem> contributors = [];
	private readonly List<RevisionItem> deletedRevisions = [];
	private readonly List<DuplicateFilesItem> duplicateFiles = [];
	private readonly List<string> externalLinks = [];
	private readonly List<FileUsageItem> fileUsages = [];
	private readonly List<IApiTitle> images = [];
	private readonly List<InterwikiTitleItem> interwikiLinks = [];
	private readonly List<LanguageLinksItem> languageLinks = [];
	private readonly List<IApiTitle> links = [];
	private readonly List<LinksHereItem> linksHere = [];
	private readonly List<PagePropertiesItem> properties = [];
	private readonly List<RedirectsItem> redirects = [];
	private readonly List<RevisionItem> revisions = [];
	private readonly List<IApiTitle> templates = [];
	private readonly List<TranscludedInItem> transcludedIn = [];
	private Dictionary<string, object>? custom;
	#endregion

	#region Static Constructors
	static PageItem()
	{
		RegisterDefaultResultHandlers();
	}
	#endregion

	#region Public Properties
	public long AnonContributors { get; private set; }

	public IReadOnlyList<CategoriesItem> Categories => this.categories;

	public CategoryInfoResult? CategoryInfo { get; private set; }

	public IReadOnlyList<ContributorsItem> Contributors => this.contributors;

	public IReadOnlyDictionary<string, object>? Custom => this.custom;

	public IReadOnlyList<RevisionItem> DeletedRevisions => this.deletedRevisions;

	public IReadOnlyList<DuplicateFilesItem> DuplicateFiles => this.duplicateFiles;

	public IReadOnlyList<string> ExternalLinks => this.externalLinks;

	public IReadOnlyList<FileUsageItem> FileUsages => this.fileUsages;

	public PageFlags Flags { get; } = flags;

	public IReadOnlyList<IApiTitle> Images => this.images;

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

	#region Public Static Methods
	public static void RegisterResultHandler<T>(PageItemResultMethod handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		ResultHandlers[typeof(T)] = handler;
	}
	#endregion

	#region Public Methods
	public void ParseModuleOutput(string name, object output)
	{
		ArgumentNullException.ThrowIfNull(output);
		if (ResultHandlers.TryGetValue(output.GetType(), out var resultHandler))
		{
			resultHandler(this, output);
		}
		else
		{
			// This isn't pretty, but we have to have a list of objects since there could be multiple partial/duplicate responses across different requests. Alternatives would be to implement a static custom handlers list here as well, like the Robby.Page object, or allow prop result classes to implement some kind of Merge<T>(T other) function.
			this.custom ??= new Dictionary<string, object>(1, StringComparer.Ordinal);
			if (!this.custom.TryGetValue(name, out var list))
			{
				var genericType = output.GetType();
				var listType = typeof(List<>).MakeGenericType(genericType);
				if (Activator.CreateInstance(listType) is IList runtimeList)
				{
					runtimeList.Add(output);
					list = runtimeList;
					this.custom.Add(name, list);
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
			else if (list is IList runtimeList)
			{
				runtimeList.Add(output);
			}
		}
	}
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion

	#region Private Static Methods
	private static void CategoriesResultHandler(PageItem page, object result) =>
		page.categories.AddRange((CategoriesResult)result);

	private static void CategoryInfoResultHandler(PageItem page, object result) =>
		page.CategoryInfo ??= (CategoryInfoResult)result;

	private static void ContributorsResultHandler(PageItem page, object result)
	{
		var realResult = (ContributorsResult)result;
		page.AnonContributors = realResult.AnonymousContributors;
		page.contributors.AddRange(realResult);
	}

	private static void DuplicateFilesResultHandler(PageItem page, object result) =>
		page.duplicateFiles.AddRange((DuplicateFilesResult)result);

	private static void ExternalLinksResultHandler(PageItem page, object result) =>
		page.externalLinks.AddRange((ExternalLinksResult)result);

	private static void FileUsageResultHandler(PageItem page, object result) =>
		page.fileUsages.AddRange((FileUsageResult)result);

	private static void ImagesResultHandler(PageItem page, object result) =>
		page.images.AddRange((ImagesResult)result);

	private static void InterwikiLinksResultHandler(PageItem page, object result) =>
		page.interwikiLinks.AddRange((InterwikiLinksResult)result);

	private static void LanguageLinksResultHandler(PageItem page, object result) =>
		page.languageLinks.AddRange((LanguageLinksResult)result);

	private static void LinksHereResultHandler(PageItem page, object result) =>
		page.linksHere.AddRange((LinksHereResult)result);

	private static void LinksResultHandler(PageItem page, object result) =>
		page.links.AddRange((LinksResult)result);

	private static void PagePropertiesResultHandler(PageItem page, object result) =>
		page.properties.AddRange((PagePropertiesResult)result);

	private static void PropDeletedRevisionsResultHandler(PageItem page, object result) =>
		page.deletedRevisions.AddRange((PropDeletedRevisionsResult)result);

	private static void RedirectsResultHandler(PageItem page, object result) =>
		page.redirects.AddRange((RedirectsResult)result);

	private static void PageInfoHandler(PageItem page, object result) =>
		page.Info ??= (PageInfo)result;

	private static void RegisterDefaultResultHandlers()
	{
		RegisterResultHandler<CategoriesResult>(CategoriesResultHandler);
		RegisterResultHandler<CategoryInfoResult>(CategoryInfoResultHandler);
		RegisterResultHandler<ContributorsResult>(ContributorsResultHandler);
		RegisterResultHandler<DuplicateFilesResult>(DuplicateFilesResultHandler);
		RegisterResultHandler<ExternalLinksResult>(ExternalLinksResultHandler);
		RegisterResultHandler<FileUsageResult>(FileUsageResultHandler);
		RegisterResultHandler<ImagesResult>(ImagesResultHandler);
		RegisterResultHandler<InterwikiLinksResult>(InterwikiLinksResultHandler);
		RegisterResultHandler<LanguageLinksResult>(LanguageLinksResultHandler);
		RegisterResultHandler<LinksHereResult>(LinksHereResultHandler);
		RegisterResultHandler<LinksResult>(LinksResultHandler);
		RegisterResultHandler<PagePropertiesResult>(PagePropertiesResultHandler);
		RegisterResultHandler<PropDeletedRevisionsResult>(PropDeletedRevisionsResultHandler);
		RegisterResultHandler<RedirectsResult>(RedirectsResultHandler);
		RegisterResultHandler<PageInfo>(PageInfoHandler);
		RegisterResultHandler<RevisionsResult>(RevisionsResultHandler);
		RegisterResultHandler<TemplatesResult>(TemplatesResultHandler);
		RegisterResultHandler<TranscludedInResult>(TranscludedInResultHandler);
	}

	private static void RevisionsResultHandler(PageItem page, object result) =>
		page.revisions.AddRange((RevisionsResult)result);

	private static void TemplatesResultHandler(PageItem page, object result) =>
		page.templates.AddRange((TemplatesResult)result);

	private static void TranscludedInResultHandler(PageItem page, object result) =>
		page.transcludedIn.AddRange((TranscludedInResult)result);
	#endregion
}