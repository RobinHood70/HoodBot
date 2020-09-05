﻿namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WikiCommon.Searches;

	internal static class EsoReplacer
	{
		#region Fields
		private static readonly Regex EsoLinks = new Regex(@"((?<before>(((''')?[0-9]+(-[0-9]+)?(''')?%?)(\smore|\smax(imum)?|\sof missing|\{\{huh}}|<br>)?|(''')?\{\{Nowrap[^}]*?}}(''')?|max(imum)?|ESO)+)\s)?(?<type>(?-i:Health|Magicka|Physical Penetration|Physical Resistance|Spell Critical|Spell Damage|Spell Penetration|Spell Resistance|Stamina|Ultimate|Weapon Critical|Weapon Damage))(\s(?<after>(Recovery|Regeneration|[0-9]+%)+))?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex ReplacementFinder = new Regex(@"^\|\ *(<nowiki/?>)?(?<from>.*?)(</?nowiki/?>)?\ *\|\|\ *(<nowiki/?>)?(?<to>.*?)(</?nowiki/?>)?\ *$", RegexOptions.Multiline);
		private static readonly Regex TemplateStripper = new Regex(@"{{\s*(ESO Quality Color|Nowrap|ESO (Health|Magicka|MagStam|Physical Penetration|Resistance|Spell Critical|Spell Damage|Spell Penetration|Stamina|Synergy|Ultimate|Weapon Critical|Weapon Damage) Link).*?}}");
		private static readonly Regex TextStripper = new Regex(@"({{huh}}|[0-9]+(-[0-9]+)?%?)");

		private static readonly List<EsoReplacement> ReplaceAllList = new List<EsoReplacement>();
		private static readonly List<EsoReplacement> ReplaceFirstList = new List<EsoReplacement>();
		private static readonly ICollection<string> UnreplacedList = new SortedSet<string>();

		private static readonly string[] OnlineSplit = new[] { "Online:" };
		private static readonly string[] ResistanceSplit = new[] { " Resistance" };

		private static bool initialized;
		#endregion

		#region Public Methods
		public static bool CompareReplacementText(WikiJob job, string oldText, string newText, string pageName)
		{
			oldText = ToPlainText(oldText);
			var replacedText = TemplateStripper.Replace(oldText, string.Empty);
			if (replacedText.Contains("[[", StringComparison.Ordinal))
			{
				job.Warn($"\nWatch for links: {pageName}\nCurrent Text: {replacedText}\nNew Text: {newText}");
			}

			if (replacedText.Contains("{{", StringComparison.Ordinal))
			{
				job.Warn($"\nWatch for templates: {pageName}\nCurrent Text: {replacedText}\nNew Text: {newText}");
			}

			return oldText != ToPlainText(newText);
		}

		public static void Initialize(WikiJob job)
		{
			if (!initialized)
			{
				job.StatusWriteLine("Parsing replacements");
				if (job.Site.User == null)
				{
					throw new InvalidOperationException("Not logged in.");
				}

				var user = job.Site.User;
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

				GetMatches(replaceFirst.Value, ReplaceFirstList);
				GetMatches(replaceAll.Value, ReplaceAllList);
				initialized = true;
			}
		}

		public static string ReplaceGlobal(string text, string? skillName)
		{
			foreach (var replacement in ReplaceAllList)
			{
				var newText = new StringBuilder(text.Length);
				var lastOffset = 0;
				var offset = text.IndexOf(replacement.From, StringComparison.Ordinal);
				while (offset >= 0)
				{
					newText.Append(text[lastOffset..offset]);
					var inLinkLeft = text.LastIndexOf("[[", offset, StringComparison.Ordinal);
					var inLinkRight = text.LastIndexOf("]]", offset, StringComparison.Ordinal);

					// Will only be equal if both are -1, meaning not found, in which case we also want to do the replacement.
					if (inLinkLeft >= inLinkRight)
					{
						newText.Append(replacement.To);
						UnreplacedList.Remove(replacement.From);
					}
					else
					{
						newText.Append(replacement.From);
					}

					lastOffset = offset + replacement.From.Length;
					offset = text.IndexOf(replacement.From, lastOffset, StringComparison.Ordinal);
				}

				newText.Append(text.Substring(lastOffset));

				text = newText.ToString();
			}

			text = EsoLinks.Replace(text, ReplaceTemplate);
			text = text.Replace("<nowiki/>", string.Empty, StringComparison.Ordinal);

			if (skillName != null)
			{
				foreach (var synergy in ReplacementData.Synergies)
				{
					if (skillName == synergy.Skill)
					{
						text = text.Replace(synergy.Text, synergy.SynergyLink, StringComparison.Ordinal);
					}
				}
			}

			return text;
		}

		public static string ReplaceFirstLink(string text, TitleCollection usedList)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			var parsedText = WikiTextParser.Parse(text);
			var currentNode = parsedText.First;
			while (currentNode != null)
			{
				if (ReplaceLink(currentNode, usedList) is NodeCollection newNodes)
				{
					foreach (var colNode in newNodes)
					{
						parsedText.AddBefore(currentNode, colNode);
					}

					currentNode = currentNode.Previous!;
					parsedText.Remove(currentNode.Next!);
				}
				else
				{
					currentNode = currentNode.Next;
				}
			}

			return WikiTextVisitor.Raw(parsedText);
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

		public static string ToPlainText(string text)
		{
			text = text.Replace("[[ON:", "[[Online:", StringComparison.OrdinalIgnoreCase);
			foreach (var replacement in ReplaceAllList)
			{
				text = text.Replace(replacement.To, replacement.From, StringComparison.Ordinal);
			}

			foreach (var replacement in ReplaceFirstList)
			{
				text = text.Replace(replacement.To, replacement.From, StringComparison.Ordinal);
				var lcReplace = replacement.To.Split(OnlineSplit, 2, StringSplitOptions.None);
				if (lcReplace.Length == 2)
				{
					text = text.Replace(lcReplace[0] + "Online:" + lcReplace[1].LowerFirst(CultureInfo.InvariantCulture), replacement.From, StringComparison.Ordinal);
				}

				lcReplace = replacement.To.Split(TextArrays.TemplateTerminator, 2, StringSplitOptions.None);
				if (lcReplace.Length == 2)
				{
					text = text.Replace(lcReplace[0] + "{{" + lcReplace[1].LowerFirst(CultureInfo.InvariantCulture), replacement.From, StringComparison.Ordinal);
				}
			}

			text = TextStripper.Replace(text, string.Empty);
			text = text.Replace("  ", " ", StringComparison.Ordinal).Trim();

			return text;
		}
		#endregion

		#region Private Methods
		private static void GetMatches(string tableText, List<EsoReplacement> list)
		{
			var matches = (IEnumerable<Match>)ReplacementFinder.Matches(tableText);
			foreach (var match in matches)
			{
				var from = match.Groups["from"].Value;
				var to = match.Groups["to"].Value;
				if (to.Length == 0)
				{
					to = "[[Online:" + from + "|" + from + "]]";
				}

				list.Add(new EsoReplacement(from, to));
				UnreplacedList.Add(from);
			}

			list.Sort((x, y) =>
				x.From.Length == y.From.Length ? 0 :
				x.From.Length < y.From.Length ? 1 :
				-1);
		}

		private static NodeCollection? ReplaceLink(LinkedListNode<IWikiNode> node, TitleCollection usedList)
		{
			if (!(node?.Value is TextNode textNode))
			{
				return null;
			}

			ThrowNull(usedList, nameof(usedList));
			var site = usedList.Site;
			var foundReplacements = new HashSet<string>(StringComparer.Ordinal);
			var newText = textNode.Text;
			var textLength = textNode.Text.Length;
			for (var i = 0; i < textLength; i++)
			{
				foreach (var replacement in ReplaceFirstList)
				{
					if (newText.StartsWith(replacement.From, StringComparison.Ordinal))
					{
						var retval = new NodeCollection(null);
						if (i != 0)
						{
							retval.AddLast(new TextNode(textNode.Text.Substring(0, i)));
						}

						foreach (var newNode in replacement.ToNodes)
						{
							if (newNode is LinkNode link)
							{
								var title = SiteLink.FromLinkNode(site, link);
								if (usedList.Contains(title) && link.Parameters.Count == 1)
								{
									retval.AddRange(link.Parameters[0].Value);
								}
								else
								{
									retval.AddLast(link);
									usedList.Add(title);
								}
							}
							else
							{
								retval.AddLast(newNode);
							}
						}

						var len = replacement.From.Length;
						if (len < textNode.Text.Length)
						{
							retval.AddLast(new TextNode(newText.Substring(len)));
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

		private static string ReplaceTemplate(Match match)
		{
			var before = match.Groups["before"];
			var after = match.Groups["after"];
			var type = match.Groups["type"].Value.UpperFirst(CultureInfo.InvariantCulture);
			Template linkTemplate;
			var resistType = type.Split(ResistanceSplit, StringSplitOptions.None);
			if (resistType.Length > 1)
			{
				linkTemplate = new Template("ESO Resistance Link");
				linkTemplate.AddAnonymous(resistType[0]);
			}
			else
			{
				linkTemplate = new Template("ESO " + type + " Link");
			}

			if (before.Success || after.Success)
			{
				switch (before.Value)
				{
					case "ESO":
						return match.Value;
					case "0":
						return "Free";
					default:
						// Because these are anonymous parameters, we must always add the before value, even if empty.
						linkTemplate.AddAnonymous(before.Value.Replace("'''", string.Empty, StringComparison.Ordinal));

						if (after.Success)
						{
							linkTemplate.AddAnonymous(after.Value.Replace("'''", string.Empty, StringComparison.Ordinal));
						}

						break;
				}
			}

			return linkTemplate.ToString();
		}
		#endregion
	}
}