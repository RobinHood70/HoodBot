namespace RobinHood70.Robby.Design
{
	using System.Collections;
	using System.Collections.Generic;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>An ISimpleTitle equality comparer to determine equality based on the Namespace and PageName only.</summary>
	/// <seealso cref="EqualityComparer{T}" />
	public class SimpleTitleEqualityComparer : IEqualityComparer<ISimpleTitle>, IEqualityComparer
	{
		#region Constructors
		private SimpleTitleEqualityComparer()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the singleton instance.</summary>
		/// <value>The instance.</value>
		public static SimpleTitleEqualityComparer Instance { get; } = new SimpleTitleEqualityComparer();
		#endregion

		#region Public Override Methods

		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type <see cref="ISimpleTitle" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="ISimpleTitle" /> to compare.</param>
		/// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
		public bool Equals(ISimpleTitle? x, ISimpleTitle? y) =>
			x == null ? y == null :
			y != null && x.NamespaceId == y.NamespaceId && x.Namespace.PageNameEquals(x.PageName, y.PageName);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <param name="obj">The object.</param>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public int GetHashCode(ISimpleTitle? obj) => obj == null ? 0 : CompositeHashCode(obj.NamespaceId, obj.PageName);

		bool IEqualityComparer.Equals(object? x, object? y) => x == y || (x is ISimpleTitle newX && y is ISimpleTitle newY && this.Equals(newX, newY));

		int IEqualityComparer.GetHashCode(object obj) => this.GetHashCode(obj as ISimpleTitle);
		#endregion
	}
}
