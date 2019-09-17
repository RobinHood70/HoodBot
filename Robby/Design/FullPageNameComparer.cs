namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;

	/// <summary>An ISimpleTitle comparer to sort by full page name.</summary>
	/// <seealso cref="IComparer{T}" />
	public class FullPageNameComparer : IComparer<ISimpleTitle>
	{
		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(ISimpleTitle x, ISimpleTitle y) =>
			x == null
			? y == null ? 0 : -1
			: y == null ? 1 : string.Compare(x.FullPageName, y.FullPageName, StringComparison.Ordinal);
		#endregion
	}
}