namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;

	/// <summary>An IWikiTitle comparer to sort by namespace and page name.</summary>
	/// <seealso cref="System.Collections.Generic.IComparer{T}" />
	public class WikiTitleComparerNamespacePageName : IComparer<IWikiTitle>
	{
		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(IWikiTitle x, IWikiTitle y)
		{
			if (x == null)
			{
				return y == null ? 0 : -1;
			}

			if (y == null)
			{
				return 1;
			}

			if (x.Namespace == null)
			{
				return y.Namespace == null ? 0 : -1;
			}

			if (y.Namespace == null)
			{
				return 1;
			}

			var nsCompare = x.Namespace.Id.CompareTo(y.Namespace.Id);
			if (nsCompare != 0)
			{
				return nsCompare;
			}

			if (x.PageName == null)
			{
				return y.PageName == null ? 0 : -1;
			}

			if (y.PageName == null)
			{
				return 1;
			}

			return string.Compare(x.PageName, y.PageName, StringComparison.Ordinal);
		}
		#endregion
	}
}
