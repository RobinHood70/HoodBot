namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	/// <summary>Interface for all pageset types.</summary>
	public interface IPageSetResult
	{
		#region Properties
		/// <summary>The list of revision IDs that could not be found on the wiki.</summary>
		IReadOnlyList<long> BadRevisionIds { get; set; }

		/// <summary>The list of page titles that were converted."</summary>
		IReadOnlyDictionary<string, string> Converted { get; set; }

		/// <summary>The list of page titles that resolved to Interwiki links.</summary>
		IReadOnlyDictionary<string, InterwikiTitleItem> Interwiki { get; set; }

		/// <summary>The list of page titles that were normalized (e.g., underscores converted to spaces).</summary>
		IReadOnlyDictionary<string, string> Normalized { get; set; }

		/// <summary>The list of page titles that were redirects.</summary>
		IReadOnlyDictionary<string, PageSetRedirectItem> Redirects { get; set; }
		#endregion
	}
}
