namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	/// <summary>An IFullTitle comparer which sorts by namespace and page name.</summary>
	/// <seealso cref="Comparer{T}" />
	public sealed class FullTitleComparer : IComparer<IFullTitle>
	{
		#region Constructors
		private FullTitleComparer()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the singleton instance.</summary>
		/// <value>The instance.</value>
		/// <remarks>Note that this is a pseudo-singleton, in that a new instance will be created for each type.</remarks>
		public static FullTitleComparer Instance { get; } = new FullTitleComparer();
		#endregion

		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(IFullTitle? x, IFullTitle? y)
		{
			if (x == null)
			{
				return y == null ? 0 : -1;
			}

			if (y == null)
			{
				return 1;
			}

			var siteCulture = x.Namespace.Site.Culture;
			int retval;
			if (x.Interwiki == null)
			{
				if (y.Interwiki != null)
				{
					return -1;
				}
			}
			else
			{
				if (y.Interwiki == null)
				{
					return 1;
				}

				retval = string.Compare(x.Interwiki.Prefix, y.Interwiki.Prefix, false, siteCulture);
				if (retval != 0)
				{
					return retval;
				}
			}

			retval = x.Namespace.Id.CompareTo(y.Namespace.Id);
			if (retval != 0)
			{
				return retval;
			}

			if (x.PageName == null)
			{
				return y.PageName == null ? 0 : -1;
			}

			if (y.PageName == null)
			{
				return 1;
			}

			retval = x.Namespace.CaseSensitive
				? string.Compare(x.PageName, y.PageName, true, siteCulture)
				: string.Compare(x.PageName.UpperFirst(siteCulture), y.PageName.UpperFirst(siteCulture), true, siteCulture);
			return retval != 0
				? retval
				: string.Compare(x.Fragment, y.Fragment, false, siteCulture);
		}
		#endregion
	}
}
