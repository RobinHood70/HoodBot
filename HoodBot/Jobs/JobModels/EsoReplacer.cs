namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
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
		private static readonly Regex EsoLinks = new Regex(@"(?<before>(((''')?([0-9]+(-[0-9]+)?|\{\{huh}}|\{\{Nowrap[^}]*?}})(''')?)%?\s+)?(((or )?more|max(imum)?|of missing|ESO)(\s+|<br>))?)?(?<type>(?-i:Health|Magicka|Physical Penetration|Physical Resistance|Spell Critical|Spell Damage|Spell Penetration|Spell Resistance|Stamina|Ultimate|Weapon Critical|Weapon Damage))(\s(?<after>(Recovery|Regeneration|[0-9]+%)+))?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex NumberStripper = new Regex(@"[0-9]+(-[0-9]+)?%?\s*", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex ReplacementFinder = new Regex(@"^\|\ *(<nowiki/?>)?(?<from>.*?)(</?nowiki/?>)?\ *\|\|\ *(<nowiki/?>)?(?<to>.*?)(</?nowiki/?>)?\ *$", RegexOptions.Multiline | RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex SpaceStripper = new Regex(@"(\s{2,}|\n)", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly List<EsoReplacement> ReplaceAllList = new List<EsoReplacement>();
		private static readonly List<EsoReplacement> ReplaceFirstList = new List<EsoReplacement>();
		private static readonly ICollection<string> UnreplacedList = new SortedSet<string>();
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
		public static void Initialize(WikiJob job)
		{
			if (!initialized)
			{
				var jobSite = job.Site;
				job.StatusWriteLine("Parsing replacements");
				if (!(jobSite.User is User user))
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
					var text = textNode.Text;
					var checkTemplate = EsoLinks.Match(text);
					if (checkTemplate.Success)
					{
						// This is a truly fugly hack of a text modification, but is necessary until such time as Nowrap/Huh insertion can handle this on their own. The logic is to check if the first match is at the beginning of the text and, if so, and the previous value is a Huh or Nowrap template, then integrate the text of that into this node and remove the template from the collection. After that's done, we proceed as normal.
						if (checkTemplate.Index == 1 &&
							nodes[i - 1] is ITemplateNode previous &&
							previous.GetTitleText() is string title &&
							(title.Equals("huh", StringComparison.OrdinalIgnoreCase) || title.Equals("nowrap", StringComparison.OrdinalIgnoreCase)))
						{
							text = WikiTextVisitor.Raw(previous) + text;
							nodes.Remove(previous);
						}

						var matches = (ICollection<Match>)EsoLinks.Matches(text);
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
							textNode.Text = text.Substring(startPos);
						}
					}
				}
			}
		}

		public static string ReplaceFirstLink(NodeCollection nodes, TitleCollection usedList)
		{
			ThrowNull(nodes, nameof(nodes));
			for (var i = 0; i < nodes.Count; i++)
			{
				if (ReplaceLink(nodes.Factory, nodes[i], usedList) is NodeCollection newNodes)
				{
					nodes.RemoveAt(i);
					nodes.InsertRange(i, newNodes);
					i += newNodes.Count - 1;
				}
			}

			return WikiTextVisitor.Raw(nodes);
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
							textNode.Text = textNode.Text.Substring(startPos);
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
			foreach (var node in oldPage.LinkNodes)
			{
				var siteLink = SiteLink.FromLinkNode(this.site, node);
				oldLinks.Add(siteLink);
			}

			foreach (var node in newPage.LinkNodes)
			{
				var siteLink = SiteLink.FromLinkNode(this.site, node);
				oldLinks.Remove(siteLink);
			}

			return oldLinks;
		}

		public ICollection<ISimpleTitle> CheckNewTemplates(ContextualParser oldPage, ContextualParser newPage)
		{
			var oldTemplates = new HashSet<ISimpleTitle>(SimpleTitleEqualityComparer.Instance);
			foreach (var node in oldPage.TemplateNodes)
			{
				oldTemplates.Add(Title.FromBacklinkNode(this.site, node));
			}

			foreach (var node in newPage.TemplateNodes)
			{
				oldTemplates.Remove(Title.FromBacklinkNode(this.site, node));
			}

			return oldTemplates;
		}

		public bool IsNonTrivialChange(ContextualParser oldPage, ContextualParser newPage) => string.Compare(
			this.StrippedTextFromNodes(oldPage.Nodes),
			this.StrippedTextFromNodes(newPage.Nodes),
			StringComparison.InvariantCultureIgnoreCase) == 0;

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

		private static NodeCollection? ReplaceLink(IWikiNodeFactory factory, IWikiNode node, TitleCollection usedList)
		{
			ThrowNull(factory, nameof(factory));
			if (!(node is ITextNode textNode))
			{
				return null;
			}

			ThrowNull(usedList, nameof(usedList));
			var foundReplacements = new HashSet<string>(StringComparer.Ordinal);
			var newText = textNode.Text;
			var textLength = textNode.Text.Length;
			for (var i = 0; i < textLength; i++)
			{
				foreach (var replacement in ReplaceFirstList)
				{
					if (newText.StartsWith(replacement.From, StringComparison.Ordinal))
					{
						var retval = factory.NodeCollection();
						if (i != 0)
						{
							retval.Add(factory.TextNode(textNode.Text.Substring(0, i)));
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

						var len = replacement.From.Length;
						if (len < textNode.Text.Length)
						{
							retval.Add(factory.TextNode(newText.Substring(len)));
						}

						foundReplacements.Add(replacement.From);
						UnreplacedList.Remove(replacement.From);
						return retval;
					}
				}

				newText = newText.Substring(1);
			}

			return null;
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
			var retval = WikiTextVisitor.Raw(nodes);
			retval = NumberStripper.Replace(retval, string.Empty);
			return SpaceStripper.Replace(retval, string.Empty);
		}
		#endregion
	}
}