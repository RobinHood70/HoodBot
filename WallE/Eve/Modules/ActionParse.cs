#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	// MWVERSION: 1.28
	public class ActionParse : ActionModule<ParseInput, ParseResult>
	{
		#region Constructors
		public ActionParse(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "parse";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
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

		protected override ParseResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new ParseResult()
			{
				Title = (string)result["title"],
				PageId = (long?)result["pageid"] ?? 0,
				RevisionId = (long?)result["revid"] ?? 0,
			};
			var redirects = new Dictionary<string, PageSetRedirectItem>();
			result["redirects"].GetRedirects(redirects);
			output.Redirects = redirects.AsReadOnly();
			output.Text = (string)result["text"];
			output.ParsedSummary = (string)result["parsedsummary"].AsBCSubContent();

			var langLinks = new List<LanguageLinksItem>();
			var subResult = result["langlinks"];
			if (subResult != null)
			{
				foreach (var link in subResult)
				{
					langLinks.Add(link.GetLanguageLink());
				}
			}

			output.LanguageLinks = langLinks.AsReadOnly();

			var categories = new List<ParseCategoriesItem>();
			subResult = result["categories"];
			if (subResult != null)
			{
				foreach (var catResult in subResult)
				{
					var category = new ParseCategoriesItem()
					{
						Category = (string)catResult.AsBCContent("category"),
						SortKey = (string)catResult["sortkey"],
						Flags =
							catResult.GetFlag("hidden", ParseCategoryFlags.Hidden) |
							catResult.GetFlag("known", ParseCategoryFlags.Known) |
							catResult.GetFlag("missing", ParseCategoryFlags.Missing),
					};
					categories.Add(category);
				}
			}

			output.Categories = categories.AsReadOnly();
			output.CategoriesHtml = (string)result["categorieshtml"];
			output.Links = GetLinks(result["links"]).AsReadOnly();
			output.Templates = GetLinks(result["templates"]).AsReadOnly();
			output.Images = result["images"].AsReadOnlyList<string>();
			output.ExternalLinks = result["externallinks"].AsReadOnlyList<string>();

			var sections = new List<SectionsItem>();
			subResult = result["sections"];
			if (subResult != null)
			{
				foreach (var secResult in subResult)
				{
					var section = new SectionsItem()
					{
						TocLevel = (int)secResult["toclevel"],
						Level = (int)secResult["level"],
						Line = (string)secResult["line"],
						Number = (string)secResult["number"],
						Index = (string)secResult["index"],
						FromTitle = (string)secResult["fromtitle"],
						ByteOffset = (int)secResult["byteoffset"],
						Anchor = (string)secResult["anchor"],
					};
					sections.Add(section);
				}
			}

			output.Sections = sections.AsReadOnly();
			output.DisplayTitle = (string)result["displaytitle"];
			output.HeadHtml = (string)result["headhtml"].AsBCSubContent();
			output.Modules = result["modules"].AsReadOnlyList<string>();
			output.ModuleScripts = result["modulescripts"].AsReadOnlyList<string>();
			output.ModuleStyles = result["modulestyles"].AsReadOnlyList<string>();
			output.JavaScriptConfigurationVariables = result["jsconfigvars"].AsReadOnlyDictionary<string, string>();
			output.Indicators = result["indicators"].AsBCDictionary();
			output.InterwikiLinks = result["iwlinks"].GetInterwikiLinks().AsReadOnly();
			output.WikiText = (string)result["wikitext"].AsBCSubContent();
			output.PreSaveTransformText = (string)result["psttext"].AsBCSubContent();
			output.Properties = result["properties"].AsBCDictionary();

			var limitData = new Dictionary<string, IReadOnlyList<decimal>>();
			subResult = result["limitreportdata"];
			if (subResult != null)
			{
				foreach (var entry in subResult)
				{
					string name = null;
					var limits = new List<decimal>();
#pragma warning disable IDE0007 // Use implicit type
					foreach (JProperty limitResult in entry)
#pragma warning restore IDE0007 // Use implicit type
					{
						if (limitResult.Name == "name")
						{
							name = (string)limitResult.Value;
						}
						else
						{
							limits.Add((decimal)limitResult.Value);
						}
					}

					limitData.Add(name, limits.AsReadOnly());
				}
			}

			output.LimitReportData = limitData.AsReadOnly();
			output.LimitReportHtml = (string)result["limitreporthtml"];
			output.ParseTree = (string)result["parsetree"];

			return output;
		}

		protected override void AddWarning(string from, string text)
		{
			ThrowNull(text, nameof(text));

			// 1.26 and 1.27 always emit a warning when the Modules property is specified, even though only one section of it is deprecated, so swallow that.
			if (!text.StartsWith("modulemessages", StringComparison.Ordinal))
			{
				base.AddWarning(from, text);
			}
		}
		#endregion

		#region Private Static Methods
		private static List<ParseLinksItem> GetLinks(JToken subResult)
		{
			var links = new List<ParseLinksItem>();
			if (subResult != null)
			{
				foreach (var linkResult in subResult)
				{
					var link = new ParseLinksItem()
					{
						Namespace = (int?)linkResult["ns"],
						Title = (string)linkResult.AsBCContent("title"),
						Exists = linkResult["exists"].AsBCBool(),
					};
					links.Add(link);
				}
			}

			return links;
		}
		#endregion
	}
}