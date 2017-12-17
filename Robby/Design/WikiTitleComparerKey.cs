namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;

	public class WikiTitleComparerKey : IComparer<IWikiTitle>
	{
		#region Public Methods
		public int Compare(IWikiTitle x, IWikiTitle y) =>
			x == null
			? y == null ? 0 : -1
			: y == null ? 1 : string.Compare(x.Key, y.Key, StringComparison.Ordinal);
		#endregion
	}
}