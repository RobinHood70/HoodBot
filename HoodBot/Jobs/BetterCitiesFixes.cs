namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;
	using static RobinHood70.CommonCode.Globals;

	public class BetterCitiesFixes : EditJob
	{
		#region Static Fields
		private static readonly Regex BoldBulletsReplacer = new Regex(@"\* *'''(?<line>.*?)'''", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex BreakReplacer = new Regex(@"\n*<br/?>\s*<br/?>\n*", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex ExternalLinkReplacer = new Regex(@"\[https?://www\.uesp\.net/wiki/(?<page>[^ \]]*)(?<params>&[^ \]]*)? ?(?<title>[^\]]*)\]", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex QuestLinkFixer = new Regex(@"({{Quest Link.*?}}).*?(</noinclude>)?\n", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex TrailingWhitespaceReplacer = new Regex(@"[ \t]*\n", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		#endregion

		#region Constructors
		[JobInfo("Better Cities Fixer", "Better Cities")]
		public BetterCitiesFixes(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var quests = this.GetQuests();
			var loreBooks = this.GetLoreBooks();

			this.Pages.GetNamespace(UespNamespaces.Tes4Mod, Filter.Any, "Better Cities/");
			//// this.Pages.GetRevisionIds(new[] { 2251055L });
			//// this.Pages.GetTitles("Tes4Mod:Better Cities/Amelia");
			foreach (var page in this.Pages)
			{
				page.Text = page.Text.Trim();
				ReplaceBreaks(page);
				ReplaceTrailingWhitespace(page);
				ChangeExternalLinks(page);
				RemoveBold(page);
				DoTrail(page);

				var parser = new ContextualParser(page);
				DoBookSummary(parser, loreBooks);
				FixFactions(parser);
				FixUpLinks(parser);
				FixQuestLinks(parser, quests);
				AddBold(parser, page.SubPageName);
				page.Text = parser.ToRaw().Trim();
				page.Text = QuestLinkFixer.Replace(page.Text, "$1$2\n");
			}
		}

		protected override void Main() => this.SavePages("Initial fix-up of Better Cities pages");
		#endregion

		#region Private Static Methods
		private static void AddBold(ContextualParser parser, string pageName)
		{
			foreach (var textNode in parser.TextNodes)
			{
				var search = $"'''{pageName}'''";
				if (textNode.Text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1)
				{
					return;
				}

				search = '\n' + search + " is";
				var find = $"\n{pageName} is";
				var index = textNode.Text.IndexOf(find, StringComparison.Ordinal);
				if (index != -1)
				{
					textNode.Text = textNode.Text
						.Remove(index, find.Length)
						.Insert(index, search);
				}
			}
		}

		private static void ChangeExternalLinks(Page page) => page.Text = ExternalLinkReplacer.Replace(page.Text, ExternalLinkValidator);

		private static void DoBookSummary(ContextualParser parser, HashSet<string> loreBooks)
		{
			var nodes = parser.Nodes;
			var i = nodes.FindIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals("Book Summary"));
			if (i != -1)
			{
				var template = (SiteTemplateNode)parser.Nodes[i];
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

		private static void DoTrail(Page page)
		{
			var text = page.Text;
			if (text.IndexOf("<small>", StringComparison.OrdinalIgnoreCase) is var trailStart && trailStart != -1 &&
				text.IndexOf("</small>", StringComparison.OrdinalIgnoreCase) is var trailEnd && trailEnd != -1)
			{
				text = text.Remove(trailStart, trailEnd + 8 - trailStart);
			}

			if (trailStart == -1)
			{
				trailStart = 0;
			}

			string? trail = null;
			if (page.PageName.EndsWith(" People", StringComparison.OrdinalIgnoreCase))
			{
				trail = "{{Trail|People}}\n";
			}
			else if (text.Contains("{{Quest Header", StringComparison.OrdinalIgnoreCase))
			{
				trail = "{{Trail|Quests}}";
			}

			if (trail != null)
			{
				text = text.Insert(trailStart, trail);
			}

			page.Text = text;
		}

		private static string ExternalLinkValidator(Match match) => match.Groups["params"].Success
			? match.ToString()
			: $"[[{match.Groups["page"].Value}|{match.Groups["title"].Value}]]";

		private static void FixFactions(ContextualParser parser)
		{
			foreach (var npcSummary in parser.FindTemplates("NPC Summary"))
			{
				if (npcSummary.Find("faction") is IParameterNode faction)
				{
					var value = faction.Value;
					var text = value.ToRaw().Split(TextArrays.Comma);
					value.Clear();
					var first = true;
					foreach (var entry in text)
					{
						var trimmed = entry.Trim();
						if (!string.Equals(trimmed, "None", StringComparison.OrdinalIgnoreCase))
						{
							if (first)
							{
								first = false;
							}
							else
							{
								value.AddText(", ");
							}

							value.Add(parser.Nodes.Factory.TemplateNodeFromParts("Faction", (null, trimmed)));
						}
					}

					value.AddText("\n");
				}
			}
		}

		private static void FixQuestLinks(ContextualParser parser, HashSet<string> quests)
		{
			var nodes = parser.Nodes;
			for (var i = 0; i < nodes.Count - 1; i++)
			{
				if (
					nodes[i] is SiteLinkNode link &&
					nodes[i + 1] is TextNode textNode &&
					textNode.Text[0] == ':' &&
					link.TitleValue.SubPageName is var questName &&
					quests.Contains(questName))
				{
					var parms = new List<string>
					{
						questName
					};
					var altName = link.Parameters[0].Value.ToRaw();
					if (!string.Equals(questName, altName, StringComparison.OrdinalIgnoreCase))
					{
						parms.Add(altName);
					}

					nodes.RemoveAt(i);
					nodes.Insert(i, parser.Nodes.Factory.TemplateNodeFromParts("Quest Link", false, parms));

					// Old description is removed in post-parser section of BeforeLogging(). If we need to keep this for future use, might be better to put this in pre-parser, split into lines, then parse each line to see if it's a quest link.
				}
			}
		}

		private static void FixUpLinks(ContextualParser parser)
		{
			foreach (var link in parser.LinkNodes)
			{
				var siteLink = FullTitle.FromBacklinkNode(parser.Site, link);
				link.Title.Clear();
				link.Title.AddText(siteLink.ToString().Replace("/Books:", "/", StringComparison.Ordinal));
			}
		}

		private static void RemoveBold(Page page) => page.Text = BoldBulletsReplacer.Replace(page.Text, "* ${line}");

		private static void ReplaceBreaks(Page page) => page.Text = BreakReplacer.Replace(page.Text, "\n\n");

		private static void ReplaceTrailingWhitespace(Page page) => page.Text = TrailingWhitespaceReplacer.Replace(page.Text, "\n");
		#endregion

		#region Private Methods
		private HashSet<string> GetLoreBooks()
		{
			var loreBookTitles = new TitleCollection(this.Site);
			loreBookTitles.GetBacklinks("Template:Lore Book", BacklinksTypes.EmbeddedIn, true, Filter.Any, UespNamespaces.Lore);
			var retval = new HashSet<string>(loreBookTitles.Count, StringComparer.Ordinal);
			foreach (var book in loreBookTitles)
			{
				retval.Add(book.PageName);
			}

			return retval;
		}

		private HashSet<string> GetQuests()
		{
			var questTitles = new TitleCollection(this.Site);
			questTitles.GetBacklinks("Template:Quest Header", BacklinksTypes.EmbeddedIn, true, Filter.Any, UespNamespaces.Tes4Mod);
			var retval = new HashSet<string>(questTitles.Count, StringComparer.Ordinal);
			foreach (var quest in questTitles)
			{
				var prefix = "Better Cities";
				if (string.Equals(quest.RootPageName, prefix, StringComparison.Ordinal))
				{
					retval.Add(quest.PageName[(prefix.Length + 1)..]);
				}
			}

			retval.TrimExcess();
			return retval;
		}
		#endregion
	}
}
