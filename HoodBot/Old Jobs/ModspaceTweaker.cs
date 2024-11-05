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
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class ModspaceTweaker : EditJob
	{
		#region Static Fields
		// private static readonly Regex BoldBulletsReplacer = new Regex(@"\* *'''(?<line>.*?)'''", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Regex BreakReplacer = new(@"\n*<br/?>\s*<br/?>\n*", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Regex ExternalLinkReplacer = new(@"\[https?://www\.uesp\.net/wiki/(?<page>[^ \]]*)(?<params>&[^ \]]*)? ?(?<title>[^\]]*)\]", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly List<int> Namespaces = new()
		{
			UespNamespaces.Tes3Mod,
			UespNamespaces.Tes4Mod,
			UespNamespaces.Tes5Mod,
		};

		private static readonly Regex QuestLinkPreFixer = new(@"^\*+ *\[\[(?<quest>.*?)(\|(?<alttext>.*?))?]]:\s*(?<desc>.*?)(?<noinclude></noinclude>)?$", RegexOptions.Multiline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Regex TrailingWhitespaceReplacer = new(@"[ \t]*\n", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private readonly TitleCollection quests;
		#endregion

		#region Constructors
		[JobInfo("Modspace Tweaker", "Modspace")]
		public ModspaceTweaker(JobManager jobManager)
			: base(jobManager) => this.quests = new TitleCollection(this.Site);
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var loreBooks = this.GetLoreBooks();
			foreach (var ns in Namespaces)
			{
				this.Pages.GetNamespace(ns);
				this.quests.GetBacklinks("Template:Quest Header", BacklinksTypes.EmbeddedIn, true, Filter.Any, ns);
			}

			foreach (var page in this.Pages)
			{
				var original = page.Text;
				page.Text = page.Text.Trim();
				ReplaceBreaks(page);
				this.ChangeExternalLinks(page);
				this.FixQuestLinksRegex(page);

				var parser = new SiteParser(page);
				DoBookSummary(parser, loreBooks);
				FixFactions(parser);
				AddBold(parser, page.SubPageName);
				page.Text = parser.ToRaw().Trim();
				if (!string.Equals(page.Text, original, StringComparison.Ordinal))
				{
					ReplaceTrailingWhitespace(page);
				}
			}
		}

		protected override void Main() => this.SavePages("Fix various modspace issues");
		#endregion

		#region Private Static Methods
		private static void AddBold(SiteParser parser, string pageName)
		{
			var headerNodes = 0;
			foreach (var node in parser)
			{
				if (node is IHeaderNode)
				{
					if (headerNodes == 1)
					{
						break;
					}

					headerNodes++;
				}
				else if (node is ITextNode textNode)
				{
					if (textNode.Text.IndexOf("'''" + pageName, StringComparison.OrdinalIgnoreCase) != -1 ||
						textNode.Text.IndexOf(pageName + "'''", StringComparison.OrdinalIgnoreCase) != -1)
					{
						break;
					}

					var search = $"\n{pageName} is";
					var index = textNode.Text.IndexOf(search, StringComparison.Ordinal);
					if (index != -1)
					{
						textNode.Text = textNode.Text
							.Remove(index, search.Length)
							.Insert(index, $"\n'''{pageName}''' is");
						break;
					}
				}
			}
		}

		private static void DoBookSummary(SiteParser parser, HashSet<string> loreBooks)
		{
			var nodes = parser;
			var i = nodes.FindIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals("Book Summary"));
			if (i != -1)
			{
				var template = (SiteTemplateNode)parser[i];
				template.Title.Clear();
				template.Title.AddText("Game Book\n");
				if ((template.Find("Fancy") ?? template.Find("fancy")) is IParameterNode fancy)
				{
					var letter = fancy.Value.ToValue().Trim();
					var newNodes = new IWikiNode[]
					{
						nodes.Factory.TextNode("\n"),
						nodes.Factory.TemplateNodeFromParts("LetterPic", (null, letter))
					};

					if (nodes[i + 1] is ITextNode textNode && textNode.Text[0] == '\n')
					{
						textNode.Text = textNode.Text[1..];
					}

					nodes.InsertRange(i + 1, newNodes);
					template.Parameters.Remove(fancy);
				}

				if (template.Find("loc") is IParameterNode loc && loc.Value.Count > 0 && loc.Value[^1] is ITextNode locTextNode)
				{
					locTextNode.Text = locTextNode.Text.TrimEnd().TrimEnd('.') + '\n';
				}

				var loreName = loreBooks.Contains(parser.Context.PageName) ? parser.Context.PageName : "none";
				template.Add("lorename", loreName);
				nodes.AddText("\n");
				nodes.Add(nodes.Factory.TemplateNodeFromParts("Book End"));
			}
		}

		private static void FixFactions(SiteParser parser)
		{
			foreach (var npcSummary in parser.FindTemplates("NPC Summary"))
			{
				if (npcSummary.Find("faction") is IParameterNode faction
					&& faction.Value is var value
					&& value.ToRaw().Trim() is var text
					&& text.Length > 0
					&& !text.Contains("{{", StringComparison.Ordinal)
					&& !text.Contains("[[", StringComparison.Ordinal))
				{
					var split = text.Split(TextArrays.Comma);
					value.Clear();
					var first = true;
					foreach (var entry in split)
					{
						var trimmed = entry.Trim();
						if (trimmed.Length > 0 && !trimmed.OrdinalICEquals("None"))
						{
							if (first)
							{
								first = false;
							}
							else
							{
								value.AddText(", ");
							}

							value.Add(parser.Factory.TemplateNodeFromParts("Faction", (null, trimmed)));
						}
					}

					value.AddText("\n");
				}
			}
		}

		private static void ReplaceBreaks(Page page) => page.Text = BreakReplacer.Replace(page.Text, "\n\n");

		private static void ReplaceTrailingWhitespace(Page page) => page.Text = TrailingWhitespaceReplacer.Replace(page.Text, "\n");
		#endregion

		#region Private Methods
		private void ChangeExternalLinks(Page page) => page.Text = ExternalLinkReplacer.Replace(page.Text, this.ExternalLinkValidator);

		private string ExternalLinkValidator(Match match) => match.Groups["params"].Success
			? match.ToString()
			: $"[[{FullTitle.FromName(this.Site, match.Groups["page"].Value)}|{match.Groups["title"].Value}]]";

		private void FixQuestLinksRegex(Page page) => page.Text = QuestLinkPreFixer.Replace(page.Text, this.QuestLinkReplacer);

		private HashSet<string> GetLoreBooks()
		{
			var loreBookTitles = new TitleCollection(this.Site);
			loreBookTitles.GetBacklinks("Template:Lore Book", BacklinksTypes.EmbeddedIn, true, Filter.Any);
			var loreBooks = new HashSet<string>(loreBookTitles.Count, StringComparer.Ordinal);
			foreach (var book in loreBookTitles)
			{
				loreBooks.Add(book.PageName);
			}

			return loreBooks;
		}

		private string QuestLinkReplacer(Match match)
		{
			var retval = new StringBuilder(match.Length); // Unlikely to be anywhere near that long, but should be a good way to ensure that enough space is always allocated initially.
			var quest = match.Groups["quest"].Value;
			var questTitle = TitleFactory.FromName(this.Site, quest);
			if (!this.quests.Contains(questTitle))
			{
				return match.Value;
			}

			var title = questTitle.SubPageName;
			retval
				.Append("* {{Quest Link|")
				.Append(title);
			var altText = match.Groups["alttext"];
			if (altText.Success && !string.Equals(altText.Value, title, StringComparison.Ordinal))
			{
				retval
					.Append('|')
					.Append(altText.Value);
			}

			retval
				.Append(match.Groups["noinclude"].Value)
				.Append("}}");
			return retval.ToString();
		}
		#endregion
	}
}