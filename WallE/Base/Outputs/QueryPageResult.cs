#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Project naming convention takes precedence.")]
	public class QueryPageResult : ReadOnlyCollection<QueryPageItem>
	{
		#region Constructors
		internal QueryPageResult(IList<QueryPageItem> list, bool cached, DateTime? cachedTimestamp, int maxResults)
			: base(list)
		{
			this.Cached = cached;
			this.CachedTimestamp = cachedTimestamp;
			this.MaxResults = maxResults;
		}
		#endregion

		#region Public Properties
		public bool Cached { get; }

		public DateTime? CachedTimestamp { get; }

		public int MaxResults { get; }
		#endregion
	}
}
