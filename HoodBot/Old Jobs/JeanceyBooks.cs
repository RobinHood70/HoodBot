namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	internal static class JeanceyBooks
	{
		#region Private Constants
		private const string Books29Path = @"D:\Books29\";
		private const string Books30Path = @"D:\Books30\";
		#endregion

		#region Private Methods
		public static void DoBooks()
		{
			// Compare Books for Jeancey.
			List<string> deleted = new();
			List<string> added = new();
			List<string> common = new();

			var fullNames29 = Directory.GetFiles(Books29Path);
			var fullNames30 = Directory.GetFiles(Books30Path);
			HashSet<string> dir29 = new(StringComparer.OrdinalIgnoreCase);
			HashSet<string> dir30 = new(StringComparer.OrdinalIgnoreCase);
			foreach (var book in fullNames29)
			{
				dir29.Add(Path.GetFileName(book));
			}

			foreach (var book in fullNames30)
			{
				dir30.Add(Path.GetFileName(book));
			}

			foreach (var book in dir29)
			{
				if (dir30.Contains(book))
				{
					common.Add(book);
				}
				else
				{
					deleted.Add(book);
				}
			}

			foreach (var book in dir30)
			{
				if (!dir29.Contains(book))
				{
					added.Add(book);
				}
			}

			common.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (var book in common)
			{
				var book29 = File.ReadAllText(Books29Path + book);
				var book30 = File.ReadAllText(Books30Path + book);
				book29 = Regex.Replace(book29, @"\s+", " ", RegexOptions.None, Globals.DefaultRegexTimeout);
				book30 = Regex.Replace(book30, @"\s+", " ", RegexOptions.None, Globals.DefaultRegexTimeout);

				if (!string.Equals(book29, book30, StringComparison.Ordinal))
				{
					Debug.WriteLine(book + " has changed");
				}
			}

			deleted.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (var book in deleted)
			{
				Debug.WriteLine(book + " deleted");
			}

			added.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (var book in added)
			{
				Debug.WriteLine(book + " is new");
			}
		}
		#endregion
	}
}
