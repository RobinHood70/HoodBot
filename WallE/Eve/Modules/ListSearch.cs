#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.25
	internal sealed class ListSearch : ListModule<SearchInput, SearchResultItem>, IGeneratorModule
	{
		#region Fields
		private string? suggestion;
		private int totalHits;
		#endregion

		#region Constructors
		public ListSearch(WikiAbstractionLayer wal, SearchInput input)
			: base(wal, input, null)
		{
		}

		public ListSearch(WikiAbstractionLayer wal, SearchInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override string ContinueName => "offset";

		public override int MinimumVersion => 111;

		public override string Name => "search";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "sr";
		#endregion

		#region Public Static Methods
		public static ListSearch CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is SearchInput listInput
				? new ListSearch(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(SearchInput), input.GetType().Name);
		#endregion

		#region Public Methods
		public SearchResult AsSearchResult() => new SearchResult(
			list: this.Output ?? Array.Empty<SearchResultItem>(),
			suggestion: this.suggestion,
			totalHits: this.totalHits);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, SearchInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(117, SearchProperties.Score | SearchProperties.TitleSnippet | SearchProperties.RedirectTitle | SearchProperties.RedirectSnippet | SearchProperties.SectionTitle | SearchProperties.SectionSnippet | SearchProperties.HasRelated)
				.FilterFrom(124, SearchProperties.HasRelated | SearchProperties.Score)
				.Value;
			request
				.AddIfNotNull("search", input.Search)
				.Add("namespace", input.Namespaces)
				.AddIfPositiveIf("what", input.What, this.SiteVersion >= 117)
				.AddFlags("info", input.Info)
				.AddFlags("prop", prop)
				.AddIf("redirects", input.Redirects, this.SiteVersion < 123)
				.AddIfNotNull("backend", input.BackEnd)
				.Add("limit", this.Limit);
		}

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			if (parent["searchinfo"] is JToken infoNode)
			{
				this.suggestion = (string?)infoNode["suggestion"];
				this.totalHits = (int?)infoNode["totalhits"] ?? 0;
			}
		}

		protected override SearchResultItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			string? redir;
			var redirectTitle = result["redirecttitle"];
			if (redirectTitle != null && this.SiteVersion < 126 && redirectTitle.Type == JTokenType.Object)
			{
				// FIX: Fix for https://phabricator.wikimedia.org/T88397 - follows same logic as Title::getPrefixedText()
				var ns = (int?)redirectTitle["mNamespace"] ?? 0;
				redir = (string?)redirectTitle["mDbkeyform"];
				if (ns != 0)
				{
					var siteInfoNamespace = this.Wal.Namespaces[ns];
					redir = string.Concat(siteInfoNamespace.Name, ":", redir);
				}

				redir = redir?.Replace('_', ' ');
			}
			else
			{
				redir = (string?)redirectTitle;
			}

			return new SearchResultItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				redirSnippet: (string?)result["redirectsnippet"],
				redirTitle: redir,
				sectionSnippet: (string?)result["sectionsnippet"],
				sectionTitle: (string?)result["sectiontitle"],
				snippet: (string?)result["snippet"],
				size: (int?)result["size"] ?? 0,
				timestamp: (DateTime?)result["timestamp"],
				titleSnippet: (string?)result["title"],
				wordCount: (int?)result["wordcount"] ?? 0);
		}
		#endregion
	}
}
