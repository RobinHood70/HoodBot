#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class SearchResultItem : ITitleOnly
	{
		#region Public Properties
		public int? Namespace { get; set; }

		public string RedirectSnippet { get; set; }

		public string RedirectTitle { get; set; }

		// Excluded because no common search engines ever used it and it's now deprecated.
		//// public float Score { get; set; }

		public string SectionSnippet { get; set; }

		public string SectionTitle { get; set; }

		public int Size { get; set; }

		public string Snippet { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; set; }

		public string TitleSnippet { get; set; }

		public int WordCount { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
