namespace RobinHood70.HoodBot.Jobs;

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
using RobinHood70.WikiCommon.Parser.Basic;

internal sealed class SFMissions : CreateOrUpdateJob<SFMissions.Mission>
{
	#region Private Static Fields
	private static readonly string DisambigsFileName = GameInfo.Starfield.ModFolder + "Quests Disambigs.txt";
	private static readonly string QuestsFileName = GameInfo.Starfield.ModFolder + "Quests.csv";
	private static readonly string StagesFileName = GameInfo.Starfield.ModFolder + "QuestStages.csv";
	private readonly TitleCollection searchTitles;
	#endregion

	#region Constructors
	[JobInfo("Missions", "Starfield")]
	public SFMissions(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.NewPageText = GetNewPageText;
		this.OnUpdate = this.UpdateMission;
		this.searchTitles = new TitleCollection(
			this.Site,
			"Template:Stub",
			"Template:Starfield Trackers Alliance Mission");
	}
	#endregion

	#region Protected Override Properties
	protected override string? GetDisambiguator(Mission item) => "mission";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create/update mission";

	protected override bool IsValidPage(SiteParser parser, Mission item) =>
		FindTemplate(parser, item) is not null;

	protected override void LoadItems()
	{
		var existing = this.LoadExisting();
		var questsMissing = !File.Exists(QuestsFileName);
		var stagesMissing = !File.Exists(StagesFileName);
		if (questsMissing && stagesMissing)
		{
			return;
		}

		if (questsMissing || stagesMissing)
		{
			this.StatusWriteLine("Missing either " + QuestsFileName + " or " + StagesFileName + " but not both. WTF?");
		}

		var stages = LoadStages();
		var disambigs = LoadDisambigs();
		if (disambigs.Count == 0)
		{
			this.StatusWriteLine("No quest disambiguations found. If needed, add conflicting quests to " + DisambigsFileName);
		}

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

					if (this.Items.TryGetValue(title, out var mission))
					{
						Debug.WriteLine($"{title.FullPageName()} ({edid} / {mission.EditorId}) needs disambiguation.");
					}
					else
					{
						mission = new Mission(edid, name, summary, missionStages);
						this.Items.Add(title, mission);
					}
				}
			}
		}
	}
	#endregion

	#region Private Static Methods
	private static string BuildStageSection(List<Stage> stages)
	{
		var factory = WikiNodeFactory.DefaultInstance;
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
						.Append(factory.EscapeParameterText(stage.Comment, true))
						.Append("}}");
					if (stage.Entry.Length > 0)
					{
						sb.Append("<br>");
					}
				}

				sb
					.Append(factory.EscapeParameterText(stage.Entry, true))
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

	private static ITemplateNode? FindTemplate(SiteParser parser, Mission item)
	{
		var template = parser.FindTemplate("Mission Header");
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
		var disambigs = new Dictionary<string, string>(StringComparer.Ordinal);
		if (File.Exists(DisambigsFileName))
		{
			var disambigLines = File.ReadAllLines(DisambigsFileName);
			foreach (var disambigLine in disambigLines)
			{
				var split = disambigLine.Split(TextArrays.Tab);
				disambigs.Add(split[0], split[1]);
			}
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
	#endregion

	#region Private Methods
	private void DoStages(SiteParser parser, List<Stage> stages)
	{
		if (parser.FindTemplate("Mission Entries") is not null)
		{
			return;
		}

		var insertLoc = parser.IndexOf<ITemplateNode>(t => this.searchTitles.Contains(t.GetTitle(parser.Site)));
		if (insertLoc == -1)
		{
			insertLoc = parser.Count;
		}

		var missionEntries = BuildStageSection(stages);
		parser.Insert(insertLoc, parser.Factory.TextNode(missionEntries));
	}

	private void DoSummary(SiteParser parser, string missionSummary)
	{
		var sections = parser.ToSections(2);
		Section? summary = null;
		foreach (var section in sections.FindAll("Official Summary", StringComparer.OrdinalIgnoreCase, 0))
		{
			var text = section.Content.ToRaw();
			if (text.Contains(missionSummary, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			summary = section;
			break;
		}

		var lead = sections[0].Content;
		if (summary is null)
		{
			summary = Section.FromText(parser.Factory, 2, "Official Summary", $"''\"{missionSummary}\"''");
			var stubIndex = lead.IndexOf<ITemplateNode>(t =>
				this.searchTitles.Contains(t.GetTitle(this.Site)));
			if (stubIndex != -1)
			{
				var stub = lead[stubIndex];
				lead.RemoveAt(stubIndex);
				summary.Content.AddText("\n\n");
				summary.Content.Add(stub);
			}

			sections.InsertWithSpaceBefore(1, summary);
		}

		parser.FromSections(sections);
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
			var parser = new SiteParser(page);
			var template = parser.FindTemplate("Mission Header");
			if (template?.GetRaw("ID") is string edid &&
				edid.Length > 0 &&
				!existing.TryAdd(edid, page.Title))
			{
				Debug.WriteLine($"Conflict for {edid} on both {existing[edid].FullPageName()} and {page.Title.FullPageName()}");
			}
		}

		return existing;
	}

	private void UpdateMission(SiteParser parser, Mission item)
	{
		if (FindTemplate(parser, item) is not ITemplateNode template)
		{
			return;
		}

		template.Update("ID", item.EditorId);
		var labelName = parser.Title.LabelName();
		if (!labelName.OrdinalEquals(item.Name))
		{
			template.UpdateIfEmpty("title", labelName, ParameterFormat.OnePerLine);
		}

		if (item.Summary.Length > 0)
		{
			this.DoSummary(parser, item.Summary);
		}

		if (item.Stages.Count > 0)
		{
			this.DoStages(parser, item.Stages);
		}
	}
	#endregion

	#region Internal Records
	internal sealed record Mission(string EditorId, string Name, string Summary, List<Stage> Stages);

	internal sealed record Stage(string Index, string Comment, string Entry);
	#endregion
}