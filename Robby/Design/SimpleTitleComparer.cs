namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	/// <summary>An ISimpleTitle comparer which sorts by namespace and page name.</summary>
	/// <seealso cref="Comparer{T}" />
	public sealed class SimpleTitleComparer : IComparer<Title>, IComparer, IEqualityComparer<Title>, IEqualityComparer
	{
		#region Constructors
		private SimpleTitleComparer()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the singleton instance.</summary>
		/// <value>The instance.</value>
		/// <remarks>Note that this is a pseudo-singleton, in that a new instance will be created for each type.</remarks>
		public static SimpleTitleComparer Instance { get; } = new SimpleTitleComparer();
		#endregion

		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(Title? x, Title? y)
		{
			if (x == null)
			{
				return y == null ? 0 : -1;
			}

			if (y == null)
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

			var siteCulture = x.Namespace.Site.Culture;
			return x.Namespace.CaseSensitive
				? string.Compare(x.PageName, y.PageName, true, siteCulture)
				: string.Compare(x.PageName.UpperFirst(siteCulture), y.PageName.UpperFirst(siteCulture), true, siteCulture);
		}

		int IComparer.Compare(object? x, object? y) => this.Compare(x as Title, y as Title);
		#endregion

		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type <see cref="Title" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="Title" /> to compare.</param>
		/// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
		public bool Equals(Title? x, Title? y) =>
			x == null ? y == null :
			y != null && x.Namespace == y.Namespace && x.Namespace.PageNameEquals(x.PageName, y.PageName, false);

		bool IEqualityComparer.Equals(object? x, object? y) => x == y || (x is Title newX && y is Title newY && this.Equals(newX, newY));

		/// <summary>Returns a hash code for this instance.</summary>
		/// <param name="obj">The object.</param>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public int GetHashCode(Title? obj) => obj == null ? 0 : HashCode.Combine(obj.Namespace, obj.PageName);

		int IEqualityComparer.GetHashCode(object obj) => this.GetHashCode(obj as Title);
	}
}
