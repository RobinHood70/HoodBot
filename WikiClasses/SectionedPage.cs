namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using static RobinHood70.WikiCommon.Globals;

	// TODO: This has had a re-think. Needs testing.
	public class SectionedPage
	{
		#region Constructors
		public SectionedPage(string pageText)
		{
			ThrowNull(pageText, nameof(pageText));
			var titles = Section.SectionFinder.Matches(pageText);
			this.Lead = titles.Count == 0 ? pageText : pageText.Substring(0, titles[0].Index);

			var offset = 0;
			while (offset < titles.Count)
			{
				this.Sections.Add(Section.Parse(titles, pageText, ref offset));
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the page footer.</summary>
		/// <remarks>This is a convenience property that allows certain trailing text to be handled separately, if desired. Use one of the ExtractFooter() methods or specify your own text. Any ExtractFooter methods will automatically remove the relevant text from the last section.</remarks>
		public string Footer { get; set; } = string.Empty;

		public string Lead { get; set; } = string.Empty;

		public IList<Section> Sections { get; } = new List<Section>();
		#endregion

		#region Public Methods
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

		public void ExtractFooter()
		{
			// Any lines that start with "[[Category" or "{{" (with some exceptions) will be split off from the last section and put into the Footer property.
			// TODO: This could use a re-think, and being UESP-specific, should be removed from this class.
			if (this.Sections.Count == 0)
			{
				return;
			}

			var lastSection = this.LastSection();
			var lines = lastSection.Text.Split('\n');
			var i = lines.Length - 1;
			var line = lines[i].TrimEnd();
			var lineLower = line.ToLowerInvariant();
			while (i >= 0
					&& (line.Length == 0
						|| lineLower.StartsWith("[[category", StringComparison.Ordinal)
						|| (lineLower.StartsWith("{{", StringComparison.Ordinal)
							&& !lineLower.Contains(":")
							&& !lineLower.StartsWith("{{bug", StringComparison.Ordinal)
							&& !lineLower.StartsWith("{{newleft", StringComparison.Ordinal)
							&& !lineLower.StartsWith("{{newright", StringComparison.Ordinal)
							&& !lineLower.StartsWith("{{newline", StringComparison.Ordinal)
							&& !lineLower.StartsWith("{{quest link", StringComparison.Ordinal))))
			{
				this.Footer = "\n" + line + this.Footer;
				i--;
				if (i >= 0)
				{
					line = lines[i].TrimEnd();
					lineLower = line.ToLowerInvariant();
				}
			}

			if (i < lines.Length - 1)
			{
				lastSection.Text = string.Join("\n", lines, 0, i + 1);
			}

			if (i == -1 && lastSection.AddBeforeTitle.Length == 0 && lastSection.AddAfterTitle.Length == 0)
			{
				throw new InvalidOperationException("Entire section appears to be a footer. This is rather unlikely.");
			}
		}

		public void ExtractFooter(IEnumerable<string> lastTexts)
		{
			// Any text coming after the earliest of the lastTexts will be split off from the last section and put into the Footer property.
			ThrowNull(lastTexts, nameof(lastTexts));
			var lastSection = this.LastSection();
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

		public void ExtractFooter(Regex lastRegex)
		{
			// Any text that matches lastRegex will be split off from the last section and put into the Footer property.
			ThrowNull(lastRegex, nameof(lastRegex));
			var lastSection = this.LastSection();
			var lastMatch = lastRegex.Match(lastSection.Text);
			if (lastMatch.Success)
			{
				this.Footer = lastSection.Text.Substring(lastMatch.Index);
				lastSection.Text = lastSection.Text.Substring(0, lastMatch.Index);
			}
		}

		public Section FindFirstSection(string title)
		{
			foreach (var section in this.FindSection(title))
			{
				return section;
			}

			return null;
		}

		public Section FindLastSection(string title)
		{
			Section foundSection = null;
			foreach (var section in this.FindSection(title))
			{
				foundSection = section;
			}

			return foundSection;
		}

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
			}

			foreach (var section in this.Sections)
			{
				var found = section.Find(title);
				if (found != null)
				{
					yield return found;
				}
			}
		}
		#endregion

		#region Private Methods
		private Section LastSection()
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