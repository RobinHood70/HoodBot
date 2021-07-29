#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class SearchResultItem : IApiTitle
	{
		#region Constructors
		public SearchResultItem(int ns, string title, string? redirSnippet, string? redirTitle, string? sectionSnippet, string? sectionTitle, int size, string? snippet, DateTime? timestamp, string? titleSnippet, int wordCount)
		{
			this.Namespace = ns;
			this.FullPageName = title;
			this.RedirectSnippet = redirSnippet;
			this.RedirectTitle = redirTitle;
			this.SectionSnippet = sectionSnippet;
			this.SectionTitle = sectionTitle;
			this.Size = size;
			this.Snippet = snippet;
			this.Timestamp = timestamp;
			this.TitleSnippet = titleSnippet;
			this.WordCount = wordCount;
		}
		#endregion

		#region Public Properties
		public int Namespace { get; }

		public string? RedirectSnippet { get; }

		public string? RedirectTitle { get; }

		// Excluded because no common search engines ever used it and it's now deprecated.
		//// public float Score { get; }

		public string? SectionSnippet { get; }

		public string? SectionTitle { get; }

		public int Size { get; }

		public string? Snippet { get; }

		public DateTime? Timestamp { get; }

		public string FullPageName { get; }

		public string? TitleSnippet { get; }

		public int WordCount { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.FullPageName;
		#endregion
	}
}
