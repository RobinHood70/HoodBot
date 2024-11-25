namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;
using static RobinHood70.WikiCommon.Searches;

internal sealed class UespReplacer
{
	#region Static Fields
	private static readonly Regex EsoLinks = new(@"(?<before>(((''')?([0-9]+(-[0-9]+)?|\{\{huh\}\}|\{\{Nowrap[^}]*\}\})(''')?)%?\s+)?(((or )?more|max(imum)?|of missing|ESO)(\s+|<br>))?)?(?<type>(?-i:Health|Magicka|Physical Penetration|Physical Resistance|Spell Critical|Spell Damage|Spell Penetration|Spell Resistance|Stamina|Ultimate|Weapon Critical|Weapon Damage))(\s(?<after>(Recovery|Regeneration|[0-9]+%)+))?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	private static readonly Regex NumberStripper = new(@"[0-9]+(-[0-9]+)?%?\s*", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	private static readonly Regex ReplacementFinder = new(@"^\|\ *(<nowiki/?>)?(?<from>.*?)(</?nowiki/?>)?\ *\|\|\ *(<nowiki/?>)?(?<to>.*?)(</?nowiki/?>)?\ *$", RegexOptions.Multiline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	private static readonly Regex SpaceStripper = new(@"(\s{2,}|\n)", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	private static readonly List<EsoReplacement> ReplaceAllList = [];
	private static readonly List<EsoReplacement> ReplaceFirstList = [];
	private static readonly ICollection<string> UnreplacedList = new SortedSet<string>(StringComparer.Ordinal);
	private static readonly string[] ResistanceSplit = [" Resistance"];

	private static bool initialized;
	#endregion

	#region Fields
	private readonly WikiNodeCollection oldNodes;
	private readonly WikiNodeCollection newNodes;
	#endregion

	#region Constructors
	public UespReplacer(Site site, WikiNodeCollection oldNodes, WikiNodeCollection newNodes)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(oldNodes);
		ArgumentNullException.ThrowIfNull(newNodes);
		this.Site = site;
		this.oldNodes = oldNodes.Clone();
		this.oldNodes.RemoveAll<IIgnoreNode>();
		this.newNodes = newNodes.Clone();
		this.newNodes.RemoveAll<IIgnoreNode>();
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

	public Site Site { get; }
	#endregion

	#region Public Static Methods
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

			var replacements = jobSite.LoadPageText(user.Title, "/ESO Replacements");
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

			GetMatches(replaceFirst.Value, ReplaceFirstList);
			GetMatches(replaceAll.Value, ReplaceAllList);
			initialized = true;
		}
	}

	public static void ReplaceEsoLinks(Site site, WikiNodeCollection nodes)
	{
		// Iterating manually rather than with WikiNodeCollection methods, since the list is being altered as we go.
		var searchTitles = new TitleCollection(site, "Template:Huh", "Template:Nowrap");
		for (var i = 0; i < nodes.Count; i++)
		{
			if (nodes[i] is ITextNode textNode)
			{
				// TODO: This used to just be fugly, it's now a disaster. Rewrite.

				// This is a truly fugly hack of a text modification, but is necessary until such time as Nowrap/Huh insertion can handle this on their own. The logic is to check if the first match is at the beginning of the text and, if so, and the previous value is a Huh or Nowrap template, then integrate the text of that into this node and remove the template from the collection. After that's done, we proceed as normal.
				var text = textNode.Text;
				if (i > 0 &&
					nodes[i - 1] is ITemplateNode previous &&
					searchTitles.Contains(previous.GetTitle(site)))
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
					WikiNodeCollection replacementNodes = new(nodes.Factory);
					var startPos = 0;
					foreach (var match in matches)
					{
						if (match.Index > startPos)
						{
							replacementNodes.AddText(text[startPos..match.Index]);
						}

						replacementNodes.Add(ReplaceTemplatableText(site, match, nodes.Factory));
						startPos = match.Index + match.Length;
					}

					nodes.InsertRange(i, replacementNodes);
					i += replacementNodes.Count;
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

	public static void ReplaceFirstLink(WikiNodeCollection nodes, TitleCollection usedList)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(usedList);
		for (var i = 0; i < nodes.Count; i++)
		{
			if (nodes[i] is ITextNode textNode && ReplaceLink(nodes.Factory, textNode.Text, usedList) is WikiNodeCollection linkNodes)
			{
				nodes.RemoveAt(i);
				nodes.InsertRange(i, linkNodes);
				i += linkNodes.Count - 1;
			}
		}
	}

	public static void ReplaceGlobal(WikiNodeCollection nodes)
	{
		// We only look at the top level...anything below that represents a replacement and should not be re-evaluated.
		var factory = nodes.Factory;
		var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
		for (var i = 0; i < nodes.Count; i++)
		{
			if (nodes[i] is ITextNode textNode)
			{
				var text = textNode.Text;
				WikiNodeCollection replacementNodes = new(factory);
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
								replacementNodes.AddText(text[startPos..currentPos]);
							}

							foreach (var node in replacement.To)
							{
								replacementNodes.Add(node);
							}

							startPos = currentPos + fromLength;
							currentPos = startPos - 1; // Because the loop will increment it.
							break;
						}
					}
				}

				if (replacementNodes.Count > 0)
				{
					nodes.InsertRange(i, replacementNodes);
					i += replacementNodes.Count;
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

	public static void ReplaceSkillLinks(WikiNodeCollection nodes, string skillName)
	{
		foreach (var textNode in nodes.FindAll<ITextNode>())
		{
			foreach (var synergy in ReplacementData.Synergies)
			{
				if (skillName.OrdinalEquals(synergy.Skill))
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

		Debug.WriteLine("*** Remember to run both the Sets and Skills jobs before deleting any unused replacements ***");
	}
	#endregion

	#region Public Methods

	public ICollection<Title> CheckNewLinks()
	{
		HashSet<Title> oldLinks = [];
		foreach (var node in this.oldNodes.FindAll<ILinkNode>(null, false, true, 0))
		{
			var siteLink = SiteLink.FromLinkNode(this.Site, node);
			oldLinks.Add(siteLink.Title);
		}

		foreach (var node in this.newNodes.FindAll<ILinkNode>(null, false, true, 0))
		{
			var siteLink = SiteLink.FromLinkNode(this.Site, node);
			oldLinks.Remove(siteLink.Title);
		}

		return oldLinks;
	}

	public ICollection<Title> CheckNewTemplates()
	{
		HashSet<Title> oldTemplates = [];
		foreach (var node in this.oldNodes.FindAll<ITemplateNode>(null, false, true, 0))
		{
			oldTemplates.Add(TitleFactory.FromBacklinkNode(this.Site, node));
		}

		foreach (var node in this.newNodes.FindAll<ITemplateNode>(null, false, true, 0))
		{
			oldTemplates.Remove(TitleFactory.FromBacklinkNode(this.Site, node));
		}

		// Always ignore these
		oldTemplates.Remove(TitleFactory.FromUnvalidated(this.Site, "Huh"));

		return oldTemplates;
	}

	public IEnumerable<string> Compare(string location)
	{
		var newLinks = this.CheckNewLinks();
		if (newLinks.Count > 0)
		{
			yield return this.ConstructWarning(location, newLinks, "links");
		}

		var newTemplates = this.CheckNewTemplates();
		if (newTemplates.Count > 0)
		{
			yield return this.ConstructWarning(location, newTemplates, "templates");
		}
	}

	public bool IsNonTrivialChange()
	{
		var oldText = this.StrippedTextFromNodes(this.oldNodes);
		var newText = this.StrippedTextFromNodes(this.newNodes);
		return !oldText.OrdinalICEquals(newText);
	}
	#endregion

	#region Private Static Methods
	private static void GetMatches(string tableText, List<EsoReplacement> list)
	{
		IEnumerable<Match> matches = ReplacementFinder.Matches(tableText);
		var factory = new WikiNodeFactory();
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

		// Sort by longest first for most accurate matches.
		list.Sort((x, y) => y.From.Length.CompareTo(x.From.Length));
	}

	private static WikiNodeCollection? ReplaceLink(IWikiNodeFactory factory, string text, TitleCollection usedList)
	{
		HashSet<string> foundReplacements = new(StringComparer.Ordinal);
		var textLength = text.Length;
		WikiNodeCollection retval = new(factory);
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
						// Do NOT change this to AddText or it'll mess things up. Needs more investigation as to why if ever I feel like it..
						retval.Add(factory.TextNode(text[start..i]));
					}

					foreach (var newNode in replacement.To)
					{
						if (newNode is ILinkNode link)
						{
							var siteLink = SiteLink.FromLinkNode(usedList.Site, link);
							if (usedList.Contains(siteLink.Title) && link.Parameters.Count > 0 && link.Parameters[0].Value is WikiNodeCollection valueNode)
							{
								retval.AddRange(valueNode);
							}
							else
							{
								retval.Add(link);
								usedList.Add(siteLink.Title);
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

	private static IWikiNode ReplaceTemplatableText(Site site, Match match, IWikiNodeFactory factory)
	{
		var type = match.Groups["type"].Value.UpperFirst(site.Culture);
		var resistType = type.Split(ResistanceSplit, StringSplitOptions.None);
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
	private string ConstructWarning(string location, IEnumerable<Title> titles, string warningType)
	{
		ArgumentNullException.ThrowIfNull(titles);
		ArgumentNullException.ThrowIfNull(warningType);
		var oldText = this.oldNodes.ToRaw().Trim();
		var newText = this.newNodes.ToRaw().Trim();
		var warning = new StringBuilder()
			.Append("Watch for ")
			.Append(warningType)
			.Append(" on ")
			.AppendLine(location)
			.Append(warningType.UpperFirst(CultureInfo.CurrentCulture))
			.Append(": ");
		foreach (var link in titles)
		{
			warning
				.Append(link)
				.Append(", ");
		}

		warning
			.Remove(warning.Length - 2, 2)
			.AppendLine()
			.AppendLine("Old Text:")
			.AppendLine(oldText)
			.AppendLine("New Text:")
			.AppendLine(newText)
			.AppendLine();
		return warning.ToString();
	}

	private void RemoveTrivialTemplates(WikiNodeCollection nodes)
	{
		bool IsRemovable(ITemplateNode node) => this.RemoveableTemplates.Contains(TitleFactory.FromBacklinkNode(this.Site, node));

		nodes.RemoveAll<ITemplateNode>(IsRemovable);
	}

	private string StrippedTextFromNodes(WikiNodeCollection nodes)
	{
		var onlyNodes = nodes.Clone();
		this.RemoveTrivialTemplates(onlyNodes);
		var retval = onlyNodes.ToRaw();
		retval = NumberStripper.Replace(retval, string.Empty);
		return SpaceStripper.Replace(retval, string.Empty);
	}
	#endregion
}