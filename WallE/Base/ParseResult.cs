#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	// IMPNOTE: HeadItems is not currently supported due to being deprecated, being largely redundant to HeadHtml, and having an odd, complex format. If someone really needs it for some strange reason, let me know and I'll implement it.
	// IMPNOTE: EncodedJavaScriptConfigurationVariables is not implemented as it seems fairly useless when you've already got JavaScriptConfigurationVariables.
	// IMPNOTE: ModuleMessages is not implemented, since it only existed for two versions before being deprecated, then removed two versions after that.
	public class ParseResult
	{
		#region Public Properties
		public IReadOnlyList<ParseCategoriesItem> Categories { get; set; }

		public string CategoriesHtml { get; set; }

		public string DisplayTitle { get; set; }

		public IReadOnlyList<string> ExternalLinks { get; set; }

		public string HeadHtml { get; set; }

		public IReadOnlyList<string> Images { get; set; }

		public IReadOnlyList<InterwikiTitleItem> InterwikiLinks { get; set; }

		public IReadOnlyDictionary<string, string> JavaScriptConfigurationVariables { get; set; }

		public IReadOnlyDictionary<string, string> Indicators { get; set; }

		public IReadOnlyList<ParseLinksItem> Links { get; set; }

		public IReadOnlyList<LanguageLinksItem> LanguageLinks { get; set; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Reasonably straight-forward as is, if not entirely desirable.")]
		public IReadOnlyDictionary<string, IReadOnlyList<decimal>> LimitReportData { get; set; }

		public string LimitReportHtml { get; set; }

		public IReadOnlyList<string> Modules { get; set; }

		public IReadOnlyList<string> ModuleScripts { get; set; }

		public IReadOnlyList<string> ModuleStyles { get; set; }

		public long PageId { get; set; }

		public string ParsedSummary { get; set; }

		public string ParseTree { get; set; }

		public string PreSaveTransformText { get; set; }

		public IReadOnlyDictionary<string, string> Properties { get; set; }

		public IReadOnlyDictionary<string, PageSetRedirectItem> Redirects { get; set; }

		public long RevisionId { get; set; }

		public IReadOnlyList<SectionsItem> Sections { get; set; }

		public IReadOnlyList<ParseLinksItem> Templates { get; set; }

		public string Text { get; set; }

		public string Title { get; set; }

		public string WikiText { get; set; }
		#endregion
	}
}
