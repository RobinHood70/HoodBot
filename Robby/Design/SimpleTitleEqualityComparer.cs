namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>An ISimpleTitle equality comparer to determine equality based on the Namespace and PageName only.</summary>
	/// <seealso cref="System.Collections.Generic.EqualityComparer{T}" />
	public class SimpleTitleEqualityComparer : EqualityComparer<ISimpleTitle>
	{
		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type <see cref="ISimpleTitle" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="ISimpleTitle" /> to compare.</param>
		/// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
		public override bool Equals(ISimpleTitle x, ISimpleTitle y) =>
			x == null ? x == y :
			y == null ? false :
			x.Namespace.Equals(y.Namespace) && x.Namespace.PageNameEquals(x.PageName, y.PageName);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <param name="obj">The object.</param>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode(ISimpleTitle obj) => obj == null ? 0 : CompositeHashCode(obj.Namespace.GetHashCode(), obj.PageName.GetHashCode());
	}
}
