namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Static Fields
		private static readonly Regex UpdateFinder = new(@"Update\s*(?<num>\d\d)", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly Dictionary<Title, string> dict = [];
		private readonly TitleCollection navboxes = new(jobManager.Site);
		private readonly TitleCollection notAdded = new(jobManager.Site);
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Add Release Notes";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages()
		{
			if (this.notAdded.Count == 0)
			{
				return;
			}

			this.notAdded.Sort();
			this.WriteLine("Pages that want a Release Notes section but don't have either a Housing section or existing Release Notes:");
			foreach (var title in this.notAdded)
			{
				this.WriteLine($"* [[{title.FullPageName()}]]");
			}
		}

		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			this.Shuffle = true;
			this.navboxes.GetCategoryMembers("Navbox Templates", true);
			var fileName = LocalConfig.BotDataSubPath("Online Furnishing Summary - Release Dates.csv");
			var csvFile = new CsvFile();
			csvFile.Load(fileName, true);
			foreach (var row in csvFile)
			{
				var title = TitleFactory.FromUnvalidated(this.Site, row["Page"]);
				var value = row["Update"];
				if (!string.IsNullOrEmpty(value))
				{
					this.dict.Add(title, value);
				}
			}

			this.Pages.GetTitles(this.dict.Keys);
		}

		protected override void PageLoaded(Page page)
		{
			page.Text = page.Text.Replace("<!--Instructions", "==Instructions~HoodBot==\n<!--Instructions", StringComparison.Ordinal);
			base.PageLoaded(page);
			page.Text = page.Text.Replace("==Instructions~HoodBot==\n<!--Instructions", "<!--Instructions", StringComparison.Ordinal);
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Online Release Notes") is not null)
			{
				return;
			}

			var sections = parser.ToSections(2);
			var prev =
				RemoveReleaseNotes(sections) ??
				FindHouses(sections) ??
				FindGallery(sections) ??
				sections[^1].Content;
			if (prev is null)
			{
				this.notAdded.Add(parser.Title);
				return;
			}

			// Remove immediately preceding NewLeft or whitespace, if any
			var oldCount = prev.Count + 1;
			while (prev.Count > 0 && prev.Count < oldCount)
			{
				oldCount = prev.Count;
				if (prev[^1] is ITextNode text && text.Text.Trim().Length == 0)
				{
					prev.RemoveAt(prev.Count - 1);
				}

				if (prev[^1] is SiteTemplateNode newLeft && newLeft.TitleValue.PageNameEquals("NewLeft"))
				{
					prev.RemoveAt(prev.Count - 1);
				}
			}

			this.AddTemplate(prev, this.dict[parser.Title]);
			parser.FromSections(sections);
		}
		#endregion

		#region Private Static Methods
		private static NodeCollection? FindGallery(IList<Section> sections)
		{
			for (var sectionNum = 1; sectionNum < sections.Count; sectionNum++)
			{
				var section = sections[sectionNum];
				if (string.Equals(section.Header?.GetTitle(true), "Gallery", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(section.Header?.GetTitle(true), "See Also", StringComparison.OrdinalIgnoreCase))
				{
					return sections[sectionNum - 1].Content;
				}
			}

			return null;
		}

		private static NodeCollection? FindHouses(IList<Section> sections)
		{
			for (var sectionNum = 1; sectionNum < sections.Count; sectionNum++)
			{
				var section = sections[sectionNum];
				if (string.Equals(section.Header?.GetTitle(true), "Houses", StringComparison.OrdinalIgnoreCase))
				{
					return sections[sectionNum - 1].Content;
				}
			}

			return null;
		}

		private static NodeCollection? RemoveReleaseNotes(IList<Section> sections)
		{
			for (var sectionNum = 1; sectionNum < sections.Count; sectionNum++)
			{
				var section = sections[sectionNum];
				if (string.Equals(section.Header?.GetTitle(true), "Release Notes", StringComparison.OrdinalIgnoreCase))
				{
					sections.RemoveAt(sectionNum);
					return sections[sectionNum - 1].Content;
				}
			}

			return null;
		}
		#endregion

		#region Private Methods
		private void AddTemplate(NodeCollection prev, string value)
		{
			var template = new StringBuilder();
			template.Append("{{Online Release Notes\n");

			// It seems like there should be a better way to do this, but if so, it's not coming to mind.
			// Since this is a one-off job, I'm not going to worry about it.
			var split = value.Split(TextArrays.NewLineChars, StringSplitOptions.RemoveEmptyEntries);
			var text = string.Empty;
			var update = string.Empty;
			var lineNum = 0;
			var line = split[0];
			while (lineNum < split.Length)
			{
				while (line[0] != '*')
				{
					text += line + '\n';
					lineNum++;
					if (lineNum >= split.Length)
					{
						break;
					}

					line = split[lineNum];
				}

				while (line[0] == '*')
				{
					var match = UpdateFinder.Match(line);
					if (match.Success)
					{
						update = match.Groups["num"].Value;
					}

					lineNum++;
					if (lineNum >= split.Length)
					{
						break;
					}

					line = split[lineNum];
				}

				template
					.Append('|')
					.Append(update)
					.Append('|')
					.Append(text.TrimEnd())
					.Append('\n');
				update = string.Empty;
				text = string.Empty;
			}

			template.Append("}}\n\n");
			prev.TrimEnd();
			var i = prev.Count - 1;
			while (i >= 0 && prev[i] is SiteTemplateNode navbox && this.navboxes.Contains(navbox.TitleValue))
			{
				i--;
			}

			i++;
			if (i == prev.Count)
			{
				template = template.Insert(0, '\n');
			}

			prev.InsertText(i, template.ToString());
		}
		#endregion
	}
}