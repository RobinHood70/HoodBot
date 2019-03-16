namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;

	public class Section
	{
		#region Constructors
		public Section()
			: this(null, 0, null)
		{
		}

		public Section(string title, int level)
			: this(title, level, null)
		{
		}

		public Section(string title, int level, string text)
		{
			this.Title = title;
			this.Level = level;
			this.Text = text;
		}
		#endregion

		#region Public Static Properties
		public static Regex SectionFinder { get; } = new Regex(@"^(?<addbefore>\<!--\ *)?(?<levelopen>={1,6})(?<wslead>\ *)(?<title>.*?)(?<wstrail>\s*)(?<levelclose>={1,6})(?<addafter>\ *--\>)?\ *\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);
		#endregion

		#region Public Properties
		public string AddAfterTitle { get; set; }

		public string AddBeforeTitle { get; set; }

		public string FormattedTitle
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.Title))
				{
					return null;
				}

				var equals = "======".Substring(0, this.Level);
				return string.Concat(
					this.AddBeforeTitle,
					equals,
					this.TitleLeadingWhiteSpace,
					this.Title,
					this.TitleTrailingWhiteSpace,
					equals,
					this.AddAfterTitle);
			}
		}

		public int Level { get; set; }

		public IList<Section> Subsections { get; } = new List<Section>();

		public string Text { get; set; }

		public string Title { get; set; }

		public string TitleLeadingWhiteSpace { get; set; }

		public string TitleTrailingWhiteSpace { get; set; }
		#endregion

		#region Public Methods
		public string Build() => this.Build(new StringBuilder()).ToString();

		public Section Find(string title)
		{
			foreach (var section in this.Subsections)
			{
				if (section.Title == title)
				{
					return section;
				}
			}

			// Separate loops so that all of current level is checked before trying subtitles.
			foreach (var section in this.Subsections)
			{
				var subTitle = section.Find(title);
				if (subTitle != null)
				{
					return subTitle;
				}
			}

			return null;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion

		#region Internal Static Methods
		internal static Section Parse(MatchCollection matches, string text, ref int offset)
		{
			var retval = new Section();
			Match match = matches[offset];
			var groups = match.Groups;
			var level = groups["levelopen"].Value.Length;
			if (level != groups["levelclose"].Value.Length)
			{
				throw new InvalidOperationException("Different numbers of '=' in Section header.");
			}

			var textStart = match.Index + match.Length;
			retval.AddAfterTitle = groups["addafter"].Value;
			retval.AddBeforeTitle = groups["addbefore"].Value;
			retval.Level = level;
			retval.Text = (offset == matches.Count - 1) ? text.Substring(textStart) : text.Substring(textStart, matches[offset + 1].Index - textStart);
			retval.Title = groups["title"].Value;
			retval.TitleLeadingWhiteSpace = groups["wslead"].Value;
			retval.TitleTrailingWhiteSpace = groups["wstrail"].Value;
			offset++;

			while (offset < matches.Count && matches[offset].Groups["levelopen"].Value.Length > level)
			{
				retval.Subsections.Add(Parse(matches, text, ref offset));
			}

			return retval;
		}
		#endregion

		#region Internal Methods
		internal StringBuilder Build(StringBuilder sb)
		{
			if (this.Title != null)
			{
				sb.Append(this.FormattedTitle + '\n');
			}

			sb.Append(this.Text);
			foreach (var section in this.Subsections)
			{
				if (sb[sb.Length - 1] != '\n')
				{
					sb.Append('\n');
				}

				sb.Append(section.Build());
			}

			return sb;
		}
		#endregion
	}
}