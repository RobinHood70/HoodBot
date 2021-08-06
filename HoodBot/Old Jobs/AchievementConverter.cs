namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	public class AchievementConverter : EditJob
	{
		#region Private Constants
		private const string TemplateName = "ESO Achievements List";
		#endregion

		#region Static Fields
		private static readonly Regex AchTableFinder = new(@"^\{\|\ *class=""?wikitable""?\ *\n!(\ *colspan=[23]\ *\||\ +(!!|\|\||\n[!|\|]))\ *Achievement\ *(!!|\|\||\n[!|\|])\ *Points\ *(!!|\|\||\n[!|\|])\ *(Description|Summary)(?<hasreward>\ *(!!|\|\||\n[!|\|])\ *Rewards?)?(?<hasnotes>\ *(!!|\|\||\n[!|\|])\ *Notes?)?\ *\n?(?<content>.*?)\|\}", RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline, Globals.DefaultRegexTimeout);
		private static readonly Regex DoubleGroupFinder = new(@"\{\{EAL/Group(?<params>.*?)\}\}\n\{\{EAL/Group\|indent=1", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Regex EntryFinder = new(@"\{\{Online Achievement Entry\|(?<params>[^}]*)\}\}(\ *\|\|\ *(?<posttext>.*?))?\ *$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		/*
				private static readonly Regex GroupFinder = new(@"^\{\{EAL/Group(?<params>.*?(\n\|first=.*?)?)\}\}$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
				private static readonly Regex GroupFinder2 = new(@"^\{\{EAL/Group(?<params>.*?\n.*?)\}\}\n\|", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		*/
		private static readonly Regex RowEliminator = new(@"^\|-\ *\n(\|\ *(rowspan=\d+\ *)?(width=\d+\ *)?(\|\ *)?\n)?", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		private static readonly Regex TitleFinder = new(@"^\|-\ *(?<thick>\{\{ThickLine\}\}\ *)?\n!\ *colspan=[456]\ *\|\ *(?<title>.*?)\ *\n(\|-\ *\{\{ThickLine\}\}\ *\n)?", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		private static readonly Regex UnclosedGroups = new(@"(?<=[^\}])\n\{\{EAL/", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly HashSet<string> titles = new(StringComparer.Ordinal);
		private ColumnNames columns = ColumnNames.None;
		#endregion

		#region Constructors
		[JobInfo("Achievement Converter")]
		public AchievementConverter(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Private Enumerations
		[Flags]
		private enum ColumnNames
		{
			None = 0,
			Notes = 1,
			Reward = 2,
		}
		#endregion

		#region Private Properties
		private int ColumnCount => 1 +
			((this.columns & ColumnNames.Notes) == 0 ? 0 : 1) +
			((this.columns & ColumnNames.Reward) == 0 ? 0 : 1);
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.PageLoaded += this.ParsePage;
			this.Pages.GetBacklinks("Template:Online Achievement Entry", BacklinksTypes.EmbeddedIn);
			//// this.Pages.GetTitles("Online:Morrowind Achievements");
			this.Pages.PageLoaded -= this.ParsePage;

			var saved = this.Site.EditingEnabled;
			this.Site.EditingEnabled = true;
			var purgeTitles = new TitleCollection(this.Site, UespNamespaces.Online, this.titles);
			purgeTitles.Remove("Online:Count");
			var purgePages = this.Site.CreateMetaPageCollection(PageModules.Info, false, "iconname");
			purgePages.GetTitles(purgeTitles);
			purgeTitles.Clear();
			foreach (var page in purgePages)
			{
				if (page.Exists &&
					page is VariablesPage metaPage &&
					metaPage.MainSet is IReadOnlyDictionary<string, string> mainSet &&
					!mainSet.ContainsKey("iconname"))
				{
					purgeTitles.Add(page);
				}
			}

			if (purgeTitles.Count > 0)
			{
				this.StatusWriteLine("Purging pages");
				this.ProgressMaximum = purgeTitles.Count;
				this.Progress = 0;
				var purgeGroup = new TitleCollection(this.Site);
				foreach (var page in purgeTitles)
				{
					purgeGroup.Add(page);
					if (purgeGroup.Count == 10)
					{
						purgeGroup.Purge(PurgeMethod.LinkUpdate);
						purgeGroup.Clear();
					}

					this.Progress++;
				}

				purgeGroup.Purge(PurgeMethod.LinkUpdate);
			}

			this.Site.EditingEnabled = saved;
		}

		protected override void Main() => this.SavePages("Convert to " + TemplateName + " (bot-assisted)", true, this.ParsePage);
		#endregion

		#region Private Static Methods
		private static string BasicTemplateParsing(NodeCollection parsedContent)
		{
			for (var pos = parsedContent.Count - 1; pos >= 0; pos--)
			{
				var node = parsedContent[pos];
				if (node is ITemplateNode template)
				{
					switch (template.GetTitleText().ToLowerInvariant())
					{
						case "online achievement entry":
							template.Remove("noline");
							if (template.Remove("colspan"))
							{
								template.Add("indent", "1");
							}

							if (template.Remove("ThickLine"))
							{
								template.Add("line", "thick");
							}

							break;
						case "nowrap":
							parsedContent.RemoveAt(pos);
							if (template.Find(1) is IParameterNode param)
							{
								parsedContent.InsertRange(pos, param.Value);
							}

							break;
					}
				}
			}

			return parsedContent.ToRaw();
		}

		private static string GroupFixer(NodeCollection parsedContent)
		{
			// Find closed groups with a pipe right after them which should be part of the group.
			for (var pos = parsedContent.Count - 2; pos >= 0; pos--)
			{
				if (parsedContent[pos] is ITemplateNode template &&
					template.GetTitleText().StartsWith("EAL/", StringComparison.Ordinal) &&
					parsedContent[pos + 1] is ITextNode textNode)
				{
					var text = textNode.Text.Replace("}\n}", "}}", StringComparison.Ordinal).TrimStart('}');
					var trimmedText = text.TrimStart();
					if (trimmedText.Length > 0 && trimmedText[0] == '|')
					{
						parsedContent.RemoveAt(pos);
						var newLine = ((text.Length > 0) && (text[0] == '\n')) ? string.Empty : "\n";
						text = WikiTextVisitor.Raw(template)[0..^2] + newLine + text;
					}

					textNode.Text = text;
				}
			}

			return parsedContent.ToRaw();
		}

		private static string StripSingles(NodeCollection parsedContent)
		{
			// Strip EAL/Group wrapper from single-entry groups if possible.
			for (var pos = parsedContent.Count - 1; pos >= 0; pos--)
			{
				var node = parsedContent[pos];
				if (node is ITemplateNode template &&
					string.Equals(template.GetTitleText(), "EAL/Group", StringComparison.Ordinal))
				{
					if (template.Find(1) == null)
					{
						template.Remove("indent");
						if (template.Find("title") == null)
						{
							if (template.Find("groupline") is not IParameterNode)
							{
								template.Find("first")?.Anonymize();
								template.Find("reward")?.Anonymize();
								template.Find("notes")?.Anonymize();
								parsedContent.RemoveAt(pos);
								parsedContent.InsertRange(pos, template.Parameters);
							}
						}
					}
				}
			}

			return parsedContent.ToRaw();
		}

		private static string TitleReplacer(Match match)
		{
			var retval = "{{EAL/Group";
			if (match.Groups["thick"].Success)
			{
				retval += "|groupline=thick";
			}

			return retval + "|title=" + match.Groups["title"].Value + "}}\n";
		}
		#endregion

		#region Private Methods
		private string DeconvertSingles(NodeCollection parsedContent)
		{
			// If a group is still a single entry, rename it as such.
			for (var pos = parsedContent.Count - 1; pos >= 0; pos--)
			{
				var node = parsedContent[pos];
				if (node is ITemplateNode template &&
					string.Equals(template.GetTitleText(), "EAL/Group", StringComparison.Ordinal))
				{
					if (template.Find("title") == null)
					{
						var first = template.Find("first");
						if ((first == null && template.Find(this.ColumnCount + 1) == null) ||
							(first != null && template.Find(1) == null))
						{
							template.SetTitle("EAL/Entry");
							if (template.Find("groupline") is IParameterNode groupLine)
							{
								groupLine.SetName("line");
							}

							if (first != null)
							{
								first.Anonymize();
								template.Find("reward")?.Anonymize();
								template.Find("notes")?.Anonymize();
							}
						}
					}
				}
			}

			return parsedContent.ToRaw();
		}

		private string EntryReplacer(Match match)
		{
			var retval = "{{EAL/Entry|" + match.Groups["params"].Value.Trim();
			if (this.columns != ColumnNames.None && match.Groups["posttext"].Value is string text)
			{
				retval += '|' + text
					.Trim()
					.Replace("||", "|", StringComparison.Ordinal)
					.Replace("| ", "|", StringComparison.Ordinal)
					.Replace(" |", "|", StringComparison.Ordinal);
			}

			return retval + "}}";
		}

		private string ParseEntries(NodeCollection parsedContent)
		{
			for (var pos = parsedContent.Count - 1; pos >= 0; pos--)
			{
				var node = parsedContent[pos];
				if (node is ITemplateNode template)
				{
					switch (template.GetTitleText().ToLowerInvariant())
					{
						case "eal/entry":
							if (template.Find(1) is IParameterNode first)
							{
								this.titles.Add(first.Value.ToValue());
							}

							var numbered = template.GetNumericParametersSorted();
							if (template.Parameters.Count == numbered.Count)
							{
								// Cheating outrageously by inserting parameters into raw text, but whatever works!
								parsedContent.RemoveAt(pos);
								parsedContent.InsertRange(pos, template.Parameters);
							}
							else
							{
								template.SetTitle("EAL/Group");
								template.Find("line")?.SetName("groupline");
								template.Sort("indent", "groupline", "title");
								var firstAnon = template.FindNumberedIndex(1);
								if (template.Find("groupline", "indent") != null && template.Find("title") == null && firstAnon > -1)
								{
									if (firstAnon == 0)
									{
										template.Title.AddText("\n");
									}
									else
									{
										template.Parameters[firstAnon - 1].Value.AddText("\n");
									}

									var param1 = template.Parameters[firstAnon];
									param1.SetName("first");
									var columnCount = this.ColumnCount;
									if (columnCount > 1 && numbered.TryGetValue(2, out var param2))
									{
										param2!.SetName(this.columns.HasFlag(ColumnNames.Reward)
											? "reward"
											: "notes");
										if (columnCount > 2 && numbered.TryGetValue(3, out var param3))
										{
											param3!.SetName("notes");
										}
									}
								}
							}

							break;
					}
				}
			}

			return parsedContent.ToRaw();
		}

		private void ParsePage(object sender, Page page)
		{
			var newPage = new StringBuilder();
			var oldTextIndex = 0;
			if (AchTableFinder.Matches(page.Text) is IEnumerable<Match> matches)
			{
				var factory = new WikiNodeFactory();
				this.columns = ColumnNames.None;
				var savedColumns = this.columns;
				foreach (var match in matches)
				{
					newPage.Append(page.Text[oldTextIndex..match.Index]);
					oldTextIndex = match.Index + match.Length;
					if (this.ParseRows(match, factory) is StringBuilder newText)
					{
						newPage.Append(newText);
					}
					else
					{
						newPage.Append(match.Value);
						this.columns = savedColumns;
					}
				}
			}

			newPage.Append(page.Text[oldTextIndex..]);
			page.Text = newPage.ToString();
		}

		private StringBuilder? ParseRows(Match match, WikiNodeFactory factory)
		{
			var sb = new StringBuilder();
			var hasReward = match.Groups["hasreward"].Success;
			var hasNotes = match.Groups["hasnotes"].Success;
			if (this.columns.HasFlag(ColumnNames.Reward) != hasReward)
			{
				this.columns ^= ColumnNames.Reward;
				sb
					.Append("{{#local:showreward|")
					.Append(hasReward ? "1" : string.Empty)
					.Append("}}");
			}

			if (this.columns.HasFlag(ColumnNames.Notes) != hasNotes)
			{
				this.columns ^= ColumnNames.Notes;
				sb
					.Append("{{#local:shownotes|")
					.Append(hasNotes ? "1" : string.Empty)
					.Append("}}");
			}

			var content = match.Groups["content"].Value
				.Replace("<br /> ", "<br>", StringComparison.Ordinal)
				.Replace("<br/> ", "<br>", StringComparison.Ordinal)
				.Replace("<br> ", "<br>", StringComparison.Ordinal)
				.Replace(" <br>", "<br>", StringComparison.Ordinal)
				.Replace("|||", "||", StringComparison.Ordinal);
			content = BasicTemplateParsing(factory.Parse(content));
			content = EntryFinder.Replace(content, this.EntryReplacer);
			content = TitleFinder.Replace(content, TitleReplacer);
			content = RowEliminator.Replace(content, string.Empty);
			content += "}}";
			content = this.ParseEntries(factory.Parse(content));
			content = DoubleGroupFinder.Replace(content, "{{EAL/Group|indent=1${params}");
			if (content[0] == '|')
			{
				content = content.Insert(0, "{{EAL/Group\n  ");
			}

			content = content.Replace("\n{{EAL/", "}}\n{{EAL/", StringComparison.Ordinal);
			content = GroupFixer(factory.Parse(content));
			content = StripSingles(factory.Parse(content));
			content = content.Replace("\n{{EAL/", "}}\n{{EAL/", StringComparison.Ordinal);
			content = GroupFixer(factory.Parse(content));
			if (content[0] == '|')
			{
				content = content.Insert(0, "{{EAL/Group\n  ");
			}

			content = content
				.TrimStart('}', '\n')
				.Replace("|notes=\n", "\n", StringComparison.Ordinal)
				.Replace("|reward=\n", "\n", StringComparison.Ordinal)
				.Replace("|reward=|", "|", StringComparison.Ordinal)
				.Replace("\n}}", "}}", StringComparison.Ordinal);
			content = this.DeconvertSingles(factory.Parse(content));
			content = GroupFixer(factory.Parse(content));
			content = content
				.Replace("\n|", "\n  |", StringComparison.Ordinal)
				.TrimEnd();

			sb
				.Append("\n{{")
				.Append(TemplateName)
				.Append("|multi=\n")
				.Append(content)
				.Append("\n}}");
			return sb;
		}
		#endregion
	}
}