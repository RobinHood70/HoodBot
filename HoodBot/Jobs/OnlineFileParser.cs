namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Convert to Online File")]
	public class OnlineFileParser(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Private Static Fields
		private static readonly HashSet<string> LicenseNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"Licensing",
			"{{int:license-header}}"
		};

		private static readonly HashSet<string> SummaryNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"Summary",
			"{{int:filedesc}}"
		};

		private static readonly HashSet<string> ValidDirs = new(StringComparer.Ordinal)
		{
			"alchemy",
			"banner",
			"class",
			"emotes",
			"guildfinderheraldry",
			"guildranks",
			"internal",
			"mapkey",
			"placeholder",
			"poi",
			"servicemappins",
			"servicetooltipicons",
			"verses",
			"visions",
			"worldeventunits"
		};
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Change to template";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages() => this.Pages.GetTitles(new TitleCollection(this.Site, MediaWikiNamespaces.File, File.ReadLines(@"D:\Data\HoodBot\OriginalFile.txt"))); // this.Pages.GetTitles("File:ON-icon-Murkmire.png");
		/*
				protected override void BeforeMain()
				{
					this.Shuffle = true;
					base.BeforeMain();
				}*/

		protected override void PageLoaded(Page page)
		{
			page.Text = page.Text.Replace(
				   "== Summary ==\n== Summary ==",
				   "== Summary ==",
				   StringComparison.OrdinalIgnoreCase);
			page.Text = page.Text.Replace(
				   ":Original file:",
				   "Original file:",
				   StringComparison.OrdinalIgnoreCase);
			page.Text = page.Text.Replace(
				   "Old file: [Original file:",
				   "Old file:",
				   StringComparison.OrdinalIgnoreCase);
			base.PageLoaded(page);
			page.Text = Regex.Replace(page.Text, "\n{3,}", "\n\n", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Online File") is not null)
			{
				return;
			}

			if (parser.Find<IHeaderNode>(header => SummaryNames.Contains(header.GetTitle(true))) is not null)
			{
				parser.Replace(NodeReplacer, false);
				var sections = parser.ToSections(2);
				var summary = FindSummary(parser, sections)!;
				ParseSummary(parser, summary);
				parser.FromSections(sections);
			}
		}
		#endregion

		#region Private Static Methods
		private static string CleanupFileName(string fileName, string pageName)
		{
			var newName = fileName
				.Trim()
				.Replace("<br/>", "<br>", StringComparison.Ordinal)
				.Replace(".png", string.Empty, StringComparison.Ordinal);
			if (newName.EndsWith("<br>", StringComparison.Ordinal))
			{
				newName = newName[..^4];
			}

			// These can be trusted to be accurate hits within reason, so just use simple string replacement
			newName = newName
				.TrimStart('/')
				.Replace('\\', '/')
				.Replace("depot/esoui/", "esoui/", StringComparison.Ordinal)
				.Replace("game/esoui/", "esoui/", StringComparison.Ordinal)
				.Replace("esoui/art/icons/", string.Empty, StringComparison.Ordinal);
			var fileSplit = new List<string>(newName.Split('/'));
			if (pageName.Contains("-icon-", StringComparison.Ordinal))
			{
				// This check, however, could easily be a false hit in mid string instead of at the start, so check here instead.
				if (string.Equals(fileSplit[0], "icons", StringComparison.Ordinal))
				{
					fileSplit.RemoveAt(0);
				}

				if (fileSplit.Count == 1 || ValidDirs.Contains(fileSplit[0]))
				{
					newName = "esoui/art/icons/" + string.Join('/', fileSplit);
				}
				else
				{
					Debug.WriteLine("Odd file name \"" + newName + "\" on File:" + pageName);
				}
			}

			return newName;
		}

		private static Section? FindSummary(ContextualParser parser, IList<Section> sections)
		{
			Section? summary = null;
			var summarySections = 0;
			foreach (var section in sections)
			{
				var title = section.Header?.GetTitle(true);
				if (title is not null && SummaryNames.Contains(title))
				{
					summary = section; // Set to last Summary section
					summarySections++; // ...but count all of them.
				}
			}

			if (summarySections > 1)
			{
				Debug.WriteLine("Multiple summaries on " + parser.Title.FullPageName());
			}

			return summary;
		}

		private static ICollection<IWikiNode>? NodeReplacer(IWikiNode node)
		{
			switch (node)
			{
				case IHeaderNode headerNode:
					var title = headerNode.GetTitle(true);
					if (LicenseNames.Contains(title))
					{
						return [];
					}

					break;
				case SiteTemplateNode templateNode:
					if (templateNode.TitleValue.PageNameEquals("Zenimage"))
					{
						return [];
					}

					break;
			}

			return null;
		}

		private static void ParseSummary(ContextualParser parser, Section summary)
		{
			var parameters = new List<string>();
			var pageName = parser.Title.PageName;
			var offset = summary.Content.FindIndex<SiteTemplateNode>(n => n.TitleValue.PageNameEquals("Information"));
			string? text = null;
			if (offset != -1)
			{
				var info = (SiteTemplateNode)summary.Content[offset];
				if (info.Find("description") is IParameterNode desc)
				{
					if (desc.Value.Find<SiteTemplateNode>(value => value.TitleValue.PageNameEquals("En")) is SiteTemplateNode en)
					{
						text = en.Find(1)?.Value.ToRaw();
					}

					text ??= desc.Value.ToRaw();
				}
			}

			text ??= summary.Content.ToRaw();
			var index = text.IndexOf("Original file:", StringComparison.OrdinalIgnoreCase);
			if (index == -1)
			{
				return;
			}

			var description = text[..index];
			if (description.Length > 0 && description[^1] != '\n')
			{
				Debug.WriteLine("Bad Original file start on File:" + pageName);
			}

			description = description.Trim();
			if (description.Length > 0)
			{
				parameters.Add("description=" + description);
			}

			var fileNames = new SortedSet<string>(StringComparer.Ordinal);
			var split = new List<string>(text[index..].Split(TextArrays.NewLineChars));

			for (var i = 0; i < split.Count; i++)
			{
				var line = split[i];
				var index2 = line.IndexOf("Original file:", StringComparison.OrdinalIgnoreCase);
				if (index2 > -1)
				{
					var fileNameSplit = line[(index2 + 14)..].ToLowerInvariant().Split(',');
					foreach (var fileName in fileNameSplit)
					{
						var newName = CleanupFileName(fileName, pageName);
						fileNames.Add(newName);
					}

					split.RemoveAt(i);
					i--;
				}
			}

			parameters.Add("originalfile=" + string.Join(", ", fileNames));

			var remainder = string.Empty;
			if (split.Count > 1)
			{
				int i;
				for (i = 0; i < split.Count; i++)
				{
					var line = split[i].Trim();
					if (line.Length > 0 && !line.Contains("Used for:", StringComparison.Ordinal))
					{
						if (!line.StartsWith(':'))
						{
							break;
						}

						var colonSplit = line[1..].Split(TextArrays.Colon, 2);
						parameters.Add(colonSplit[0].Trim() + '|' + (colonSplit.Length == 2 ? colonSplit[1].Trim() : string.Empty));
					}
				}

				if (i < split.Count)
				{
					remainder = string.Join('\n', split[i..]).Trim();
					if (remainder.Length > 0)
					{
						remainder += "\n\n";
					}
				}
			}

			var template = "{{Online File";
			template += parameters.Count switch
			{
				0 => string.Empty,
				1 => '|' + parameters[0],
				_ => "\n|" + string.Join("\n|", parameters) + '\n'
			};

			if (offset != -1)
			{
				template = '\n' + template + "|nosummary=1";
			}
			else
			{
				summary.Header = null;
				summary.Content.Clear();
			}

			template = (template + "}}\n\n" + remainder).TrimEnd();
			var parsed = parser.Parse(template);
			summary.Content.InsertRange(offset + 1, parsed);
		}
		#endregion
	}
}