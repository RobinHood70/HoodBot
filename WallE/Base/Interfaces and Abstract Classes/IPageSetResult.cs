namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	/// <summary>Interface for all pageset types.</summary>
	public interface IPageSetResult
	{
		#region Properties

		/// <summary>Gets the revision IDs that could not be found on the wiki.</summary>
		/// <value>The bad revision ids.</value>
		IReadOnlyList<long> BadRevisionIds { get; }

		/// <summary>Gets the page titles that were language-variant converted.</summary>
		/// <value>The page titles that were language-variant converted.</value>
		IReadOnlyDictionary<string, string> Converted { get; }

		/// <summary>Gets the page titles that resolved to interwiki links.</summary>
		/// <value>The page titles that resolved to interwiki links.</value>
		IReadOnlyDictionary<string, InterwikiTitleItem> Interwiki { get; }

		/// <summary>Gets the page titles that were normalized (e.g., underscores converted to spaces).</summary>
		/// <value>The page titles that were normalized.</value>
		IReadOnlyDictionary<string, string> Normalized { get; }

		/// <summary>Gets the page titles that were redirects.</summary>
		/// <value>The page titles that were redirects.</value>
		IReadOnlyDictionary<string, PageSetRedirectItem> Redirects { get; }
		#endregion
	}
}
