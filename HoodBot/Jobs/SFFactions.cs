namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFFactions : CreateOrUpdateJob<SFFactions.Redirect>
	{
		#region Private Constants
		private const string PageLetterPrefix = "Starfield:Factions ";
		#endregion

		#region Fields
		private readonly TitleCollection npcs;
		#endregion

		#region Constructors
		[JobInfo("Factions", "Starfield")]
		public SFFactions(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			this.NewPageText = this.GetNewPageText;
			this.StatusWriteLine("This job must be run AFTER the creature and NPC updates.");
			this.npcs = new TitleCollection(this.Site);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "faction";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create/update faction page";

		protected override bool IsValid(SiteParser parser, Redirect item) => parser.Page.IsRedirect;

		protected override IDictionary<Title, Redirect> LoadItems()
		{
			var newFactions = GetFactions(Starfield.ModFolder);
			if (newFactions.Count == 0)
			{
				return new Dictionary<Title, Redirect>();
			}

			var allMembers = this.GetFactionMembers();
			AddMembersToNewFactions(allMembers, newFactions);

			var existing = this.GetFactionPages();
			this.AddSectionsToExisting(existing, newFactions);
			UpdateFromSections(existing);

			this.StatusWriteLine("Getting NPC info");
			this.npcs
				.GetCategoryMembers("Starfield-NPCs")
				.GetCategoryMembers("Starfield-Creatures-All");
			this.AddMembersToExistingFactions(existing, allMembers);
			this.AddFactionPages(existing);

			return this.GetRedirects(newFactions);
		}
		#endregion

		#region Private Static Methods
		private static void AddMembersToNewFactions(Dictionary<string, List<Member>> allMembers, List<Faction> newFactions)
		{
			foreach (var faction in newFactions)
			{
				var factionMembers = allMembers.TryGetValue(faction.EditorId, out var membs)
					? membs
					: [];
				faction.Members.AddRange(factionMembers);
			}
		}

		private static string BuildSectionText(Faction faction, bool addHeader)
		{
			var sb = new StringBuilder();
			if (addHeader)
			{
				var titleText = Starfield.ModTemplate.Length == 0
					? faction.EditorId
					: $"{{{{Anchor|{faction.EditorId}}}}}{Starfield.ModTemplate}";
				sb.Append($"==={titleText}===\n");
			}

			sb
				.Append("{{Factions\n")
				.Append($"|edid={faction.EditorId}\n")
				.Append($"|formid={faction.FormId}\n");
			/*
			if (faction.Members.Count > 0)
			{
				sb
					.Append("|members={{Factions/Members\n")
					.Append("  |members={{List|sep=,&#32;\n");
				foreach (var member in faction.Members)
				{
					var title = this.FindNpc(member);
					sb
						.Append("    |[[")
						.Append(title.FullPageName())
						.Append("|]]");
					if (member.Rank != 0)
					{
						sb
							.Append(" (")
							.Append(member.Rank)
							.Append(')');
					}
				}

				sb.Append("\n}}}}");
			}
			*/

			sb.Append("}}\n\n");

			return sb.ToString();
		}

		private static List<Faction> GetFactions(string folder)
		{
			var csv = new CsvFile(folder + "Factions.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			var factions = new List<Faction>();
			foreach (var row in csv.ReadRows())
			{
				var formId = row["FormID"].Trim();
				if (formId.StartsWith("0x", StringComparison.Ordinal))
				{
					formId = formId[2..];
				}

				var edid = row["EditorID"].Trim();
				var name = row["Name"]
					.Replace("[", string.Empty, StringComparison.Ordinal)
					.Replace("]", string.Empty, StringComparison.Ordinal)
					.Trim();
				var faction = new Faction(formId, edid, name, []);
				factions.Add(faction);
			}

			return factions;
		}

		private static void UpdateFromSections(Dictionary<Title, SectionCollection> factionSections)
		{
			foreach (var (title, sections) in factionSections)
			{
				switch (sections.Count)
				{
					case 0:
						continue;
					case 1:
						throw new InvalidOperationException();
					default:
						sections.Parser.FromSections(sections.Values);
						break;
				}
			}
		}
		#endregion

		#region Private Methods
		private void AddFactionPages(Dictionary<Title, SectionCollection> existing)
		{
			foreach (var item in existing.Values)
			{
				item.Parser.UpdatePage();
				this.Pages.Add(item.Parser.Page);
			}
		}

		private void AddMembersToExistingFactions(Dictionary<Title, SectionCollection> existing, Dictionary<string, List<Member>> allMembers)
		{
			foreach (var (edid, newMembers) in allMembers)
			{
				foreach (var item in existing.Values)
				{
					var parser = item.Parser;
					if (parser.Find<SiteTemplateNode>(t =>
						t.Title.PageNameEquals("Factions") &&
						string.Equals(t.GetValue("edid"), edid, StringComparison.OrdinalIgnoreCase)) is not SiteTemplateNode template)
					{
						continue;
					}

					var membersNode = template.AddIfNotExists("members", "{{Factions/Members\n  }}", ParameterFormat.Packed);
					var factionMembersTemplate = membersNode.Value.Find<SiteTemplateNode>(t => t.Title.PageNameEquals("Factions/Members")) ?? throw new InvalidOperationException();
					var factionMembersNode = factionMembersTemplate.AddIfNotExists("members", string.Empty, ParameterFormat.Packed);
					if (factionMembersNode.Value.Find<SiteTemplateNode>(t => t.Title.PageNameEquals("List")) is not SiteTemplateNode listTemplate)
					{
						listTemplate = (SiteTemplateNode)factionMembersNode.Factory.TemplateNodeFromParts("List");
						factionMembersNode.Value.Add(listTemplate);
					}

					var sep = listTemplate.AddIfNotExists("sep", "\", \"", ParameterFormat.NoChange);
					var ignoreMembers = new HashSet<Title>();
					foreach (var (_, parameter) in listTemplate.GetNumericParameters())
					{
						foreach (var linkNode in parameter.Value.LinkNodes)
						{
							var siteLink = SiteLink.FromLinkNode(this.Site, linkNode);
							ignoreMembers.Add(siteLink.Title);
						}
					}

					foreach (var member in newMembers)
					{
						var title = this.FindNpc(member);
						if (!ignoreMembers.Contains(title))
						{
							var link = new SiteLink(title);
							var linkText = link.AsLink(LinkFormat.PipeTrick);
							var modTemplate = member.FromMod ? Starfield.ModTemplate : string.Empty;
							listTemplate.Add(linkText + Starfield.ModTemplate);
						}
					}

					for (int i = 0; i < listTemplate.Parameters.Count - 1; i++)
					{
						var parameter = listTemplate.Parameters[i];
						parameter.Value.Trim();
						parameter.Value.AddText("\n    ");
					}

					if (listTemplate.Parameters.Count > 0)
					{
						var parameter = listTemplate.Parameters[^1];
						parameter.Value.Trim();
						parameter.Value.AddText("\n");
					}
				}
			}
		}

		private void AddSectionsToExisting(Dictionary<Title, SectionCollection> factionSections, List<Faction> factions)
		{
			var groupedSections = new Dictionary<string, List<Faction>>(StringComparer.OrdinalIgnoreCase);
			foreach (var faction in factions)
			{
				if (faction.EditorId == "SFBGS001_LC18_CrimeFactionCrimsonFleet")
				{
				}

				if (!groupedSections.TryGetValue(faction.SectionName, out var list))
				{
					list = [];
					groupedSections.Add(faction.SectionName, list);
				}

				list.Add(faction);
			}

			var factory = new SiteNodeFactory(this.Site);
			foreach (var (sectionName, list) in groupedSections)
			{
				list.Sort((f1, f2) => f1.EditorId.CompareTo(f2.EditorId));
				var keyLetter = PageLetterMenu.GetIndexFromText(sectionName);
				var keyTitle = TitleFactory.FromUnvalidated(this.Site, PageLetterPrefix + keyLetter);
				var sections = factionSections[keyTitle];
				foreach (var faction in list)
				{
					var sectionText = BuildSectionText(faction, list.Count > 1);
					if (sections.TryGetValue(sectionName, out var section))
					{
						section.Content.AddParsed(sectionText);
					}
					else
					{
						if (sections.Count == 0)
						{
							var factionHeader = Section.FromText(factory, null, "{{Faction Header}}\n");
							sections.Add(string.Empty, factionHeader);
						}

						var title = Starfield.ModTemplate.Length == 0
							? faction.SectionName
							: $"{{{{Anchor|{faction.SectionName}}}}}{Starfield.ModTemplate}";
						section = Section.FromText(factory, title, sectionText);
						sections.Add(faction.SectionName, section);
					}
				}
			}
		}

		private Title FindNpc(Member member)
		{
			var name = member.Name;
			return
				this.npcs.TryGetValue("Starfield:" + name + " (NPC)", out var found) ? found :
				this.npcs.TryGetValue("Starfield:" + name + " (creature)", out found) ? found :
				TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
		}

		private Dictionary<string, List<Member>> GetFactionMembers()
		{
			var members = new Dictionary<string, List<Member>>(StringComparer.Ordinal);
			this.LoadNpcsFromFile(members, false);
			this.LoadNpcsFromFile(members, true);
			foreach (var (_, list) in members)
			{
				list.Sort(static (m1, m2) =>
				{
					var rankSort = m2.Rank.CompareTo(m1.Rank);
					return rankSort == 0
						? m1.Name.CompareTo(m2.Name)
						: rankSort;
				});
			}

			return members;
		}

		private Dictionary<Title, SectionCollection> GetFactionPages()
		{
			var titles = PageLetterMenu.GetTitles(this.Site, PageLetterPrefix, false);
			var existing = new PageCollection(this.Site).GetTitles(titles);
			var retval = new Dictionary<Title, SectionCollection>();
			foreach (var page in existing)
			{
				var sortedSections = new SectionCollection(page);
				retval.Add(page.Title, sortedSections);
			}

			return retval;
		}

		private string GetNewPageText(Title title, Redirect item)
		{
			var retval = $"#REDIRECT [[Starfield:Factions {item.Letter}#{item.Section}]] [[Category:Redirects to Broader Subjects]] [[Category:Starfield-Factions]]";
			var origName = (this.Disambiguator is not null && title.PageName.EndsWith(" (" + this.Disambiguator + ")", StringComparison.Ordinal))
				? title.PageName[..(this.Disambiguator.Length + 3)]
				: title.PageName;
			var data = string.Empty;
			if (item.AltName is not null)
			{
				data += "|altname=" + item.AltName;
			}

			if (origName.Contains('(', StringComparison.Ordinal))
			{
				data += "|catname=" + origName;
			}

			if (data.Length > 0)
			{
				retval += "\n{{Faction Data" + data + "}}";
			}

			return retval;
		}

		private Dictionary<Title, Redirect> GetRedirects(IEnumerable<Faction> factions)
		{
			var retval = new Dictionary<Title, Redirect>();
			foreach (var faction in factions)
			{
				var name = faction.Name;
				if (name.Length > 0)
				{
					retval.TryAdd(TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name), new Redirect(name[0], name, null));
				}

				retval.TryAdd(TitleFactory.FromUnvalidated(this.Site, "Starfield:" + faction.EditorId), new Redirect(faction.SectionName[0], faction.SectionName, faction.NameOnly));
			}

			return retval;
		}

		private void LoadNpcsFromFile(Dictionary<string, List<Member>> members, bool fromMod)
		{
			var folder = fromMod
				? Starfield.ModFolder
				: Starfield.BaseFolder;
			var factionsByFormId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var faction in GetFactions(Starfield.BaseFolder))
			{
				factionsByFormId.Add(faction.FormId, faction.SectionName);
			}

			var csv = new CsvFile(folder + "Npcs.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			foreach (var row in csv.ReadRows())
			{
				var name = row["Name"].Trim();
				var factionText = row["Factions"].Trim();
				if (name.Length == 0 || factionText.Length == 0)
				{
					continue;
				}

				var factions = factionText.Split(TextArrays.Comma);
				foreach (var faction in factions)
				{
					var split = faction.Split(TextArrays.Parentheses);
					var factionName = split[0].Trim();
					if (factionName.StartsWith("0x", StringComparison.Ordinal))
					{
						factionName = factionsByFormId[factionName[2..]];
					}

					var rank = split.Length > 1
						? (sbyte)byte.Parse(split[1], this.Site.Culture)
						: (sbyte)0;
					if (!members.TryGetValue(factionName, out var memberList))
					{
						memberList = [];
						members[factionName] = memberList;
					}

					var member = new Member(name, rank, fromMod);
					if (!memberList.Contains(member))
					{
						memberList.Add(member);
					}
				}
			}
		}
		#endregion

		#region Internal Records
		internal sealed record Redirect(char Letter, string Section, string? AltName);
		#endregion

		#region Private Records
		private sealed record Member(string Name, sbyte Rank, bool FromMod);
		#endregion

		#region Private Classes
		[DebuggerDisplay("{EditorId}")]
		private sealed class Faction(string formId, string editorId, string name, List<Member> members)
		{
			public string EditorId { get; } = editorId;

			public string FormId { get; } = formId;

			public List<Member> Members { get; } = members;

			public string Name { get; } = name;

			public string? NameOnly { get; } = name.Length == 0 || string.Equals(name, editorId, StringComparison.Ordinal) ? null : name;

			public string SectionName { get; } = name.Length == 0 ? editorId : name;
		}

		private sealed class SectionCollection : SortedDictionary<string, Section>
		{
			// Convenience class to allow sections to travel with their parent parser.
			public SectionCollection(Page page)
				: base(StringComparer.OrdinalIgnoreCase)
			{
				this.Parser = new SiteParser(page);
				var sections = this.Parser.ToSections(2);
				if (page.Text.Length > 0)
				{
					sections[^1].Content.AddText("\n\n");
					foreach (var section in sections)
					{
						this.Add(section.GetTitle() ?? string.Empty, section);
					}
				}
			}

			public SiteParser Parser { get; }
		}
		#endregion
	}
}