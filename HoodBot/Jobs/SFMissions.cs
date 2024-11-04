namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFMissions : CreateOrUpdateJob<SFMissions.Mission>
	{
		#region Private Static Fields
		private static readonly string DisambigFileName = Starfield.ModFolder + "Quests Disambigs.txt";
		private static readonly string QuestsFileName = Starfield.ModFolder + "Quests.csv";
		private static readonly string StagesFileName = Starfield.ModFolder + "QuestStages.csv";
		#endregion

		#region Constructors
		[JobInfo("Missions", "Starfield")]
		public SFMissions(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			this.NewPageText = GetNewPageText;
			this.OnUpdate = UpdateMission;
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "mission";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create/update mission";

		protected override bool IsValid(SiteParser parser, Mission item) =>
			FindTemplate(parser, item) is not null;

		protected override IDictionary<Title, Mission> LoadItems()
		{
			var existing = this.LoadExisting();
			var items = this.LoadQuests(existing);

			return items;
		}
		#endregion

		#region Private Static Methods
		private static string BuildStageSection(List<Stage> stages)
		{
			var sb = new StringBuilder();
			sb
				.Append("== Mission Stages ==\n")
				.Append("{{Mission Entries\n");
			var missing = new List<string>();
			foreach (var stage in stages)
			{
				if (stage.Entry.Length == 0 && stage.Comment.Length == 0)
				{
					missing.Add(stage.Index);
				}
				else
				{
					sb
						.Append('|')
						.Append(stage.Index)
						.Append('|')
						.Append('|');
					if (stage.Comment.Length > 0)
					{
						sb
							.Append("{{Mission Comment|")
							.Append(stage.Comment.Replace("=", "{{=}}", StringComparison.Ordinal))
							.Append("}}");
						if (stage.Entry.Length > 0)
						{
							sb.Append("<br>");
						}
					}

					sb
						.Append(stage.Entry)
						.Append('\n');
				}
			}

			if (missing.Count > 0)
			{
				sb
					.Append("|missing=")
					.AppendJoin(", ", missing)
					.Append('\n');
				if (missing.Count == stages.Count)
				{
					sb.Append("|allmissing=1\n");
				}
			}

			sb.Append("}}\n\n");
			return sb.ToString();
		}

		private static void DoStages(SiteParser parser, List<Stage> stages)
		{
			var stagesTemplate = parser.FindSiteTemplate("Mission Entries");
			if (stagesTemplate is null)
			{
				var insertLoc = parser.FindIndex<SiteTemplateNode>(t => t.Title.PageNameEquals("Stub") || t.Title.PageNameEquals("Starfield Trackers Alliance Mission"));
				if (insertLoc == -1)
				{
					insertLoc = parser.Count;
				}

				var missionEntries = BuildStageSection(stages);
				parser.Insert(insertLoc, parser.Factory.TextNode(missionEntries));
			}
		}

		private static void DoSummary(SiteParser parser, string missionSummary)
		{
			var sections = parser.ToSections(2);
			Section? summary = null;
			foreach (var section in sections)
			{
				if (string.Equals(section.GetTitle(), "Official Summary", StringComparison.OrdinalIgnoreCase))
				{
					var text = section.Content.ToRaw();
					if (text.Contains(missionSummary, StringComparison.OrdinalIgnoreCase))
					{
						return;
					}

					summary = section;
					break;
				}
			}

			var lead = sections[0].Content;
			lead.TrimEnd();
			lead.AddText("\n\n");
			if (summary is null)
			{
				summary = Section.FromText(parser.Factory, 2, "Official Summary", $"''\"{missionSummary}\"''");
				var stubIndex = lead.FindIndex<SiteTemplateNode>(t => t.Title.PageNameEquals("Stub") || t.Title.PageNameEquals("Starfield Trackers Alliance Mission"));
				if (stubIndex != -1)
				{
					var stub = lead[stubIndex];
					lead.RemoveAt(stubIndex);
					lead.TrimEnd();
					lead.AddText("\n\n");
					summary.Content.AddText("\n\n");
					summary.Content.Add(stub);
				}

				sections.Insert(1, summary);
			}

			parser.FromSections(sections);
		}

		private static SiteTemplateNode? FindTemplate(SiteParser parser, Mission item)
		{
			var template = parser.FindSiteTemplate("Mission Header");
			return (template?.GetValue("ID")?.Trim()).OrdinalEquals(item.EditorId)
				? template
				: null;
		}

		private static string GetNewPageText(Title title, Mission item) => new StringBuilder()
			.Append("{{Mission Header\n")
			.Append("|type=\n")
			.Append("|Giver=\n")
			.Append("|Icon=\n")
			.Append("|Reward={{Huh}}\n")
			.Append($"|ID={item.EditorId}\n")
			.Append("|Prev=\n")
			.Append("|Next=\n")
			.Append("|Loc=\n")
			.Append("|image=\n")
			.Append("|imgdesc=\n")
			.Append("|description=\n")
			.Append("}}\n\n")
			.Append("{{Stub|Mission}}")
			.ToString();

		private static Dictionary<string, string> LoadDisambigs()
		{
			var disambigLines = File.ReadAllLines(DisambigFileName);
			var disambigs = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var disambigLine in disambigLines)
			{
				var split = disambigLine.Split(TextArrays.Tab);
				disambigs.Add(split[0], split[1]);
			}

			return disambigs;
		}

		private static Dictionary<string, List<Stage>> LoadStages()
		{
			var retval = new Dictionary<string, List<Stage>>(StringComparer.Ordinal);
			var csv = new CsvFile(StagesFileName)
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			foreach (var row in csv.ReadRows())
			{
				var edid = row["QuestEditorID"];
				var index = row["INDX1"].Trim(); // Actually an int, but there's no need to convert back and forth.
				var comment = row["NAM2"].Trim();
				var entry = row["CNAM"].Trim();
				var stage = new Stage(index, comment, entry);

				if (!retval.TryGetValue(edid, out var list))
				{
					list = [];
					retval[edid] = list;
				}

				list.Add(stage);
			}

			return retval;
		}

		private static string SanitizeName(string name)
		{
			if (name.StartsWith('[') && name.EndsWith(']'))
			{
				name = name[1..^1];
			}

			return name
				.Replace("<Alias=", string.Empty, StringComparison.Ordinal)
				.Replace(">", string.Empty, StringComparison.Ordinal)
				.Replace('[', '(')
				.Replace(']', ')')
				.Trim();
		}

		private static void UpdateMission(SiteParser parser, Mission item)
		{
			var template = FindTemplate(parser, item);
			if (template is null)
			{
				return;
			}

			template.Update("ID", item.EditorId);
			var labelName = parser.Page.Title.LabelName();
			if (!labelName.OrdinalEquals(item.Name))
			{
				template.UpdateIfEmpty("title", labelName, ParameterFormat.OnePerLine);
			}

			if (item.Summary.Length > 0)
			{
				DoSummary(parser, item.Summary);
			}

			if (item.Stages.Count > 0)
			{
				DoStages(parser, item.Stages);
			}
		}
		#endregion

		#region Private Methods
		private Dictionary<string, Title> LoadExisting()
		{
			/* var exclusions = new TitleCollection(
				this.Site,
				StarfieldNamespaces.Starfield,
				"Deliver to Location",
				"Destroy Ship",
				"Drydock Blues: Deimos Staryard",
				"Drydock Blues: Hopetech",
				"Drydock Blues: Stroud-Eklund S",
				"Drydock Blues: Taiyo Astroneering Store",
				"Drydock Blues: Trident Luxury Lines Staryard",
				"Starborn (mission)",
				"Supply Location",
				"Survey Planet",
				"Trader (mission)",
				"Transport Passenger"); */

			var existing = new Dictionary<string, Title>(StringComparer.Ordinal);
			var backlinks = new PageCollection(this.Site);
			backlinks.GetBacklinks("Template:Mission Header", BacklinksTypes.EmbeddedIn);
			//// backlinks.Remove(exclusions);

			foreach (var page in backlinks)
			{
				var parser = new SiteParser(page);
				var template = parser.FindSiteTemplate("Mission Header");
				if (template?.GetRaw("ID") is string edid &&
					edid.Length > 0 &&
					!existing.TryAdd(edid, page.Title))
				{
					Debug.WriteLine($"Conflict for {edid} on both {existing[edid].FullPageName()} and {page.Title.FullPageName()}");
				}
			}

			return existing;
		}

		private Dictionary<Title, Mission> LoadQuests(Dictionary<string, Title> existing)
		{
			var retval = new Dictionary<Title, Mission>();
			var stages = LoadStages();
			var disambigs = LoadDisambigs();
			var csv = new CsvFile(QuestsFileName)
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			foreach (var row in csv.ReadRows())
			{
				var edid = row["EditorID"];
				var name = row["Full"];
				name = SanitizeName(name);
				if (edid.Contains("dialog", StringComparison.OrdinalIgnoreCase) ||
					name.Length == 0 ||
					name.Contains('_', StringComparison.Ordinal) ||
					name.Contains("convers", StringComparison.OrdinalIgnoreCase) ||
					name.Contains("dialog", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				var summary = row["Summary"];
				summary = summary
					.Replace("!!----------------PLEASE WAIT TO DUPLICATE OR MODIFY-----------------!!", string.Empty, StringComparison.Ordinal)
					.Trim();

				var missionStages = stages.GetValueOrDefault(edid, []);

				if (summary.Length > 0 || missionStages.Count > 0)
				{
					name = disambigs.GetValueOrDefault(edid, name);
					if (name.Length > 0)
					{
						if (!existing.TryGetValue(edid, out var title))
						{
							title = TitleFactory.FromUnvalidated(this.Site.Namespaces["Starfield"], name);
						}

						if (retval.TryGetValue(title, out var mission))
						{
							Debug.WriteLine($"{title.FullPageName()} ({edid} / {mission.EditorId}) needs disambiguation.");
						}
						else
						{
							mission = new Mission(edid, name, summary, missionStages);
							retval.Add(title, mission);
						}
					}
				}
			}

			return retval;
		}
		#endregion

		#region Internal Records
		internal sealed record Mission(string EditorId, string Name, string Summary, List<Stage> Stages);

		internal sealed record Stage(string Index, string Comment, string Entry);
		#endregion
	}
}