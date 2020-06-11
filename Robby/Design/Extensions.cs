namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Text.RegularExpressions;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>A generic set of extensions useful in the program's design.</summary>
	public static class Extensions
	{
		#region Constants
		// The following is taken from DefaultSettings::$wgLegalTitleChars and always assumes the default setting. I believe this is emitted as part of API:Siteinfo, but I wouldn't trust any kind of automated conversion, so better to just leave it as default, which is what 99.99% of wikis will probably use.
		private const string TitleChars = @"[ %!\""$&'()*,\-.\/0-9:;=?@A-Z\\^_`a-z~+\P{IsBasicLatin}-[()（）]]";
		#endregion

		#region Fields
		private static readonly Regex LabelCommaRemover = new Regex(@"\ *([,，]" + TitleChars + @"*?)\Z", RegexOptions.Compiled);
		private static readonly Regex LabelParenthesesRemover = new Regex(@"\ *(\(" + TitleChars + @"*?\)|（" + TitleChars + @"*?）)\Z", RegexOptions.Compiled);
		#endregion

		#region ISimpleTitle Extensions

		/// <summary>Returns the provided title as link text.</summary>
		/// <param name="title">The title to get the link text for.</param>
		/// <param name="friendly">Whether to format the link as friendly (<c>[[Talk:Page|Page]]</c>) or raw (<c>[[Talk:Page]]</c>).</param>
		/// <returns>The current title, formatted as a link.</returns>
		public static string AsLink(this ISimpleTitle title, bool friendly) => "[[" + FullPageName(title) + (friendly ? "|" + LabelName(title) : string.Empty) + "]]";

		/// <summary>Gets the full page name of a title.</summary>
		/// <param name="title">The titles to use.</param>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		public static string FullPageName(this ISimpleTitle? title)
		{
			ThrowNull(title, nameof(title));
			return title.Namespace.DecoratedName + title.PageName;
		}

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="title">The title to get the label name for.</param>
		/// <remarks>This doesn't precisely match the pipe trick logic - they differ in their handling of some abnormal page names. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text". The text should already have been cleaned using Fixup().</returns>
		[return: NotNullIfNotNull("title")]
		public static string? LabelName(this ISimpleTitle title)
		{
			if (title == null)
			{
				return null;
			}

			var pageName = LabelCommaRemover.Replace(title.PageName, string.Empty, 1, 1);
			pageName = LabelParenthesesRemover.Replace(pageName, string.Empty, 1, 1);
			return pageName;
		}

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="title">The title to check.</param>
		/// <param name="other">The title to compare to.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		public static bool SimpleEquals([AllowNull] this ISimpleTitle title, ISimpleTitle? other) =>
			title == null ? other == null :
			other != null &&
			title.Namespace == other.Namespace &&
			title.Namespace.PageNameEquals(title.PageName, other.PageName);
		#endregion

		#region IFullTitle Extensions

		/// <summary>Gets the label name for the title.</summary>
		/// <param name="title">The title to get the label name for.</param>
		/// <remarks>Unlike the regular pipe trick, this will take a non-empty fragment name in preference to the page name.</remarks>
		/// <returns>A name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</returns>
		public static string? LabelName(this IFullTitle? title) => title == null ? null : (title.Fragment?.Trim() ?? LabelName(title as ISimpleTitle));
		#endregion

		#region IEnumerable<ISimpleTitle> Extensions

		/// <summary>Convert a collection of ISimpleTitles to their full page names.</summary>
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

		/// <summary>Convert a collection of ISimpleTitles to their page names, ignoring namespace.</summary>
		/// <param name="titles">The titles to convert.</param>
		/// <returns>An enumeration of the titles converted to their page names, ignoring namespace.</returns>
		public static IEnumerable<string> ToPageNames(this IEnumerable<ISimpleTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					yield return title.PageName;
				}
			}
		}
		#endregion
	}
}