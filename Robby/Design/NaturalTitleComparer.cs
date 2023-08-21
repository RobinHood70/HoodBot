namespace RobinHood70.Robby.Design
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.CommonCode;

	/// <summary>An ISimpleTitle comparer which sorts by namespace and page name.</summary>
	/// <seealso cref="Comparer{T}" />
	public sealed class NaturalTitleComparer : IComparer<ITitle>, IComparer
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
		public static NaturalTitleComparer Instance { get; } = new NaturalTitleComparer();
		#endregion

		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(ITitle? x, ITitle? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}

			if (y is null)
			{
				return 1;
			}

			var xTitle = x.Title;
			var yTitle = y.Title;
			var nsCompare = xTitle.Namespace.Id.CompareTo(yTitle.Namespace.Id);
			if (nsCompare != 0)
			{
				return nsCompare;
			}

			if (xTitle.PageName == null)
			{
				return yTitle.PageName == null ? 0 : -1;
			}

			if (yTitle.PageName == null)
			{
				return 1;
			}

			var siteCulture = xTitle.Namespace.Site.Culture;
			return xTitle.Namespace.CaseSensitive
				? NaturalSort.Compare(xTitle.PageName, yTitle.PageName, siteCulture, CompareOptions.None)
				: NaturalSort.Compare(xTitle.PageName.UpperFirst(siteCulture), yTitle.PageName.UpperFirst(siteCulture), siteCulture, CompareOptions.IgnoreCase);
		}

		int IComparer.Compare(object? x, object? y) => this.Compare(x as ITitle, y as ITitle);
		#endregion
	}
}
