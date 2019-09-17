#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using static RobinHood70.WikiCommon.Globals;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Project naming convention takes precedence.")]
	public class PageSetResult<T> : ReadOnlyDictionary<string, T>, IPageSetResult
		where T : ITitle
	{
		#region Constructors
		public PageSetResult(IEnumerable<T> titles)
			: base(ToDictionary(titles))
		{
		}
		#endregion

		#region Properties
		public IReadOnlyList<long> BadRevisionIds { get; set; }

		public IReadOnlyDictionary<string, string> Converted { get; set; }

		public IReadOnlyDictionary<string, InterwikiTitleItem> Interwiki { get; set; }

		public IReadOnlyDictionary<string, string> Normalized { get; set; }

		public IReadOnlyDictionary<string, PageSetRedirectItem> Redirects { get; set; }
		#endregion

		#region Private Static Functions
		private static IDictionary<string, T> ToDictionary(IEnumerable<T> titles)
		{
			ThrowNull(titles, nameof(titles));
			var output = new Dictionary<string, T>();
			foreach (var title in titles)
			{
				output.Add(title.Title, title);
			}

			return output;
		}
		#endregion
	}
}