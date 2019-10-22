#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	// IMPNOTE: HeadItems is not currently supported due to being deprecated, being largely redundant to HeadHtml, and having an odd, complex format. If someone really needs it for some strange reason, let me know and I'll implement it.
	// IMPNOTE: EncodedJavaScriptConfigurationVariables is not implemented as it seems fairly useless when you've already got JavaScriptConfigurationVariables.
	// IMPNOTE: ModuleMessages is not implemented, since it only existed for two versions before being deprecated, then removed two versions after that.
	public class ParseResult
	{
		#region Constructors
		internal ParseResult(IReadOnlyList<ParseCategoriesItem> categories, string? categoriesHtml, string? displayTitle, IReadOnlyList<string> externalLinks, string? headHtml, IReadOnlyList<string> images, IReadOnlyDictionary<string, string?> indicators, IReadOnlyList<InterwikiTitleItem> interwikiLinks, Dictionary<string, string> javaScriptConfigurationVariables, IReadOnlyList<LanguageLinksItem> languageLinks, IReadOnlyDictionary<string, IReadOnlyList<string>> limitReportData, string? limitReportHtml, IReadOnlyList<ParseLinksItem> links, IReadOnlyList<string> moduleScripts, IReadOnlyList<string> moduleStyles, IReadOnlyList<string> modules, long pageId, string? parseTree, string? parsedSummary, string? preSaveTransformText, IReadOnlyDictionary<string, string?> properties, Dictionary<string, PageSetRedirectItem> redirects, long revisionId, IReadOnlyList<SectionsItem> sections, IReadOnlyList<ParseLinksItem> templates, string? text, string? title, string? wikiText)
		{
			this.Categories = categories;
			this.CategoriesHtml = categoriesHtml;
			this.DisplayTitle = displayTitle;
			this.ExternalLinks = externalLinks;
			this.HeadHtml = headHtml;
			this.Images = images;
			this.Indicators = indicators;
			this.InterwikiLinks = interwikiLinks;
			this.JavaScriptConfigurationVariables = javaScriptConfigurationVariables;
			this.LanguageLinks = languageLinks;
			this.LimitReportData = limitReportData;
			this.LimitReportHtml = limitReportHtml;
			this.Links = links;
			this.ModuleScripts = moduleScripts;
			this.ModuleStyles = moduleStyles;
			this.Modules = modules;
			this.PageId = pageId;
			this.ParseTree = parseTree;
			this.ParsedSummary = parsedSummary;
			this.PreSaveTransformText = preSaveTransformText;
			this.Properties = properties;
			this.Redirects = redirects;
			this.RevisionId = revisionId;
			this.Sections = sections;
			this.Templates = templates;
			this.Text = text;
			this.Title = title;
			this.WikiText = wikiText;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<ParseCategoriesItem> Categories { get; }

		public string? CategoriesHtml { get; }

		public string? DisplayTitle { get; }

		public IReadOnlyList<string> ExternalLinks { get; }

		public string? HeadHtml { get; }

		public IReadOnlyList<string> Images { get; }

		public IReadOnlyList<InterwikiTitleItem> InterwikiLinks { get; }

		public IReadOnlyDictionary<string, string> JavaScriptConfigurationVariables { get; }

		public IReadOnlyDictionary<string, string?> Indicators { get; }

		public IReadOnlyList<ParseLinksItem> Links { get; }

		public IReadOnlyList<LanguageLinksItem> LanguageLinks { get; }

		public IReadOnlyDictionary<string, IReadOnlyList<string>> LimitReportData { get; }

		public string? LimitReportHtml { get; }

		public IReadOnlyList<string> Modules { get; }

		public IReadOnlyList<string> ModuleScripts { get; }

		public IReadOnlyList<string> ModuleStyles { get; }

		public long PageId { get; }

		public string? ParsedSummary { get; }

		public string? ParseTree { get; }

		public string? PreSaveTransformText { get; }

		public IReadOnlyDictionary<string, string?> Properties { get; }

		public IReadOnlyDictionary<string, PageSetRedirectItem> Redirects { get; }

		public long RevisionId { get; }

		public IReadOnlyList<SectionsItem> Sections { get; }

		public IReadOnlyList<ParseLinksItem> Templates { get; }

		public string? Text { get; }

		public string? Title { get; }

		public string? WikiText { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
