#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class SearchResultItem(int ns, string title, string? redirSnippet, string? redirTitle, string? sectionSnippet, string? sectionTitle, int size, string? snippet, DateTime? timestamp, string? titleSnippet, int wordCount) : IApiTitle
	{
		#region Public Properties
		public int Namespace { get; } = ns;

		public string? RedirectSnippet { get; } = redirSnippet;

		public string? RedirectTitle { get; } = redirTitle;

		// Excluded because no common search engines ever used it and it's now deprecated.
		//// public float Score { get; }

		public string? SectionSnippet { get; } = sectionSnippet;

		public string? SectionTitle { get; } = sectionTitle;

		public int Size { get; } = size;

		public string? Snippet { get; } = snippet;

		public DateTime? Timestamp { get; } = timestamp;

		public string Title { get; } = title;

		public string? TitleSnippet { get; } = titleSnippet;

		public int WordCount { get; } = wordCount;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}