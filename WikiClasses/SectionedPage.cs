namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>A simple class to allow parsing and manipulation of wikitext as lead text with a collection of page sections.</summary>
	public class SectionedPage
	{
		#region Static Fields
		private static readonly Regex SectionFinder = new Regex(@"^(?<addbefore>\<!--\ *)?(?<levelopen>={1,6})(?<wslead>\ *)(?<title>.*?)(?<wstrail>\s*)(?<levelclose>={1,6})(?<addafter>\ *--\>)?\ *\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SectionedPage"/> class.</summary>
		/// <param name="pageText">The page text.</param>
		public SectionedPage(string pageText)
		{
			ThrowNull(pageText, nameof(pageText));
			var titles = SectionFinder.Matches(pageText);
			this.Lead = titles.Count == 0 ? pageText : pageText.Substring(0, titles[0].Index);

			var offset = 0;
			while (offset < titles.Count)
			{
				this.Sections.Add(ParseSection(titles, pageText, ref offset));
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the page footer.</summary>
		/// <remarks>This is a convenience property that allows certain trailing text to be handled separately, if desired. Use one of the ExtractFooter() methods or specify your own text. Any ExtractFooter methods will automatically remove the relevant text from the last section.</remarks>
		public string Footer { get; set; } = string.Empty;

		/// <summary>Gets or sets the lead text.</summary>
		/// <value>The lead.</value>
		public string Lead { get; set; } = string.Empty;

		/// <summary>Gets the collection of page sections.</summary>
		/// <value>The page sections.</value>
		public IList<Section> Sections { get; } = new List<Section>();
		#endregion

		#region Public Methods

		/// <summary>Builds all sections within the page and returns the text of the page.</summary>
		/// <returns>The text of the page.</returns>
		public string Build()
		{
			var sb = new StringBuilder();
			sb.Append(this.Lead);
			foreach (var section in this.Sections)
			{
				if (sb[sb.Length - 1] != '\n')
				{
					sb.Append('\n');
				}

				section.Build(sb);
			}

			if (!string.IsNullOrWhiteSpace(this.Footer))
			{
				if (sb[sb.Length - 1] != '\n')
				{
					sb.Append('\n');
				}

				sb.Append(this.Footer);
			}

			return sb.ToString().Trim();
		}

		/// <summary>Extracts text from the final section and puts it into the Footer section instead.</summary>
		/// <param name="lastTexts">A collection of strings to search for, any one of which designates that the footer starts at that position. Text from the earliest match forwards will be moved into the footer property.</param>
		public void ExtractFooter(IEnumerable<string> lastTexts)
		{
			// Any text coming after the earliest of the lastTexts will be split off from the last section and put into the Footer property.
			ThrowNull(lastTexts, nameof(lastTexts));
			var lastSection = this.GetLastSection();
			var pos = int.MaxValue;
			foreach (var text in lastTexts)
			{
				var textPos = lastSection.Text.IndexOf(text, StringComparison.Ordinal);
				if (textPos >= 0 && textPos < pos)
				{
					pos = textPos;
				}
			}

			if (pos < int.MaxValue)
			{
				this.Footer = lastSection.Text.Substring(pos);
				lastSection.Text = lastSection.Text.Substring(0, pos);
			}
		}

		/// <summary>Extracts text from the final section and puts it into the Footer section instead.</summary>
		/// <param name="lastRegex">A Regex which, if matched within the final section text, will be moved into the footer property.</param>
		public void ExtractFooter(Regex lastRegex)
		{
			// Any text that matches lastRegex will be split off from the last section and put into the Footer property.
			ThrowNull(lastRegex, nameof(lastRegex));
			var lastSection = this.GetLastSection();
			var lastMatch = lastRegex.Match(lastSection.Text);
			if (lastMatch.Success)
			{
				this.Footer = lastSection.Text.Substring(lastMatch.Index);
				lastSection.Text = lastSection.Text.Substring(0, lastMatch.Index);
			}
		}

		/// <summary>Finds the first section on the page with the given title, regardless of level.</summary>
		/// <param name="title">The title.</param>
		/// <returns>The first section on the page with the specified title, or null if no section with that title was found.</returns>
		public Section FindFirstSection(string title)
		{
			foreach (var section in this.FindSection(title))
			{
				return section;
			}

			return null;
		}

		/// <summary>Finds the last section with the specified title, regardless of level.</summary>
		/// <param name="title">The title to search for.</param>
		/// <returns>The last section on the with the specified title, or null if no section with that title was found.</returns>
		public Section FindLastSection(string title)
		{
			Section foundSection = null;
			foreach (var section in this.FindSection(title))
			{
				foundSection = section;
			}

			return foundSection;
		}

		/// <summary>Finds all sections on the page with the given title.</summary>
		/// <param name="title">The title.</param>
		/// <returns>Zero or more sections with the given title.</returns>
		/// <remarks>Sections will be returned from top to bottom of the page.</remarks>
		public IEnumerable<Section> FindSection(string title)
		{
			// Primitive search that assumes you want the first title with the matching name.
			// TODO: If needed, build other FindSections that look for titles with specific levels or level ranges.
			foreach (var section in this.Sections)
			{
				if (section.Title == title)
				{
					yield return section;
				}

				var found = section.Find(title);
				if (found != null)
				{
					yield return found;
				}
			}
		}
		#endregion

		#region Private Static Methods

		/// <summary>Parses the specified MatchCollection into a section.</summary>
		/// <param name="matches">The matches.</param>
		/// <param name="text">The text.</param>
		/// <param name="offset">The offset.</param>
		/// <returns>A section with subsections based on the matches found.</returns>
		/// <exception cref="InvalidOperationException">Different numbers of '=' in Section title.</exception>
		private static Section ParseSection(MatchCollection matches, string text, ref int offset)
		{
			var retval = new Section();
			var match = matches[offset];
			var groups = match.Groups;
			var level = groups["levelopen"].Value.Length;
			if (level != groups["levelclose"].Value.Length)
			{
				throw new InvalidOperationException("Different numbers of '=' in Section title.");
			}

			var textStart = match.Index + match.Length;
			retval.AddAfterTitle = groups["addafter"].Value;
			retval.AddBeforeTitle = groups["addbefore"].Value;
			retval.Level = level;
			retval.Text = (offset == matches.Count - 1) ? text.Substring(textStart) : text.Substring(textStart, matches[offset + 1].Index - textStart);
			retval.Title = new PaddedString(groups["wslead"].Value, groups["title"].Value, groups["wstrail"].Value);
			offset++;

			while (offset < matches.Count && matches[offset].Groups["levelopen"].Value.Length > level)
			{
				retval.Subsections.Add(ParseSection(matches, text, ref offset));
			}

			return retval;
		}
		#endregion

		#region Private Methods

		/// <summary>Gets the last section or subsection on the page.</summary>
		/// <returns>The last section or subsection on the page.</returns>
		private Section GetLastSection()
		{
			if (this.Sections.Count == 0)
			{
				return null;
			}

			var section = this.Sections[this.Sections.Count - 1];
			while (section.Subsections.Count > 0)
			{
				section = section.Subsections[section.Subsections.Count - 1];
			}

			return section;
		}
		#endregion
	}
}