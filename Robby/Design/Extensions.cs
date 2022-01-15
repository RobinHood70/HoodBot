namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using RobinHood70.CommonCode;

	/// <summary>A generic set of extensions useful in the program's design.</summary>
	public static class Extensions
	{
		#region IEnumerable<ISimpleTitle> Extensions

		/// <summary>Convert a collection of SimpleTitles to their full page names.</summary>
		/// <param name="titles">The titles to convert.</param>
		/// <returns>An enumeration of the titles converted to their full page names.</returns>
		public static IEnumerable<string> ToFullPageNames(this IEnumerable<ISimpleTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					yield return title.FullPageName();
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

		#region ISimpleTitle Extensions

		/// <summary>Returns the provided title as link text.</summary>
		/// <param name="title">The title to get the link text for.</param>
		/// <param name="friendly">Whether to format the link as friendly (<c>[[Talk:Page|Page]]</c>) or raw (<c>[[Talk:Page]]</c>).</param>
		/// <returns>The current title, formatted as a link.</returns>
		public static string AsLink(this ISimpleTitle title, bool friendly)
		{
			var linkName = title.NotNull(nameof(title)).Namespace.LinkName;
			StringBuilder sb = new(linkName.Length + 5 + (title.PageName.Length << 1));
			sb
				.Append("[[")
				.Append(linkName)
				.Append(title.PageName);
			if (friendly)
			{
				sb
					.Append('|')
					.Append(title.LabelName());
			}

			sb.Append("]]");
			return sb.ToString();
		}

		/// <summary>Compares two <see cref="ISimpleTitle"/> objects for namespace and page name equality.</summary>
		/// <param name="title">The title to check.</param>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public static bool SimpleEquals(this ISimpleTitle title, ISimpleTitle other) =>
			title == null ? other == null :
			other != null &&
			title.Namespace == other.Namespace &&
			title.Namespace.PageNameEquals(title.PageName, other.PageName, false);

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="title">The title to get the label name for.</param>
		/// <remarks>This doesn't precisely match the pipe trick logic - they differ in their handling of some abnormal page names. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text".</returns>
		[return: NotNullIfNotNull("title")]
		public static string? LabelName(this ISimpleTitle? title) => title == null
			? null
			: TitleFactory.LabelName(title.PageName);
		#endregion
	}
}