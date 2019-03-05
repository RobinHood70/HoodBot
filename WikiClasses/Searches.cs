namespace RobinHood70.WikiClasses
{
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;

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
		public static Regex TableFinder { get; } = new Regex(@"\{\|.*?\n\|\}", RegexOptions.Singleline);
		#endregion

		#region Public Methods

		/// <summary>  Creates a Regex based on a collection of strings.</summary>
		/// <param name="input">The input.</param>
		/// <param name="casing">The casing to allow for each entry.</param>
		/// <returns>A Regex fragment containing a pipe-separated list of the collection items, with appropriate case modifiers.</returns>
		public static string EnumerableRegex(IEnumerable<string> input, SearchCasing casing)
		{
			if (input == null)
			{
				return null;
			}

			var sb = new StringBuilder();
			foreach (var name in input)
			{
				sb.Append('|');
				if (name.Length > 0)
				{
					if (casing == SearchCasing.IgnoreInitialCaps)
					{
						sb.Append("(?i:" + Regex.Escape(name.Substring(0, 1)) + ")");
						if (name.Length > 1)
						{
							var nameRemainder = Regex.Escape(name.Substring(1));
							nameRemainder = nameRemainder.Replace(@"\ ", @"[_\ ]+");

							sb.Append(nameRemainder);
						}
					}
					else
					{
						sb.Append(Regex.Escape(name));
					}
				}
			}

			if (sb.Length > 0)
			{
				sb.Remove(0, 1);
				return casing == SearchCasing.IgnoreCase
					? "(?i:" + sb.ToString() + ")"
					: sb.ToString();
			}

			return null;
		}
		#endregion
	}
}
