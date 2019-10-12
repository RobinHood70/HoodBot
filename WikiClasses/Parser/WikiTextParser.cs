namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using static WikiCommon.Globals;

	/// <summary>A <see langword="static" /> class which encompasses all common methods for converting a block of text into parsed wikitext nodes.</summary>
	public static class WikiTextParser
	{
		#region Fields
		private static readonly Regex EolNormalizer = new Regex(@"(\r\n|\n\r|\r)");
		#endregion

		#region Public Properties

		/// <summary>Gets the list of tags which are not parsed into wikitext.</summary>
		/// <value>The unparsed tags.</value>
		public static IList<string> UnparsedTags { get; } = new List<string> { "pre", "nowiki", "gallery", "indicator" };
		#endregion

		#region Public Methods

		/// <summary>Normalizes the specified text.</summary>
		/// <param name="text">The text to normalize.</param>
		/// <remarks>Numerous parts of the parser rely on linebreaks being <c>\n</c>. This method provides offers a way to ensure that line endings conform to that expectation. This also removes null characters because while the parser can handle them fine, C# doesn't do so well with them in terms of displaying strings and such, and there really is no reason you should have null characters in wikitext anyway.</remarks>
		/// <returns>The normalized text.</returns>
		public static string Normalize(string text) => EolNormalizer.Replace(text, "\n").Replace("\0", string.Empty);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		public static NodeCollection Parse(string text) => Parse(text, null, false);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="include">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		public static NodeCollection Parse(string text, bool? include, bool strictInclusion)
		{
			ThrowNull(text, nameof(text));
			var stack = new WikiStack(text, UnparsedTags, include, strictInclusion);
			var nodes = stack.GetElements();
			return new NodeCollection(null, nodes);
		}
		#endregion
	}
}