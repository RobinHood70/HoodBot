namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.25
	internal sealed class ListSearch(WikiAbstractionLayer wal, SearchInput input, IPageSetGenerator? pageSetGenerator) : ListModule<SearchInput, SearchResultItem>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Constructors
		public ListSearch(WikiAbstractionLayer wal, SearchInput input)
			: this(wal, input, null)
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
		public static ListSearch CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (SearchInput)input, pageSetGenerator);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, SearchInput input)
		{
			var prop = FlagFilter
				.Check(this.SiteVersion, input.NotNull().Properties)
				.FilterBefore(117, SearchProperties.Score | SearchProperties.TitleSnippet | SearchProperties.RedirectTitle | SearchProperties.RedirectSnippet | SearchProperties.SectionTitle | SearchProperties.SectionSnippet | SearchProperties.HasRelated)
				.FilterFrom(124, SearchProperties.HasRelated | SearchProperties.Score)
				.Value;
			request
				.NotNull()
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
			ArgumentNullException.ThrowIfNull(parent);
			var infoNode = parent["searchinfo"];
			var suggestion = infoNode is null ? null : (string?)infoNode["suggestion"];
			var totalHits = infoNode is null ? 0 : (int?)infoNode["totalhits"] ?? 0;
			this.Output = new SearchResult(suggestion, totalHits);
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
				size: (int?)result["size"] ?? 0,
				snippet: (string?)result["snippet"],
				timestamp: (DateTime?)result["timestamp"],
				titleSnippet: (string?)result["title"],
				wordCount: (int?)result["wordcount"] ?? 0);
		}
		#endregion
	}
}