namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	/// <summary>An IWikiTitle equality comparer to determine equality based on the namespace, page name, and key.</summary>
	/// <seealso cref="System.Collections.Generic.IComparer{T}" />
	public class WikiTitleEqualityComparerFull : IEqualityComparer<IWikiTitle>
	{
		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type <see cref="IWikiTitle" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="IWikiTitle" /> to compare.</param>
		/// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
		public bool Equals(IWikiTitle x, IWikiTitle y) =>
			x == null ? x == y :
			y == null ? false :
			x.Namespace.Equals(y.Namespace) && x.PageName.Equals(y.PageName) && x.Key.Equals(y.Key);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <param name="obj">The object.</param>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public int GetHashCode(IWikiTitle obj) => obj == null ? 0 : CompositeHashCode(obj.Namespace.GetHashCode(), obj.PageName.GetHashCode(), obj.Key.GetHashCode());
	}
}
