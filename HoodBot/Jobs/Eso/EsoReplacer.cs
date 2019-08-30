namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiClasses.Searches;

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

		private static readonly string[] OnlineSplit = new string[] { "Online:" };
		private static readonly string[] ResistanceSplit = new string[] { " Resistance" };

		private static bool initialized = false;
		#endregion

		#region Public Methods
		public static bool CompareReplacementText(WikiJob job, string oldText, string newText, string pageName)
		{
			oldText = ToPlainText(oldText);
			var replacedText = TemplateStripper.Replace(oldText, string.Empty);
			if (replacedText.Contains("[["))
			{
				job.Warn($"\nWatch for links: {pageName}\nCurrent Text: {replacedText}\nNew Text: {newText}");
			}

			if (replacedText.Contains("{{"))
			{
				job.Warn($"\nWatch for templates: {pageName}\nCurrent Text: {replacedText}\nNew Text: {newText}");
			}

			return oldText != ToPlainText(newText);
		}

		public static string FirstLinksOnly(Site site, string text)
		{
			var uniqueLinks = new HashSet<string>();
			var linkFinder = SiteLink.Find();
			return linkFinder.Replace(text, (match) => LinkReplacer(match, site, uniqueLinks));
		}

		public static void Initialize(WikiJob job)
		{
			if (!initialized)
			{
				job.StatusWriteLine("Parsing replacements");
				var replacements = job.Site.LoadPageText(job.Site.User.FullPageName + "/ESO Replacements");
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

		public static string ReplaceGlobal(string text, string skillName)
		{
			foreach (var replacement in ReplaceAllList)
			{
				var newText = new StringBuilder(text.Length);
				var lastOffset = 0;
				var offset = text.IndexOf(replacement.From, StringComparison.Ordinal);
				while (offset >= 0)
				{
					newText.Append(text.Substring(lastOffset, offset - lastOffset));
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

			if (skillName != null)
			{
				foreach (var synergy in ReplacementData.Synergies)
				{
					if (skillName == synergy.Skill)
					{
						text = text.Replace(synergy.Text, synergy.SynergyLink);
					}
				}
			}

			return text;
		}

		public static string ReplaceLink(string text)
		{
			foreach (var replacement in ReplaceFirstList)
			{
				if (text.Contains(replacement.From))
				{
					UnreplacedList.Remove(replacement.From);
					text = replacement.ReplaceFirst(text);
				}
			}

			return text;
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
			text = text.Replace("[[ON:", "[[Online:");
			foreach (var replacement in ReplaceAllList)
			{
				text = text.Replace(replacement.To, replacement.From);
			}

			foreach (var replacement in ReplaceFirstList)
			{
				text = text.Replace(replacement.To, replacement.From);
				var lcReplace = replacement.To.Split(OnlineSplit, 2, StringSplitOptions.None);
				if (lcReplace.Length == 2)
				{
					text = text.Replace(lcReplace[0] + "Online:" + lcReplace[1].LowerFirst(CultureInfo.InvariantCulture), replacement.From);
				}

				lcReplace = replacement.To.Split(TextArrays.TemplateTerminator, 2, StringSplitOptions.None);
				if (lcReplace.Length == 2)
				{
					text = text.Replace(lcReplace[0] + "{{" + lcReplace[1].LowerFirst(CultureInfo.InvariantCulture), replacement.From);
				}
			}

			text = TextStripper.Replace(text, string.Empty);
			text = text.Replace("  ", " ").Trim();

			return text;
		}
		#endregion

		#region Private Methods
		private static void GetMatches(string tableText, List<EsoReplacement> list)
		{
			foreach (Match match in ReplacementFinder.Matches(tableText))
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

		private static string LinkReplacer(Match match, Site site, HashSet<string> uniqueLinks)
		{
			var link = new SiteLink(site, match.Value);
			if (!uniqueLinks.Contains(link.FullPageName))
			{
				uniqueLinks.Add(link.FullPageName);
				return match.Value;
			}

			return link.DisplayParameter;
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
						return "free";
					default:
						// Because these are anonymous parameters, we must always add the before value, even if empty.
						linkTemplate.AddAnonymous(before.Value.Replace("'''", string.Empty));

						if (after.Success)
						{
							linkTemplate.AddAnonymous(after.Value.Replace("'''", string.Empty));
						}

						break;
				}
			}

			return linkTemplate.ToString();
		}
		#endregion
	}
}