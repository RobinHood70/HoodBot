namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFMissions : CreateOrUpdateJob<SFMissions.Mission>
	{
		#region Constructors
		[JobInfo("Missions", "Starfield")]
		public SFMissions(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "mission";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create/update mission";

		protected override bool IsValid(ContextualParser parser, Mission item) => parser.FindSiteTemplate("Mission Header") is not null;

		protected override IDictionary<Title, Mission> LoadItems()
		{
			var existing = this.LoadExisting();
			var items = this.LoadQuests(existing);

			return items;
		}

		protected override string NewPageText(Title title, Mission item) => new StringBuilder()
			.Append("{{Mission Header\n")
			.Append("|type=\n")
			.Append("|Giver=\n")
			.Append("|Icon=\n")
			.Append("|Reward={{Huh}}\n")
			.Append("|ID=\n")
			.Append("|Prev=\n")
			.Append("|Next=\n")
			.Append("|Loc=\n")
			.Append("|image=\n")
			.Append("|imgdesc=\n")
			.Append("|description=\n")
			.Append("}}\n\n")
			.Append("{{Stub|Mission}}")
			.ToString();

		protected override void PageLoaded(ContextualParser parser, Mission item)
		{
			var tpl = parser.FindSiteTemplate("Mission Header");
			if (tpl is null)
			{
				return;
			}

			tpl.Update("ID", item.EditorId);
			var labelName = parser.Page.Title.LabelName();
			if (!string.Equals(labelName, item.Name, StringComparison.Ordinal))
			{
				tpl.UpdateIfEmpty("title", labelName, ParameterFormat.OnePerLine);
			}

			var sections = parser.ToSections(2);
			Section? summary = null;
			foreach (var section in sections)
			{
				if (string.Equals(section.Header?.GetTitle(true), "Official Summary", StringComparison.OrdinalIgnoreCase))
				{
					var text = section.Content.ToRaw();
					if (text.Contains(item.Summary, StringComparison.OrdinalIgnoreCase))
					{
						return;
					}

					summary = section;
					break;
				}
			}

			var lead = sections[0].Content;
			if (summary is null)
			{
				summary = Section.FromText(parser.Factory, 2, "Official Summary", $"''\"{item.Summary}\"''\n\n");
				var stubIndex = lead.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Stub"));
				if (stubIndex != -1)
				{
					var stub = lead[stubIndex];
					lead.RemoveAt(stubIndex);
					summary.Content.Add(stub);
					summary.Content.AddText("\n\n");
				}

				lead.TrimEnd();
				lead.AddText("\n\n");
				sections.Insert(1, summary);
			}

			parser.FromSections(sections);

			var stagesTemplate = parser.FindSiteTemplate("Mission Entries");
			if (stagesTemplate is null)
			{
				var insertLoc = parser.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Stub"));
				if (insertLoc == -1)
				{
					insertLoc = parser.Count;
				}

				var missionEntries = BuildStageSection(item);
				parser.Insert(insertLoc, parser.Factory.TextNode(missionEntries));
			}
		}
		#endregion

		#region Private Static Methods
		private static string BuildStageSection(Mission item)
		{
			var sb = new StringBuilder();
			sb
				.Append("\n\n== Mission Stages ==\n")
				.Append("{{Mission Entries\n");
			var missing = new List<string>();
			foreach (var stage in item.Stages)
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
				if (missing.Count == item.Stages.Count)
				{
					sb.Append("|allmissing=1\n");
				}
			}

			sb.Append("}}");
			return sb.ToString();
		}

		private static Dictionary<string, List<Stage>> LoadStages()
		{
			var retval = new Dictionary<string, List<Stage>>(StringComparer.Ordinal);
			var csv = new CsvFile
			{
				Encoding = Encoding.GetEncoding(1252),
				AutoTrim = true
			};
			csv.Load(LocalConfig.BotDataSubPath("Starfield/QuestStages2.csv"), true);
			foreach (var row in csv)
			{
				var edid = row["QuestEditorID"];
				var index = row["INDX1"].Trim(); // Actually an int, but there's no need to convert back and forth.
				var comment = row["NAM2"].Trim();
				var entry = row["CNAM"].Trim();
				var stage = new Stage(index, comment, entry);

				if (!retval.TryGetValue(edid, out var list))
				{
					list = [];
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
		#endregion

		#region Private Methods
		private Dictionary<string, string> LoadDisambigs()
		{
			var disambigLines = File.ReadAllLines(LocalConfig.BotDataSubPath("Starfield/Quests Disambigs.txt"));
			var disambigs = new Dictionary<string, string>(StringComparer.Ordinal);
			var disambigPages = new PageCollection(this.Site);
			foreach (var disambig in disambigLines)
			{
				var split = disambig.Split(TextArrays.Tab);
				var name = split[1];
				disambigs.Add(split[0], name);
				var pageName = "Starfield:" + name;
				if (!disambigPages.TryGetValue(pageName, out var disambigPage))
				{
					disambigPage = this.Site.CreatePage(pageName);
					disambigPage.Text = $"'''{name}''' may refer to:\n";
					disambigPages.Add(disambigPage);
				}

				var linkText = SiteLink.FromText(this.Site, pageName).ToString();
				disambigPage.Text += "* " + linkText + "\n";
			}

			var disambigCheck = new PageCollection(this.Site);
			disambigCheck.GetTitles(disambigPages.ToFullPageNames());
			foreach (var page in disambigPages)
			{
				var checkPage = disambigCheck[page.Title];
				if (!string.Equals(checkPage.Revisions[0].User, "RobinHood70", StringComparison.Ordinal))
				{
					Debug.WriteLine(page.Title.FullPageName() + " modified.");
				}

				page.Text += "\n{{Disambig}}";
				page.Save("Replace with disambiguation page", false);
			}

			return disambigs;
		}

		private Dictionary<string, Title> LoadExisting()
		{
			var exclusions = new TitleCollection(
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
				"Transport Passenger");

			var existing = new Dictionary<string, Title>(StringComparer.Ordinal);
			var backlinks = new PageCollection(this.Site);
			backlinks.GetBacklinks("Template:Mission Header", BacklinksTypes.EmbeddedIn);
			backlinks.Remove(exclusions);

			foreach (var page in backlinks)
			{
				var parser = new ContextualParser(page);
				var templates = new List<SiteTemplateNode>(parser.FindSiteTemplates("Mission Header"));
				if (templates.Count > 1)
				{
					Debug.WriteLine("Multiple templates on " + page.Title.FullPageName());
				}

				foreach (var template in templates)
				{
					var edidParam = template.Find(true, "ID");
					if (edidParam is not null)
					{
						var edid = edidParam.Value.ToRaw().Trim();
						if (edid.Length > 0)
						{
							if (!existing.TryAdd(edid, page.Title))
							{
								Debug.WriteLine($"Conflict for {edid} on both {existing[edid].FullPageName()} and {page.Title.FullPageName()}");
							}
						}
					}
				}
			}

			return existing;
		}

		private Dictionary<Title, Mission> LoadQuests(Dictionary<string, Title> existing)
		{
			var retval = new Dictionary<Title, Mission>();
			var stages = LoadStages();
			var disambigs = this.LoadDisambigs();

			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Quests2.csv"), true);
			foreach (var row in csv)
			{
				var edid = row["EditorID"];
				var name = row["Full"];
				name = SanitizeName(name);

				var summary = row["Summary"];
				summary = summary
					.Replace("!!----------------PLEASE WAIT TO DUPLICATE OR MODIFY-----------------!!", string.Empty, StringComparison.Ordinal)
					.Trim();

				var missionStages = stages.GetValueOrDefault(edid, []);

				if (
					name.Length > 0 &&
					!name.Contains('_', StringComparison.Ordinal) &&
					!name.Contains("convers", StringComparison.OrdinalIgnoreCase) &&
					!name.Contains("dialog", StringComparison.OrdinalIgnoreCase) &&
					!edid.Contains("dialog", StringComparison.OrdinalIgnoreCase) &&
					(summary.Length > 0 || missionStages.Count > 0))
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