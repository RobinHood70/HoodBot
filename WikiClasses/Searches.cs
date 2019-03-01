namespace RobinHood70.WikiClasses
{
	using System.Text.RegularExpressions;

	/// <summary>Houses various useful wiki-oriented searches.</summary>
	public static class Searches
	{
		/// <summary>Gets the a Regex to find a table.</summary>
		/// <value>The table finder.</value>
		/// <remarks>This is a very simple Regex which does not attempt to handle nested tables.</remarks>
		public static Regex TableFinder { get; } = new Regex(@"\{\|.*?\n\|\}", RegexOptions.Singleline);
	}
}
