namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WikiCommon.Searches;

	internal sealed class EsoReplacer
	{
		#region Static Fields
		private static readonly Regex EsoLinks = new(@"(?<before>(((''')?([0-9]+(-[0-9]+)?|\{\{huh\}\}|\{\{Nowrap[^}]*\}\})(''')?)%?\s+)?(((or )?more|max(imum)?|of missing|ESO)(\s+|<br>))?)?(?<type>(?-i:Health|Magicka|Physical Penetration|Physical Resistance|Spell Critical|Spell Damage|Spell Penetration|Spell Resistance|Stamina|Ultimate|Weapon Critical|Weapon Damage))(\s(?<after>(Recovery|Regeneration|[0-9]+%)+))?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex NumberStripper = new(@"[0-9]+(-[0-9]+)?%?\s*", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex ReplacementFinder = new(@"^\|\ *(<nowiki/?>)?(?<from>.*?)(</?nowiki/?>)?\ *\|\|\ *(<nowiki/?>)?(?<to>.*?)(</?nowiki/?>)?\ *$", RegexOptions.Multiline | RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex SpaceStripper = new(@"(\s{2,}|\n)", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly List<EsoReplacement> ReplaceAllList = new();
		private static readonly List<EsoReplacement> ReplaceFirstList = new();
		private static readonly ICollection<string> UnreplacedList = new SortedSet<string>(StringComparer.Ordinal);
		private static readonly string[] ResistanceSplit = new[] { " Resistance" };

		private static bool initialized;
		#endregion

		#region Fields
		private readonly Site site;
		#endregion

		#region Constructors
		public EsoReplacer(Site site)
		{
			this.site = site ?? throw ArgumentNull(nameof(site));
			this.RemoveableTemplates = new TitleCollection(
				site,
				MediaWikiNamespaces.Template,
				"ESO Health Link",
				"ESO MagStam Link",
				"ESO Magicka Link",
				"ESO Physical Penetration Link",
				"ESO Quality Color",
				"ESO Resistance Link",
				"ESO Spell Critical Link",
				"ESO Spell Damage Link",
				"ESO Spell Penetration Link",
				"ESO Stamina Link",
				"ESO Synergy Link",
				"ESO Ultimate Link",
				"ESO Weapon Critical Link",
				"ESO Weapon Damage Link",
				"Nowrap");
		}
		#endregion

		#region Public Properties
		public TitleCollection RemoveableTemplates { get; }
		#endregion

		#region Public Static Methods
		public static string? ConstructWarning(Page page, ICollection<ISimpleTitle> titles, string warningType)
		{
			if (titles.Count > 0)
			{
				var warning = new StringBuilder();
				warning
					.Append("Watch for ")
					.Append(warningType)
					.Append(" on ")
					.Append(page.FullPageName)
					.Append(": ");
				foreach (var link in titles)
				{
					warning
						.Append(link)
						.Append(", ");
				}

				warning.Remove(warning.Length - 2, 2);
				return warning.ToString();
			}

			return null;
		}

		public static void Initialize(WikiJob job)
		{
			if (!initialized)
			{
				var jobSite = job.Site;
				job.StatusWriteLine("Parsing replacements");
				if (jobSite.User is not User user)
				{
					throw new InvalidOperationException("Not logged in.");
				}

				var replacementsTitle = new Page(user.Namespace, user.PageName + "/ESO Replacements");
				replacementsTitle.Load();
				var replacements = replacementsTitle.Text;
				if (string.IsNullOrEmpty(replacements))
				{
					throw new InvalidOperationException("Replacements page not found or empty!");
				}

				var replaceFirst = TableFinder.Match(replacements);
				var replaceAll = replaceFirst.NextMatch();

				if (!replaceFirst.Success || !replaceAll.Success)
				{
					throw new InvalidOperationException("Tables are missing on the replacements page!");
				}

				GetMatches(jobSite, replaceFirst.Value, ReplaceFirstList);
				GetMatches(jobSite, replaceAll.Value, ReplaceAllList);
				initialized = true;
			}
		}

		public static void ReplaceEsoLinks(Site site, NodeCollection nodes)
		{
			// Iterating manually rather than with NodeCollection methods, since the list is being altered as we go and I'm not sure how foreach would deal with that in this situation.
			var factory = nodes.Factory;
			for (var i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] is ITextNode textNode)
				{
					// TODO: This used to just be fugly, it's now a disaster. Rewrite.

					// This is a truly fugly hack of a text modification, but is necessary until such time as Nowrap/Huh insertion can handle this on their own. The logic is to check if the first match is at the beginning of the text and, if so, and the previous value is a Huh or Nowrap template, then integrate the text of that into this node and remove the template from the collection. After that's done, we proceed as normal.
					var text = textNode.Text;
					if (i > 0 &&
						nodes[i - 1] is ITemplateNode previous &&
						previous.GetTitleText() is string title &&
						(title.Equals("huh", StringComparison.OrdinalIgnoreCase) || title.Equals("nowrap", StringComparison.OrdinalIgnoreCase)))
					{
						text = WikiTextVisitor.Raw(previous) + text;
						var boldStart = false;
						ITextNode? backText = null;
						if (i > 1)
						{
							backText = nodes[i - 2] as ITextNode;
							if (backText != null)
							{
								boldStart = backText.Text.EndsWith("'''", StringComparison.Ordinal);
								if (boldStart)
								{
									text = "'''" + text;
								}
							}
						}

						var firstMatch = EsoLinks.Match(text);
						if (firstMatch.Success && firstMatch.Index == 0)
						{
							if (boldStart && backText != null)
							{
								backText.Text = backText.Text[0..^3];
							}

							nodes.Remove(previous);
							i--;
						}
						else
						{
							text = textNode.Text;
						}
					}

					var matches = (ICollection<Match>)EsoLinks.Matches(text);
					if (matches.Count > 0)
					{
						var newNodes = factory.NodeCollection();
						var startPos = 0;
						foreach (var match in matches)
						{
							if (match.Index > startPos)
							{
								newNodes.Add(factory.TextNode(text[startPos..match.Index]));
							}

							newNodes.Add(ReplaceTemplatableText(match, site));
							startPos = match.Index + match.Length;
						}

						nodes.InsertRange(i, newNodes);
						i += newNodes.Count;
						if (startPos == text.Length)
						{
							nodes.RemoveAt(i);
							i--;
						}
						else
						{
							textNode.Text = text[startPos..];
						}
					}
				}
			}
		}

		public static void ReplaceFirstLink(NodeCollection nodes, TitleCollection usedList)
		{
			ThrowNull(nodes, nameof(nodes));
			for (var i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] is ITextNode textNode && ReplaceLink(nodes.Factory, textNode.Text, usedList) is NodeCollection newNodes)
				{
					nodes.RemoveAt(i);
					nodes.InsertRange(i, newNodes);
					i += newNodes.Count - 1;
				}
			}
		}

		public static void ReplaceGlobal(NodeCollection nodes)
		{
			// We only look at the top level...anything below that represents a replacement and should not be re-evaluated.
			var factory = nodes.Factory;
			var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
			for (var i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] is ITextNode textNode)
				{
					var text = textNode.Text;
					var newNodes = factory.NodeCollection();
					var startPos = 0;
					for (var currentPos = 0; currentPos < text.Length; currentPos++)
					{
						foreach (var replacement in ReplaceAllList)
						{
							var fromLength = replacement.From.Length;
							if (((currentPos + fromLength) < text.Length) && compareInfo.Compare(text, currentPos, fromLength, replacement.From, 0, fromLength) == 0)
							{
								UnreplacedList.Remove(replacement.From);
								if (currentPos > startPos)
								{
									newNodes.Add(factory.TextNode(text[startPos..currentPos]));
								}

								foreach (var node in replacement.To)
								{
									newNodes.Add(node);
								}

								startPos = currentPos + fromLength;
								currentPos = startPos - 1; // Because the loop will increment it.
								break;
							}
						}
					}

					if (newNodes.Count > 0)
					{
						nodes.InsertRange(i, newNodes);
						i += newNodes.Count;
						if (startPos == text.Length)
						{
							nodes.RemoveAt(i);
						}
						else
						{
							textNode.Text = textNode.Text[startPos..];
						}
					}
				}
			}
		}

		public static void ReplaceSkillLinks(NodeCollection nodes, string skillName)
		{
			foreach (var textNode in nodes.FindAll<ITextNode>())
			{
				foreach (var synergy in ReplacementData.Synergies)
				{
					if (string.Equals(skillName, synergy.Skill, StringComparison.Ordinal))
					{
						textNode.Text = textNode.Text.Replace(synergy.Text, synergy.SynergyLink, StringComparison.Ordinal);
					}
				}
			}
		}

		public static void ShowUnreplaced()
		{
			Debug.WriteLine("Replacements from wiki that are no longer used:");
			foreach (var replacement in UnreplacedList)
			{
				Debug.WriteLine("  " + replacement);
			}

			Debug.WriteLine("*** Remember to run all of the following, taking results only from the last one, before deleting any unused replacements: Active skills, Passive skills, Item Sets ***");
		}
		#endregion

		#region Public Methods

		public ICollection<ISimpleTitle> CheckNewLinks(ContextualParser oldPage, ContextualParser newPage)
		{
			var oldLinks = new HashSet<ISimpleTitle>(SimpleTitleEqualityComparer.Instance);
			foreach (var node in oldPage.Nodes.FindAll<ILinkNode>(null, false, true, 0))
			{
				var siteLink = SiteLink.FromLinkNode(this.site, node);
				oldLinks.Add(siteLink);
			}

			foreach (var node in newPage.Nodes.FindAll<ILinkNode>(null, false, true, 0))
			{
				var siteLink = SiteLink.FromLinkNode(this.site, node);
				oldLinks.Remove(siteLink);
			}

			return oldLinks;
		}

		public ICollection<ISimpleTitle> CheckNewTemplates(ContextualParser oldPage, ContextualParser newPage)
		{
			var oldTemplates = new HashSet<ISimpleTitle>(SimpleTitleEqualityComparer.Instance);
			foreach (var node in oldPage.Nodes.FindAll<ITemplateNode>(null, false, true, 0))
			{
				oldTemplates.Add(Title.FromBacklinkNode(this.site, node));
			}

			foreach (var node in newPage.Nodes.FindAll<ITemplateNode>(null, false, true, 0))
			{
				oldTemplates.Remove(Title.FromBacklinkNode(this.site, node));
			}

			return oldTemplates;
		}

		public bool IsNonTrivialChange(ContextualParser oldPage, ContextualParser newPage)
		{
			var oldText = this.StrippedTextFromNodes(oldPage.Nodes);
			var newText = this.StrippedTextFromNodes(newPage.Nodes);
			return string.Compare(oldText, newText, StringComparison.InvariantCultureIgnoreCase) != 0;
		}

		public void RemoveTrivialTemplates(NodeCollection oldNodes) =>
			oldNodes.RemoveAll<ITemplateNode>(node => this.RemoveableTemplates.Contains(Title.FromBacklinkNode(this.site, node)));
		#endregion

		#region Private Static Methods
		private static void GetMatches(Site site, string tableText, List<EsoReplacement> list)
		{
			var matches = (IEnumerable<Match>)ReplacementFinder.Matches(tableText);
			var factory = new SiteNodeFactory(site);
			foreach (var match in matches)
			{
				var from = match.Groups["from"].Value;
				var to = match.Groups["to"].Value;
				if (to.Length == 0)
				{
					to = "[[Online:" + from + "|" + from + "]]";
				}

				list.Add(new EsoReplacement(from, factory.Parse(to)));
				UnreplacedList.Add(from);
			}

			list.Sort((x, y) =>
				x.From.Length == y.From.Length ? 0 :
				x.From.Length < y.From.Length ? 1 :
				-1);
		}

		private static NodeCollection? ReplaceLink(IWikiNodeFactory factory, string text, TitleCollection usedList)
		{
			ThrowNull(factory, nameof(factory));
			ThrowNull(usedList, nameof(usedList));
			var foundReplacements = new HashSet<string>(StringComparer.Ordinal);
			var textLength = text.Length;
			var retval = factory.NodeCollection();
			var start = 0;
			for (var i = 0; i < textLength; i++)
			{
				var newText = text[i..];
				foreach (var replacement in ReplaceFirstList)
				{
					if (newText.StartsWith(replacement.From, StringComparison.Ordinal))
					{
						if (i != start)
						{
							retval.Add(factory.TextNode(text[start..i]));
						}

						foreach (var newNode in replacement.To)
						{
							if (newNode is ILinkNode link)
							{
								var title = SiteLink.FromLinkNode(usedList.Site, link);
								if (usedList.Contains(title) && link.Parameters.Count > 0 && link.Parameters[0].Value is NodeCollection valueNode)
								{
									retval.AddRange(valueNode);
								}
								else
								{
									retval.Add(link);
									usedList.Add(title);
								}
							}
							else
							{
								retval.Add(newNode);
							}
						}

						foundReplacements.Add(replacement.From);
						UnreplacedList.Remove(replacement.From);
						i += replacement.From.Length - 1;
						start = i + 1;
						break;
					}
				}
			}

			if (start < text.Length)
			{
				retval.Add(factory.TextNode(text[start..]));
			}

			return retval;
		}

		private static IWikiNode ReplaceTemplatableText(Match match, Site site)
		{
			var type = match.Groups["type"].Value.UpperFirst(CultureInfo.InvariantCulture);
			var resistType = type.Split(ResistanceSplit, StringSplitOptions.None);
			var factory = new SiteNodeFactory(site);
			var templateNode = resistType.Length > 1
				? factory.TemplateNodeFromParts("ESO Resistance Link", (null, resistType[0]))
				: factory.TemplateNodeFromParts("ESO " + type + " Link");

			var beforeSuccess = false;
			if (match.Groups["before"] is Group before)
			{
				beforeSuccess = before.Success;
				if (beforeSuccess)
				{
					var value = before.Value.Trim().Replace("'''", string.Empty, StringComparison.Ordinal);
					if (value.Length == 0)
					{
						// Before is seeing success even with nothing and with both individual parts false. Not sure what that's about, but compensating for it here.
						beforeSuccess = false;
					}
					else
					{
						templateNode.Add(value);
					}
				}
			}

			if (match.Groups["after"] is Group after && after.Success)
			{
				if (!beforeSuccess)
				{
					// Because these are anonymous parameters, we must always add the before value, even if empty.
					templateNode.Add(string.Empty);
				}

				templateNode.Add(after.Value.Trim().Replace("'''", string.Empty, StringComparison.Ordinal));
			}

			return templateNode;
		}
		#endregion

		#region Private Methods
		private string StrippedTextFromNodes(NodeCollection nodes)
		{
			this.RemoveTrivialTemplates(nodes);
			var retval = nodes.ToRaw();
			retval = NumberStripper.Replace(retval, string.Empty);
			return SpaceStripper.Replace(retval, string.Empty);
		}
		#endregion
	}
}