namespace RobinHood70.WikiCommon
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	/// <summary>Provides methods to normalize wiki text or titles.</summary>
	public static class WikiTextUtilities
	{
		#region Static Fields
		private static readonly Regex BidiText = new(@"[\u200E\u200F\u202A\u202B\u202C\u202D\u202E]", RegexOptions.Compiled, Globals.DefaultRegexTimeout); // Taken from MediaWikiTitleCodec->splitTitleString, then converted to Unicode
		private static readonly Regex TitleSpaceText = new(@"[ _\xA0\u1680\u180E\u2000-\u200A\u2028\u2029\u202F\u205F\u3000]", RegexOptions.Compiled, Globals.DefaultRegexTimeout);
		private static readonly Regex SpaceTextHtml = new(@"(&(#32|#x20|nbsp);|[ _\xA0\u1680\u180E\u2000-\u200A\u2028\u2029\u202F\u205F\u3000])", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout); // as above, but already Unicode in MW code, modified to add HTML spaces
		#endregion

		#region Public Methods

		/// <summary>HTML- and URL-decodes the specified text, removes bidirectional text markers, and replaces space-like characters with spaces.</summary>
		/// <param name="text">The text to decode and normalize.</param>
		/// <returns>The original text with bidirectional text markers removed and space-like characters converted to spaces.</returns>
		public static string DecodeAndNormalize([Localizable(false)] string text)
		{
			text = text.NotNull(nameof(text)).Replace("+", "%2B", StringComparison.Ordinal);
			text = WebUtility.UrlDecode(text);
			text = WebUtility.HtmlDecode(text);
			text = RemoveInivisibleCharacters(text);
			return ReplaceTitleSpaces(text, false);
		}

		/// <summary>Removes invisible characters from the text.</summary>
		/// <param name="text">The text.</param>
		/// <returns>The provided text with any invisible characters removed.</returns>
		public static string RemoveInivisibleCharacters([Localizable(false)] string text) => BidiText.Replace(text, string.Empty);

		/// <summary>Replaces any space-like characters with spaces, optionally including basic HTML entities without fully decoding the text.</summary>
		/// <param name="text">The text.</param>
		/// <param name="includeHtmlEntities">if set to <see langword="true"/> also replaces <c>&amp;#32;</c>, <c>&amp;#x20;</c> and <c>&amp;nbsp;</c>.</param>
		/// <returns>The provided text with anything resembling a space converted to a normal space.</returns>
		public static string ReplaceTitleSpaces([Localizable(false)] string text, bool includeHtmlEntities) => (includeHtmlEntities ? SpaceTextHtml : TitleSpaceText).Replace(text, " ");
		#endregion
	}
}
