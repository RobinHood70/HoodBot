#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.CommonCode;

	#region Public Enumerations
	[Flags]
	public enum ParseProperties
	{
		None = 0,
		Text = 1,
		LangLinks = 1 << 1,
		Categories = 1 << 2,
		CategoriesHtml = 1 << 3,
		LanguagesHtml = 1 << 4,
		Links = 1 << 5,
		Templates = 1 << 6,
		Images = 1 << 7,
		ExternalLinks = 1 << 8,
		Sections = 1 << 9,
		RevId = 1 << 10,
		DisplayTitle = 1 << 11,
		//// HeadItems = 1 << 12,
		HeadHtml = 1 << 13,
		Modules = 1 << 14,
		JsConfigVars = 1 << 15,
		//// EncodedJsConfigVars = 1 << 16,
		Indicators = 1 << 17,
		IWLinks = 1 << 18,
		WikiText = 1 << 19,
		Properties = 1 << 20,
		LimitReportData = 1 << 21,
		LimitReportHtml = 1 << 22,
		ParseTree = 1 << 23,
		All = Text | LangLinks | Categories | CategoriesHtml | LanguagesHtml | Links | Templates | Images | ExternalLinks | Sections | RevId | DisplayTitle | HeadHtml | Modules | JsConfigVars | Indicators | IWLinks | WikiText | Properties | LimitReportData | LimitReportHtml | ParseTree
	}

	public enum PreSaveTransformOption
	{
		No,
		Yes,
		Only
	}
	#endregion

	public sealed class ParseInput
	{
		#region Constructors
		private ParseInput()
		{
		}
		#endregion

		#region Public Properties
		public string? ContentFormat { get; set; }

		public string? ContentModel { get; set; }

		public bool DisableEditSection { get; set; }

		public bool DisableLimitReport { get; set; }

		public bool DisableTableOfContents { get; set; }

		public bool DisableTidy { get; set; }

		public bool EffectiveLangLinks { get; set; }

		public long OldId { get; private set; }

		public string? Page { get; private set; }

		public long PageId { get; private set; }

		public PreSaveTransformOption PreSaveTransform { get; set; }

		public bool Preview { get; set; }

		public ParseProperties Properties { get; set; }

		public bool Redirects { get; set; }

		public string? Section { get; set; }

		public bool SectionPreview { get; set; }

		public string? SectionTitle { get; set; }

		public string? Summary { get; set; }

		public string? Text { get; private set; }

		public string? Title { get; private set; }
		#endregion

		#region Public Static Methods
		public static ParseInput FromOldId(long oldId) => new() { OldId = oldId };

		public static ParseInput FromPage(string page) => new() { Page = page.NotNull(nameof(page)) };

		public static ParseInput FromPageId(long pageId) => new() { PageId = pageId };

		public static ParseInput FromText(string text) => FromText(text, null);

		// Odd that someone would pass whitespace here, but not inconceivable, so only check text for null. Title can be null, so no check.
		public static ParseInput FromText(string text, string? title) =>
			new() { Text = text.NotNull(nameof(text)), Title = title };
		#endregion
	}
}