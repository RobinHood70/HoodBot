namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.WikiCommon;

	/// <summary>An ISimpleTitle comparer which sorts by namespace and page name.</summary>
	/// <typeparam name="T">The item types to compare. Must implement ISimpleTitle.</typeparam>
	/// <seealso cref="Comparer{T}" />
	public sealed class TitleComparer<T> : IComparer<T>
		where T : ISimpleTitle
	{
		#region Constructors
		private TitleComparer()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the singleton instance.</summary>
		/// <value>The instance.</value>
		/// <remarks>Note that this is a pseudo-singleton, in that a new instance will be created for each type.</remarks>
		[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Pseudo-singleton comparable to Comparer<T>.Default.")]
		public static TitleComparer<T> Instance { get; } = new TitleComparer<T>();
		#endregion

		#region Public Methods

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
		public int Compare(T x, T y)
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

			var siteCulture = x.Site.Culture;
			return x.Namespace.CaseSensitive
				? string.Compare(x.PageName, y.PageName, true, siteCulture)
				: string.Compare(x.PageName.UpperFirst(siteCulture), y.PageName.UpperFirst(siteCulture), true, siteCulture);
		}
		#endregion
	}
}
