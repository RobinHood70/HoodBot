namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using RobinHood70.CommonCode;

	/// <summary>An ISimpleTitle comparer which sorts by namespace and page name.</summary>
	/// <seealso cref="Comparer{T}" />
	public sealed class NaturalTitleComparer : IComparer<ISimpleTitle>
	{
		#region Constructors
		private NaturalTitleComparer()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the singleton instance.</summary>
		/// <value>The instance.</value>
		/// <remarks>Note that this is a pseudo-singleton, in that a new instance will be created for each type.</remarks>
		[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Pseudo-singleton comparable to Comparer<T>.Default.")]
		public static NaturalTitleComparer Instance { get; } = new NaturalTitleComparer();
		#endregion

		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(ISimpleTitle? x, ISimpleTitle? y)
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
				? NaturalSort.Compare(x.PageName, y.PageName, siteCulture, CompareOptions.None)
				: NaturalSort.Compare(x.PageName.UpperFirst(siteCulture), y.PageName.UpperFirst(siteCulture), siteCulture, CompareOptions.IgnoreCase);
		}
		#endregion
	}
}
