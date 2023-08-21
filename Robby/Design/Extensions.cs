namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;

	/// <summary>A generic set of extensions useful in the program's design.</summary>
	public static class Extensions
	{
		#region IEnumerable<ITitle> Extensions

		/// <summary>Convert a collection of SimpleTitles to their full page names.</summary>
		/// <param name="titles">The titles to convert.</param>
		/// <returns>An enumeration of the titles converted to their full page names.</returns>
		public static IEnumerable<string> ToFullPageNames(this IEnumerable<ITitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					yield return title.Title.FullPageName();
				}
			}
		}
		#endregion

		#region IFullTitle Extensions

		/// <summary>Indicates whether the current title is equal to another title based on Interwiki, Namespace, PageName, and Fragment.</summary>
		/// <param name="title">The title to check.</param>
		/// <param name="other">The title to compare to.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public static bool FullEquals(this IFullTitle? title, IFullTitle? other) =>
			title == null ? other == null :
			other != null &&
			title.Interwiki == other.Interwiki &&
			title.Title.Namespace == other.Title.Namespace &&
			title.Title.Namespace.PageNameEquals(title.Title.PageName, other.Title.PageName, false) && string.Equals(title.Fragment, other.Fragment, System.StringComparison.Ordinal);
		#endregion

		#region IReadOnlyCollection<T> Extensions

		/// <summary>Checks for a likely faster Contains method in the collection before handing it off to Linq to search.</summary>
		/// <typeparam name="T">The type of item to search for.</typeparam>
		/// <param name="collection">The collection to search.</param>
		/// <param name="item">The item to search for.</param>
		/// <returns><see langword="true"/> if the collection contained the item; otherwise, <see langword="false"/>.</returns>
		/// <remarks>As of this writing, Linq does no checking at all on IEnumerables to see if they might contain any kind of high-speed search function. For now, I'm assuming only sets are likely to have one. Sorted collections could search faster, but the ones I can think of are either already sets or require a lot of handling/checking, like <c>List{T}.BinarySearch()</c>.</remarks>
		public static bool Contains<T>(this IReadOnlyCollection<T> collection, T item) => collection is IReadOnlySet<T> set
			? set.Contains(item)
			: System.Linq.Enumerable.Contains(collection, item);
		#endregion
	}
}