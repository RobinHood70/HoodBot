﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.28
	internal class ActionParse : ActionModule<ParseInput, ParseResult>
	{
		#region Constructors
		public ActionParse(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "parse";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ParseInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(126, ParseProperties.JsConfigVars | ParseProperties.ParseTree)
				.FilterBefore(125, ParseProperties.Indicators)
				.FilterBefore(124, ParseProperties.Modules)
				.FilterBefore(123, ParseProperties.LimitReportData | ParseProperties.LimitReportHtml)
				.FilterBefore(120, ParseProperties.Properties)
				.FilterBefore(117, ParseProperties.CategoriesHtml | ParseProperties.LanguagesHtml | ParseProperties.IWLinks | ParseProperties.WikiText)
				.FilterFrom(124, ParseProperties.LanguagesHtml)
				.Value;
			request
				.AddIfNotNull("title", input.Title)
				.AddIfNotNull("text", input.Text)
				.AddIfNotNull("summary", input.Summary)
				.AddIfNotNull("page", input.Page)
				.AddIf("pageid", input.PageId, input.PageId > 0 && this.SiteVersion >= 117)
				.Add("redirects", input.Redirects)
				.AddIfPositive("oldid", input.OldId)
				.AddFlags("prop", prop)
				.Add("pst", input.PreSaveTransform == PreSaveTransformOption.Yes)
				.Add("onlypst", input.PreSaveTransform == PreSaveTransformOption.Only)
				.AddIf("effectivelanglinks", input.EffectiveLangLinks, this.SiteVersion >= 122)
				.AddIfNotNullIf("section", input.Section, this.SiteVersion >= 117)
				.AddIfNotNullIf("sectiontitle", input.SectionTitle, this.SiteVersion >= 125)
				.AddIf("disablepp", input.DisableLimitReport, this.SiteVersion < 126)
				.AddIf("disablelimitreport", input.DisableLimitReport, this.SiteVersion >= 126)
				.AddIf("disableeditsection", input.DisableEditSection, this.SiteVersion >= 124)
				.AddIf("disabletidy", input.DisableTidy, this.SiteVersion >= 126)
				.AddIf("generatexml", input.Properties.HasFlag(ParseProperties.ParseTree), this.SiteVersion >= 120 && this.SiteVersion < 126)
				.AddIf("preview", input.Preview, this.SiteVersion >= 122)
				.AddIf("sectionpreview", input.SectionPreview, this.SiteVersion >= 122)
				.AddIf("disabletoc", input.DisableTableOfContents, this.SiteVersion >= 123)
				.AddIfNotNullIf("contentformat", input.ContentFormat, this.SiteVersion >= 121)
				.AddIfNotNullIf("contentmodel", input.ContentModel, this.SiteVersion >= 121);
		}

		protected override ParseResult DeserializeResult(JToken? result)
		{
			ThrowNull(result, nameof(result));
			return new ParseResult(
				categories: DeserializeCategories(result["categories"]),
				categoriesHtml: (string?)result["categorieshtml"],
				displayTitle: (string?)result["displaytitle"],
				externalLinks: result["externallinks"].GetList<string>(),
				headHtml: (string?)result["headhtml"].GetBCSubElements(),
				images: result["images"].GetList<string>(),
				indicators: result["indicators"].GetBCDictionary(),
				interwikiLinks: result["iwlinks"].GetInterwikiLinks(),
				javaScriptConfigurationVariables: result["jsconfigvars"].GetStringDictionary<string>(),
				languageLinks: DeserializeLanguageLinks(result["langlinks"]),
				limitReportData: DeserializeLimitReportData(result["limitreportdata"]),
				limitReportHtml: (string?)result["limitreporthtml"],
				links: DeserializeLinks(result["links"]),
				moduleScripts: result["modulescripts"].GetList<string>(),
				moduleStyles: result["modulestyles"].GetList<string>(),
				modules: result["modules"].GetList<string>(),
				pageId: (long?)result["pageid"] ?? 0,
				parseTree: (string?)result["parsetree"],
				parsedSummary: (string?)result["parsedsummary"].GetBCSubElements(),
				preSaveTransformText: (string?)result["psttext"].GetBCSubElements(),
				properties: result["properties"].GetBCDictionary(),
				redirects: result["redirects"].GetRedirects(this.Wal.InterwikiPrefixes, this.SiteVersion),
				revisionId: (long?)result["revid"] ?? 0,
				sections: DeserializeSections(result["sections"]),
				templates: DeserializeLinks(result["templates"]),
				text: (string?)result["text"],
				title: (string?)result["title"],
				wikiText: (string?)result["wikitext"].GetBCSubElements());
		}

		// 1.26 and 1.27 always emit a warning when the Modules property is specified, even though only one section of it is deprecated, so swallow that.
		protected override bool HandleWarning(string? from, string? text)
		{
			ThrowNull(from, nameof(from));
			ThrowNull(text, nameof(text));
			return text.StartsWith("modulemessages", StringComparison.Ordinal) || base.HandleWarning(from, text);
		}
		#endregion

		#region Private Static Methods

		private static List<ParseCategoriesItem> DeserializeCategories(JToken? subResult)
		{
			var categories = new List<ParseCategoriesItem>();
			if (subResult != null)
			{
				foreach (var catResult in subResult)
				{
					categories.Add(new ParseCategoriesItem(
						category: catResult.MustHaveBCString("category"),
						sortKey: catResult.MustHaveString("sortkey"),
						flags: catResult.GetFlags(
							("hidden", ParseCategoryFlags.Hidden),
							("known", ParseCategoryFlags.Known),
							("missing", ParseCategoryFlags.Missing))));
				}
			}

			return categories;
		}

		private static IReadOnlyList<LanguageLinksItem> DeserializeLanguageLinks(JToken? subResult)
		{
			var langLinks = new List<LanguageLinksItem>();
			if (subResult != null)
			{
				foreach (var link in subResult)
				{
					if (link.GetLanguageLink() is LanguageLinksItem item)
					{
						langLinks.Add(item);
					}
				}
			}

			return langLinks.AsReadOnly();
		}

		private static Dictionary<string, IReadOnlyList<string>> DeserializeLimitReportData(JToken? subResult)
		{
			var limitData = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			if (subResult != null)
			{
				foreach (var entry in subResult)
				{
					var name = entry.MustHaveString("name");
					var limits = new List<string>();
					foreach (var limitResult in entry.Children<JProperty>())
					{
						if (limitResult.Name != "name" && (string?)limitResult.Value is string value)
						{
							limits.Add(value);
						}
					}

					limitData.Add(name, limits);
				}
			}

			return limitData;
		}

		private static List<ParseLinksItem> DeserializeLinks(JToken? linkResults)
		{
			var links = new List<ParseLinksItem>();
			if (linkResults != null)
			{
				foreach (var result in linkResults)
				{
					links.Add(new ParseLinksItem((int)result.MustHave("ns"), result.MustHaveString("title"), result["exists"].GetBCBool()));
				}
			}

			return links;
		}

		private static IReadOnlyList<SectionsItem> DeserializeSections(JToken? subResult)
		{
			var sections = new List<SectionsItem>();
			if (subResult != null)
			{
				foreach (var secResult in subResult)
				{
					sections.Add(new SectionsItem(
						tocLevel: (int)secResult.MustHave("toclevel"),
						level: (int)secResult.MustHave("level"),
						anchor: secResult.MustHaveString("anchor"),
						line: secResult.MustHaveString("line"),
						number: secResult.MustHaveString("number"),
						index: secResult.MustHaveString("index"),
						byteOffset: (int?)secResult["byteoffset"],
						fromTitle: (string?)secResult["fromtitle"]));
				}
			}

			return sections;
		}
		#endregion
	}
}