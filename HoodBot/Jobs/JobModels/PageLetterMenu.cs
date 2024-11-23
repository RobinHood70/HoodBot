namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RobinHood70.Robby;

internal static class PageLetterMenu
{
	public static string GetIndexFromText(string text)
	{
		if (text.Length == 0)
		{
			return text;
		}

		var letter = char.ToUpper(text[0], CultureInfo.CurrentCulture);
		return (letter is >= 'A' and <= 'Z')
			? letter.ToString()
			: "Numeric";
	}

	public static IEnumerable<string> GetLetters(bool includeNumeric)
	{
		var allLetters = Enumerable.Range('A', 26);
		foreach (var letter in allLetters)
		{
			yield return ((char)letter).ToString();
		}

		if (includeNumeric)
		{
			yield return "Numeric";
		}
	}

	/// <summary>Gets a <see cref="TitleCollection"/> filled with all possible titles in a PageLetterMenu.</summary>
	/// <param name="site">The site the titles are on.</param>
	/// <param name="prefix">The full page name, minus only the letter (i.e., includes namespace and trailing space, if appropriate).</param>
	/// <param name="includeNumeric">Whether to include the "Numeric" entry.</param>
	/// <returns>A <see cref="TitleCollection"/> of the requested page names.</returns>
	public static TitleCollection GetTitles(Site site, string prefix, bool includeNumeric)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(prefix);
		var retval = new TitleCollection(site);
		foreach (var letter in GetLetters(includeNumeric))
		{
			retval.Add(prefix + letter);
		}

		return retval;
	}
}