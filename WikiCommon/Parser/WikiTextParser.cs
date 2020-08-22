namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using System.Text.RegularExpressions;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.CommonCode.Globals;

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
		/// <param name="txt">The text to normalize.</param>
		/// <remarks>Numerous parts of the parser rely on linebreaks being <c>\n</c>. This method provides offers a way to ensure that line endings conform to that expectation. This also removes null characters because while the parser can handle them fine, C# doesn't do so well with them in terms of displaying strings and such, and there really is no reason you should have null characters in wikitext anyway.</remarks>
		/// <returns>The normalized text.</returns>
		public static string Normalize(string txt) => EolNormalizer.Replace(txt, "\n").Replace("\0", string.Empty, StringComparison.Ordinal);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		public static NodeCollection Parse(string? text) => Parse(text, null, false);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="include">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		public static NodeCollection Parse(string? text, bool? include, bool strictInclusion)
		{
			ThrowNull(text, nameof(text));
			var stack = new WikiStack(text ?? string.Empty, UnparsedTags, include, strictInclusion);
			var nodes = stack.GetFinalNodes();
			return new NodeCollection(null, nodes);
		}

		/// <summary>If the text provided represents a single node of the specified type, returns that node. Otherwise, throws an error.</summary>
		/// <typeparam name="T">The type of node desired.</typeparam>
		/// <param name="txt">The text to parse.</param>
		/// <param name="callerName">  The caller member name.</param>
		/// <returns>The single node of the specified type.</returns>
		/// <exception cref="ArgumentException">Thrown if there is more than one node in the collection, or the node is not of the specified type.</exception>
		public static T SingleNode<T>(string txt, [CallerMemberName] string callerName = "<Unknown>")
			where T : IWikiNode
		{
			var parser = Parse(txt);
			return (parser.Count == 1 && parser.First is LinkedListNode<IWikiNode> first && first.Value is T node)
				? node
				: throw new ArgumentException(CurrentCulture(Resources.MalformedNodeText, typeof(T).Name, callerName));
		}
		#endregion
	}
}