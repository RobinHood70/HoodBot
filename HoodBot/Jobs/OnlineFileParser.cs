namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Convert to Online File")]
	public class OnlineFileParser(JobManager jobManager) : EditJob(jobManager)
	{
		#region Private Constants
		private const string AchievementsQuery = "SELECT name FROM achievements";
		private const string CollectiblesQuery = "SELECT id, name FROM collectibles";
		#endregion

		#region Static Fields
		private static readonly HashSet<string> LicenseNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"Licensing",
			"{{int:license-header}}"
		};

		private static readonly Dictionary<long, string> NameOverrides = new()
		{
			[248] = "Treasure Hunter (costume)",
			[1155] = "Arachnimunculus",
			[1185] = "Skitters",
			[1395] = "Pumice",
			[1396] = "Shyscales",
			[1413] = "Ice Stalker",
			[1457] = "Totem Eyes",
			[1461] = "Factotum (personality)",
			[1480] = "Factotum (polymorph)",
			[4671] = "Tsunny",
			[4745] = "Totem-Tusk",
			[4746] = "Pudgelocks",
			[4747] = "Shaggy Pelt",
			[4993] = "*** UNUSED 1 ***",
			[5884] = "Treasure Hunter (personality)",
			[6017] = "*** UNUSED 2 ***",
			[6117] = "Honor Guard Jack",
			[6457] = "*** UNUSED 3 ***",
			[6657] = "First Cat's Pride Tattoo (face)",
			[6658] = "First Cat's Pride Tattoo (body)",
			[7864] = "Antiquarian's Eye (memento)",
			[8006] = "Antiquarian's Eye (tool)",
			[9245] = "Bastian Hallix (companion)",
			[9353] = "Mirri Elendis (companion)",
			[9440] = "Bastian Hallix (houseguest)",
			[9441] = "Mirri Elendis (houseguest)",
			[9911] = "Ember (companion)",
			[9912] = "Isobel Veloise (companion)",
			[10434] = "Ember (houseguest)",
			[10435] = "Isobel Veloise (houseguest)",
			[11113] = "Sharp-as-Night (companion)",
			[11390] = "Sharp-as-Night (houseguest)",
			[11197] = "Siggride",
			[11441] = "Nylak",
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

		#region Fields
		private readonly HashSet<string> achievementsLookup = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, long> collectiblesLookup = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Change to template";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			foreach (var row in Database.RunQuery(EsoLog.Connection, AchievementsQuery))
			{
				var name = (string)row["name"];
				this.achievementsLookup.Add(name);
			}

			foreach (var row in Database.RunQuery(EsoLog.Connection, CollectiblesQuery))
			{
				var id = (long)row["id"];
				var name = (string)row["name"];
				if (NameOverrides.TryGetValue(id, out var newName))
				{
					name = newName;
				}

				this.collectiblesLookup.Add(name, id);
			}

			var titles = new TitleCollection(this.Site);
			foreach (var filename in File.ReadLines(@"D:\Data\HoodBot\OriginalFile.txt"))
			{
				if (string.Compare(filename, "ON-", StringComparison.Ordinal) > 0)
				{
					titles.Add(TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.File], filename));
				}
			}

			//// string[] fileNames = ["ON-icon-achievecat-Blackwood.png"];
			this.Pages.GetTitles(titles);
		}

		protected override void PageLoaded(Page page)
		{
			var text = SanitizeText(page.Text);
			this.UpdateText(page, text);
		}
		#endregion

		#region Private Static Methods
		private static string CleanupFileName(string fileName, string pageName)
		{
			var newName = fileName
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
				if (fileSplit[0].OrdinalEquals("icons"))
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

		private static string CreateTemplate(Section summary, List<string> parameters, int infoOffset, string remainder)
		{
			var template = "{{Online File";
			template += parameters.Count switch
			{
				0 => string.Empty,
				1 => '|' + parameters[0],
				_ => "\n|" + string.Join("\n|", parameters) + '\n'
			};

			if (infoOffset != -1)
			{
				var newLine = parameters.Count > 1 ? "\n" : string.Empty;
				template = '\n' + template + newLine + "|nosummary=1";
			}
			else
			{
				summary.Header = null;
				summary.Content.Clear();
			}

			template = (template + "}}\n\n" + remainder).TrimEnd();
			return template;
		}

		private static Section? FindSummary(SiteParser parser, SectionCollection sections)
		{
			var summarySections = new List<Section>(sections.FindAll(SummaryNames));
			if (summarySections.Count > 1)
			{
				Debug.WriteLine("Multiple summaries on " + parser.Title.FullPageName());
			}

			return summarySections.Count == 0 ? null : summarySections[^1];
		}

		private static List<string> GetFilenames(SiteParser parser, List<string> parameters, string original)
		{
			var filenames = new List<string>();
			var remainder = new List<string>();
			var split = original.Split(TextArrays.NewLineChars);
			foreach (var line in split)
			{
				var index2 = line.IndexOf("Original file:", StringComparison.OrdinalIgnoreCase);
				if (index2 > -1)
				{
					var fileNameSplit = line[(index2 + 14)..].ToLowerInvariant().Split(',');
					foreach (var fileName in fileNameSplit)
					{
						var trimmedName = fileName.Trim();
						var newName = CleanupFileName(trimmedName, parser.Title.PageName);
						var uri = new Uri("https://esoicons.uesp.net/" + newName + ".png");
						if (!parser.Page.Site.AbstractionLayer.UriExists(uri))
						{
							newName = trimmedName;
						}

						filenames.Add(newName);
					}
				}
				else
				{
					remainder.Add(line);
				}
			}

			filenames.Sort(StringComparer.Ordinal);
			parameters.Insert(0, "originalfile=" + string.Join(", ", filenames));
			return remainder;
		}

		private static string? GetInfoText(SiteTemplateNode info)
		{
			if (info.Find("description") is IParameterNode desc)
			{
				IParameterNode? node = null;
				if (desc.Value.Find<SiteTemplateNode>(value => value.Title.PageNameEquals("En")) is SiteTemplateNode en)
				{
					node = en.Find(1);
				}

				return (node ?? desc).Value.ToRaw();
			}

			return null;
		}

		private static bool IsCollectibleLink(string description) => description.StartsWith("{{Item Link", StringComparison.OrdinalIgnoreCase) && description.Contains("collectid", StringComparison.Ordinal);

		private static IList<IWikiNode>? NodeReplacer(IWikiNode node)
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
					if (templateNode.Title.PageNameEquals("Zenimage"))
					{
						return [];
					}

					break;
			}

			return null;
		}

		private static string SanitizeText(string text)
		{
			text = text
				.Replace(
				   "== Summary ==\n== Summary ==",
				   "== Summary ==",
				   StringComparison.OrdinalIgnoreCase)
				.Replace(
				   "Original filename:",
				   "Original file:",
				   StringComparison.OrdinalIgnoreCase)
				.Replace(
				   ":Original file:",
				   "Original file:",
				   StringComparison.OrdinalIgnoreCase)
				.Replace(
				   "Old file: [Original file:",
				   "Old file:",
				   StringComparison.OrdinalIgnoreCase);
			var size = text.Length;
			text = text.Replace("files:", "file", StringComparison.OrdinalIgnoreCase);
			if (text.Length != size)
			{
				text = text.Replace("\n*", ",", StringComparison.Ordinal);
			}

			return text;
		}
		#endregion

		#region Private Methods
		private void ParseDescription(SiteParser parser, List<string> parameters, string description)
		{
			if (description.Length > 0 && description[^1] != '\n')
			{
				Debug.WriteLine("Bad Original file start on File:" + parser.Title.PageName);
			}

			description = description.Trim();
			if (description.Length > 0)
			{
				if (IsCollectibleLink(description))
				{
					parameters.Add("Collectible|" + description);
				}
				else if (this.achievementsLookup.Contains(description))
				{
					parameters.Add($"Achievement|{description}");
				}
				else if (this.collectiblesLookup.TryGetValue(description, out var id))
				{
					parameters.Add($"Collectible|{{{{Item Link|{description}|collectid={id}}}}}");
				}
				else
				{
					parameters.Insert(0, "description=" + description);
				}
			}
		}

		private string ParseRemainder(List<string> parameters, List<string> filenames)
		{
			var remainder = string.Empty;
			if (filenames.Count > 1)
			{
				int i;
				for (i = 0; i < filenames.Count; i++)
				{
					var line = filenames[i].Trim();
					if (line.Length > 0 && !line.Contains("Used for:", StringComparison.Ordinal))
					{
						if (line.StartsWith(':'))
						{
							var colonSplit = line[1..].Split(TextArrays.Colon, 2);
							parameters.Add(colonSplit[0].Trim() + '|' + (colonSplit.Length == 2 ? colonSplit[1].Trim() : string.Empty));
						}
						else if (this.achievementsLookup.Contains(line))
						{
							parameters.Add($"Achievement|{line}");
							i++;
						}
						else if (IsCollectibleLink(line))
						{
							parameters.Add("Collectible|" + line);
							i++;
						}
						else if (this.collectiblesLookup.TryGetValue(line, out var id))
						{
							parameters.Add($"Collectible|{{{{Item Link|{line}|collectid={id}}}}}");
							i++;
						}

						break;
					}
				}

				if (i < filenames.Count)
				{
					remainder = string.Join('\n', filenames[i..]).Trim();
					if (remainder.Length > 0)
					{
						remainder += "\n\n";
					}
				}
			}

			return remainder;
		}

		private bool ParseSummary(SiteParser parser, Section summary)
		{
			string? text = null;
			var infoOffset = summary.Content.FindIndex<SiteTemplateNode>(n => n.Title.PageNameEquals("Information"));
			if (infoOffset != -1)
			{
				var info = (SiteTemplateNode)summary.Content[infoOffset];
				text = GetInfoText(info);
			}

			text ??= summary.Content.ToRaw();
			var index = text.IndexOf("Original file:", StringComparison.OrdinalIgnoreCase);
			if (index == -1)
			{
				return false;
			}

			var description = text[..index];
			var original = text[index..];
			var parameters = new List<string>();
			var remainingLines = GetFilenames(parser, parameters, original);
			this.ParseDescription(parser, parameters, description);
			var remainder = this.ParseRemainder(parameters, remainingLines);
			var template = CreateTemplate(summary, parameters, infoOffset, remainder);
			var parsed = parser.Parse(template);
			summary.Content.InsertRange(infoOffset + 1, parsed);
			return true;
		}

		private void UpdateText(Page page, string text)
		{
			SiteParser parser = new(page, text);
			if (parser.FindSiteTemplate("Online File") is not null)
			{
				return;
			}

			var sections = parser.ToSections(2);
			var summary = FindSummary(parser, sections) ?? sections[0];
			if (this.ParseSummary(parser, summary))
			{
				parser.FromSections(sections);
				parser.Replace(NodeReplacer, false);
				parser.UpdatePage();
				page.Text = Regex.Replace(page.Text, "\n{3,}", "\n\n", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
				page.Text = page.Text.Replace("]]\n\n[[", "]]\n[[", StringComparison.Ordinal);
			}
		}
		#endregion
	}
}