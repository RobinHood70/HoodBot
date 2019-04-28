namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using static WikiCommon.Globals;

	public static class WikiTextParser
	{
		#region Fields
		private static readonly ICollection<string> TagList = new[] { "pre", "nowiki", "gallery", "indicator" };
		private static readonly Regex Normalizer = new Regex(@"(\r\n|\n\r|\r)");
		#endregion

		#region Public Methods

		// Numerous parts of the parser rely on linebreaks being \n, so this offers an optional way to ensure that that's the case.
		// This also removes null characters because while the parser can handle them fine, C# doesn't do so well with them in terms of displaying strings and such, and there really is no reason you should have null characters in your text anyway.
		public static string Normalize(string text) => Normalizer.Replace(text, "\n").Replace("\0", string.Empty);

		public static NodeCollection Parse(string text) => Parse(text, null);

		public static NodeCollection Parse(string text, bool? include)
		{
			ThrowNull(text, nameof(text));
			var stack = new WikiStack(text, TagList, include);
			stack.Preprocess();

			return stack.Merge();
		}
		#endregion
	}
}