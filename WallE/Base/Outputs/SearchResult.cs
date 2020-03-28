#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using RobinHood70.CommonCode;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Project naming convention takes precedence.")]
	public class SearchResult : ReadOnlyCollection<SearchResultItem>
	{
		#region Constructors
		internal SearchResult(IList<SearchResultItem> list, string? suggestion, int totalHits)
			: base(list)
		{
			this.Suggestion = suggestion;
			this.TotalHits = totalHits;
		}
		#endregion

		#region Public Properties
		public string? Suggestion { get; }

		public int TotalHits { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.TotalHits.ToString(CultureInfo.CurrentCulture) + ": " + this.Suggestion.Ellipsis(20);
		#endregion
	}
}
