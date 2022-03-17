namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;

	/// <summary>The format to use for the link text.</summary>
	public enum LinkFormat
	{
		/// <summary>Plain link with no text.</summary>
		Plain,

		/// <summary>Link text should follow "pipe trick" rules.</summary>
		PipeTrick,

		/// <summary>Link text should strip paranthetical text only.</summary>
		LabelName
	}

	/// <summary>A generic set of extensions useful in the program's design.</summary>
	public static class Extensions
	{
		#region IEnumerable<Title> Extensions

		/// <summary>Convert a collection of SimpleTitles to their full page names.</summary>
		/// <param name="titles">The titles to convert.</param>
		/// <returns>An enumeration of the titles converted to their full page names.</returns>
		public static IEnumerable<string> ToFullPageNames(this IEnumerable<Title> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					yield return title.FullPageName;
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
			title.Namespace == other.Namespace &&
			title.Namespace.PageNameEquals(title.PageName, other.PageName, false) && string.Equals(title.Fragment, other.Fragment, System.StringComparison.Ordinal);
		#endregion
	}
}