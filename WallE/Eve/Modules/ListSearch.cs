#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListSearch : ListModule<SearchInput, SearchResultItem>, IGeneratorModule
	{
		#region Fields
		private string suggestion;
		private int totalHits;
		#endregion

		#region Constructors
		public ListSearch(WikiAbstractionLayer wal, SearchInput input)

			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override string ContinueName { get; } = "offset";

		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "search";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "sr";
		#endregion

		#region Public Static Methods
		public static ListSearch CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListSearch(wal, input as SearchInput);
		#endregion

		#region Public Methods
		public SearchResult AsSearchTitleCollection() =>
			new SearchResult(this.Output)
			{
				Suggestion = this.suggestion,
				TotalHits = this.totalHits,
			};
		#endregion

		#region Public Override Methods
		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Not a normalization")]
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

		protected override void DeserializeParent(JToken parent, IList<SearchResultItem> output)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(output, nameof(output));
			var infoNode = parent["searchinfo"];
			this.suggestion = (string)infoNode["suggestion"];
			this.totalHits = (int?)infoNode["totalhits"] ?? 0;
		}

		protected override SearchResultItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new SearchResultItem()
			{
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
				Snippet = (string)result["snippet"],
				Size = (int?)result["size"] ?? 0,
				WordCount = (int?)result["wordcount"] ?? 0,
				Timestamp = (DateTime?)result["timestamp"],
				TitleSnippet = (string)result["title"],
			};
			var redirectTitle = result["redirecttitle"];
			if (redirectTitle != null)
			{
				if (redirectTitle.Type == JTokenType.Object)
				{
					// FIX: Fix for https://phabricator.wikimedia.org/T88397 - follows same logic as Title::getPrefixedText()
					var ns = (int?)redirectTitle["mNamespace"] ?? 0;
					item.RedirectTitle = (string)redirectTitle["mDbkeyform"];
					if (ns != 0)
					{
						var siteInfoNamespace = this.Wal.Namespaces[ns];
						item.RedirectTitle = string.Concat(siteInfoNamespace.Name, ":", item.RedirectTitle);
					}

					item.RedirectTitle = item.RedirectTitle.Replace('_', ' ');
				}
				else
				{
					item.RedirectTitle = (string)redirectTitle;
				}
			}

			item.RedirectSnippet = (string)result["redirectsnippet"];
			item.SectionTitle = (string)result["sectiontitle"];
			item.SectionSnippet = (string)result["sectionsnippet"];

			return item;
		}
		#endregion
	}
}
