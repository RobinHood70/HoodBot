namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>An ISimpleTitle equality comparer to determine equality based on the Namespace and PageName only.</summary>
	/// <seealso cref="EqualityComparer{T}" />
	public sealed class FullTitleEqualityComparer : IEqualityComparer<IFullTitle>, IEqualityComparer
	{
		#region Constructors
		private FullTitleEqualityComparer()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the singleton instance.</summary>
		/// <value>The instance.</value>
		public static FullTitleEqualityComparer Instance { get; } = new FullTitleEqualityComparer();
		#endregion

		#region Public Override Methods

		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type <see cref="IFullTitle" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="IFullTitle" /> to compare.</param>
		/// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
		public bool Equals(IFullTitle? x, IFullTitle? y) => x == null
			? y == null
			: y != null
				&& x.Interwiki == y.Interwiki
				&& x.Namespace == y.Namespace
				&& x.Namespace.PageNameEquals(x.PageName, y.PageName, false)
				&& string.Equals(x.Fragment, y.Fragment, StringComparison.Ordinal);

		/// <summary>Returns a hash code for this instance.</summary>
		/// <param name="obj">The object.</param>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public int GetHashCode(IFullTitle? obj) => obj == null ? 0 : HashCode.Combine(obj.Interwiki, obj.Namespace, obj.PageName, obj.Fragment);

		bool IEqualityComparer.Equals(object? x, object? y) => x == y || (x is IFullTitle newX && y is IFullTitle newY && this.Equals(newX, newY));

		int IEqualityComparer.GetHashCode(object obj) => this.GetHashCode(obj as IFullTitle);
		#endregion
	}
}
