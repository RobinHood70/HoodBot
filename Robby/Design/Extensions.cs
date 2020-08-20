namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
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
		public static string AsLink(this ISimpleTitle title, bool friendly)
		{
			var fullPageName = FullPageName(title);
			var sb = new StringBuilder(fullPageName.Length << 1);
			sb.Append("[[");
			sb.Append(title.Namespace.LinkName);
			sb.Append(title.PageName);
			if (friendly)
			{
				sb.Append('|');
				sb.Append(LabelName(title));
			}

			sb.Append("]]");
			return sb.ToString();
		}

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <param name="title">The title to get the base page name for.</param>
		/// <returns>The name of the base page.</returns>
		public static string BasePageName(this ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			if (title.Namespace.AllowsSubpages)
			{
				var subpageLoc = title.PageName.LastIndexOf('/');
				if (subpageLoc >= 0)
				{
					return title.PageName.Substring(0, subpageLoc);
				}
			}

			return title.PageName;
		}

		/// <summary>Gets the full page name of a title.</summary>
		/// <param name="title">The title to get the full page name for.</param>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		public static string FullPageName(this ISimpleTitle? title)
		{
			ThrowNull(title, nameof(title));
			return title.Namespace.DecoratedName + title.PageName;
		}

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="title">The title to get the label name for.</param>
		/// <remarks>This doesn't precisely match the pipe trick logic - they differ in their handling of some abnormal page names. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text".</returns>
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

		/// <summary>Checks if the current page name is the same as the specified page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="title">The title to check.</param>
		/// <param name="pageName">The page name to compare to.</param>
		/// <returns><see langword="true" /> if the two string are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the parameter is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public static bool PageNameEquals(this ISimpleTitle title, string pageName) => title == null ? pageName == null : title.Namespace.PageNameEquals(title.PageName, pageName);

		/// <summary>Gets the value corresponding to {{ROOTPAGENAME}}.</summary>
		/// <param name="title">The title to get the root page name for.</param>
		/// <returns>The name of the base page.</returns>
		public static string RootPageName(this ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			if (title.Namespace.AllowsSubpages)
			{
				var subpageLoc = title.PageName.IndexOf('/', StringComparison.Ordinal);
				if (subpageLoc >= 0)
				{
					return title.PageName.Substring(0, subpageLoc);
				}
			}

			return title.PageName;
		}

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="title">The title to check.</param>
		/// <param name="other">The title to compare to.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		public static bool SimpleEquals(this ISimpleTitle? title, ISimpleTitle? other) =>
			title == null ? other == null :
			other != null &&
			title.Namespace == other.Namespace &&
			title.Namespace.PageNameEquals(title.PageName, other.PageName);

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <param name="title">The title to get the subject page for.</param>
		/// <returns>The subject page.</returns>
		/// <remarks>If title Title is a subject page, returns itself.</remarks>
		public static ISimpleTitle SubjectPage(this ISimpleTitle title) =>
			title == null ? throw ArgumentNull(nameof(title)) :
			title.Namespace.IsSubjectSpace ? title :
			new Title(title.Namespace.SubjectSpace, title.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <param name="title">The title to get the sub-page name for.</param>
		/// <returns>The name of the subpage.</returns>
		public static string SubpageName(this ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			if (title.Namespace.AllowsSubpages)
			{
				var subpageLoc = title.PageName.LastIndexOf('/') + 1;
				if (subpageLoc > 0)
				{
					return title.PageName.Substring(subpageLoc);
				}
			}

			return title.PageName;
		}

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <param name="title">The title to get the talk page for.</param>
		/// <returns>The talk page.</returns>
		/// <remarks>If <paramref name="title"/> is a talk page, the return value will be the same reference. Returns null for pages which have no associated talk page.</remarks>
		public static ISimpleTitle? TalkPage(this ISimpleTitle title) =>
			title == null ? null :
			title.Namespace.TalkSpace == null ? null :
			title.Namespace.IsTalkSpace ? title :
			new Title(title.Namespace.TalkSpace, title.PageName);
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
			title.Namespace.PageNameEquals(title.PageName, other.PageName) &&
			title.Fragment == other.Fragment;

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