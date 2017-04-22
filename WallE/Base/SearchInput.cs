#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using static Globals;

	#region Public Enumerations
	public enum SearchWhat
	{
		Title,
		Text,
		NearMatch
	}

	[SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification = "Collective term")]
	[Flags]
	public enum SearchInfo
	{
		None = 0,
		Suggestion = 1,
		TotalHits = 1 << 1,
		All = Suggestion | TotalHits
	}

	[Flags]
	public enum SearchProperties
	{
		None = 0,
		Size = 1,
		WordCount = 1 << 1,
		Timestamp = 1 << 2,
		Score = 1 << 3,
		Snippet = 1 << 4,
		TitleSnippet = 1 << 5,
		RedirectTitle = 1 << 6,
		RedirectSnippet = 1 << 7,
		SectionTitle = 1 << 8,
		SectionSnippet = 1 << 9,
		HasRelated = 1 << 10,
		All = Size | WordCount | Timestamp | Score | Snippet | TitleSnippet | RedirectTitle | RedirectSnippet | SectionTitle | SectionSnippet | HasRelated
	}
	#endregion

	public class SearchInput : ILimitableInput, IGeneratorInput
	{
		#region Constructors
		public SearchInput(string search)
		{
			ThrowNullOrWhiteSpace(search, nameof(search));
			this.Search = search;
		}
		#endregion

		#region Public Properties
		public string BackEnd { get; set; }

		public SearchInfo Info { get; set; }

		// Currently not supported because I couldn't find any search engines that supported interwiki searches to use for testing.
		//// public bool Interwiki { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public SearchProperties Properties { get; set; }

		public bool Redirects { get; set; }

		public string Search { get; }

		public SearchWhat What { get; set; }
		#endregion
	}
}
