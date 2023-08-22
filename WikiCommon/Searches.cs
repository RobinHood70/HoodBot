namespace RobinHood70.WikiCommon
{
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	#region Public Enumerations

	/// <summary>The allowable combinations of upper- and lower-case characters in a search.</summary>
	public enum SearchCasing
	{
		/// <summary>Only exact matches should be allowed.</summary>
		Exact,

		/// <summary>The first character of the text should be case-insensitive; the remainder are case-sensitive.</summary>
		IgnoreInitialCaps,

		/// <summary>The entire text is case-insensitive.</summary>
		IgnoreCase,
	}
	#endregion

	/// <summary>Houses various useful wiki-oriented searches.</summary>
	public static class Searches
	{
		#region Public Properties

		/// <summary>Gets the a Regex to find a table.</summary>
		/// <value>The table finder.</value>
		/// <remarks>This is a very simple Regex which does not attempt to handle nested tables.</remarks>
		public static Regex TableFinder { get; } = new Regex(@"\{\|.*?\n\|\}", RegexOptions.Singleline, Globals.DefaultRegexTimeout);
		#endregion
	}
}