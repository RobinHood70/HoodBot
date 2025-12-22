namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class SFNpcs : CreateOrUpdateJob<SFNpcs.Npcs>
{
	#region Constructors
	[JobInfo("NPCs", "Starfield")]
	public SFNpcs(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.NewPageText = GetNewPageText;
		this.OnUpdate = UpdateNpcs;
	}
	#endregion

	#region Protected Override Properties
	protected override string? GetDisambiguator(Npcs item) => "NPC";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create NPC page";

	protected override TitleDictionary<Npcs> GetExistingItems() => [];

	protected override void GetExternalData()
	{
		// NOTE: This was a hasty conversion to the new format that just stuffs everything in GetExternalData(). If used again in the future, it should probably be separated into its proper GetExternal/GetExisting/GetNew components.
		var fauna = GetFauna();
		var races = GetRaces();
		var csv = new CsvFile(GameInfo.Starfield.ModFolder + "Npcs.csv")
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		foreach (var row in csv.ReadRows())
		{
			var name = row["Name"];
			if (name.Length > 0)
			{
				var editorId = row["EditorID"];
				if (fauna.Contains(editorId))
				{
					continue;
				}

				var acbs = Convert.ToInt32(row["Acbs1"], 16);
				var gender = (acbs & 1) != 0;
				var dead = (acbs & 0x200000) != 0;
				var raceId = row["Race"];
				if (!races.TryGetValue(raceId, out var race))
				{
					race = string.Empty;
				}

				var factionText = row["Factions"];
				var factions = factionText.Length == 0
					? []
					: factionText.Split(TextArrays.Comma);
				var npc = new Npc(
					UespFunctions.FixFormId(row["FormID"]),
					editorId,
					name,
					race,
					gender,
					dead,
					factions,
					string.Empty);
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
				var list = this.Items.TryGetValue(title, out var npcs) ? npcs : [];
				list.Add(npc);
				this.Items[title] = list;
			}
		}
	}

	protected override TitleDictionary<Npcs> GetNewItems() => [];

	protected override bool IsValidPage(SiteParser parser, Npcs item) => parser.FindTemplate("NPC Summary") is not null;
	#endregion

	#region Private Static Methods
	private static void BuildTemplate(StringBuilder sb, Npc npc)
	{
		var gender = npc.Gender switch
		{
			null => string.Empty,
			true => "Female",
			false => "Male"
		};
		var factions = npc.Factions.Count == 0
			? string.Empty
			: ("{{Faction|" + string.Join("}}, {{Faction|", npc.Factions) + "}}");
		sb
			.Append("\n\n{{NewLine}}\n")
			.Append("{{NPC Summary\n")
			.Append($"|eid={npc.EditorID}\n")
			.Append($"|race={npc.Race}\n")
			.Append($"|baseid={npc.FormID}\n")
			.Append($"|gender={gender}\n")
			.Append($"|health={npc.Health}\n");
		if (npc.Dead)
		{
			sb.Append("|dead=1\n");
		}

		sb
			.Append("|skills=\n")
			.Append($"|faction={factions}\n")
			.Append("|image=\n")
			.Append("|imgdesc=\n")
			.Append("}}");
	}

	private static ITemplateNode? FindMatchingTemplate(SiteParser parser, Npc search)
	{
		var templates = parser.FindTemplates("NPC Summary");
		foreach (var template in templates)
		{
			if ((template.GetValue("eid")?.Trim()).OrdinalICEquals(search.EditorID))
			{
				return template;
			}
		}

		return null;
	}

	private static HashSet<string> GetFauna()
	{
		var fauna = new HashSet<string>(StringComparer.Ordinal);
		var csv = new CsvFile(GameInfo.Starfield.ModFolder + "Fauna.csv");
		foreach (var row in csv.ReadRows())
		{
			fauna.Add(row["EditorID"]);
		}

		return fauna;
	}

	private static string GetNewPageText(Title title, Npcs item)
	{
		var sb = new StringBuilder();
		foreach (var npc in item)
		{
			BuildTemplate(sb, npc);
		}

		if (sb.Length > 0)
		{
			sb.Remove(0, 14);
		}

		sb.Append("\n\n{{Npc Navbox}}\n\n{{Stub|NPC}}");
		return sb.ToString();
	}

	private static Dictionary<string, string> GetRaces()
	{
		var dict = new Dictionary<string, string>(StringComparer.Ordinal);
		GetRaces(dict, GameInfo.Starfield.BaseFolder);
		GetRaces(dict, GameInfo.Starfield.ModFolder);

		return dict;
	}

	private static void GetRaces(Dictionary<string, string> dict, string folder)
	{
		var fileName = folder + "Races.csv";
		if (!File.Exists(fileName))
		{
			return;
		}

		var csv = new CsvFile(fileName)
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var row in csv)
		{
			var formId = row["FormID"];
			var name = row["Name"];
			if (name.Length == 0)
			{
				name = row["EditorID"]
					.UnCamelCase()
					.Replace('_', ' ')
					.Replace(" Race", string.Empty, StringComparison.Ordinal);
			}

			dict[formId] = name;
		}
	}

	private static void UpdateNpcs(SiteParser parser, Npcs item)
	{
		// Currently designed for insert only, no updating. Template code has to be duplicated here as well as on NewPageText so that it passes validity checks but also handles insertion correctly.
		var insertPos = parser.LastIndexOf<ITemplateNode>(t => t.GetTitle(parser.Site) == "Template:NPC Summary") + 1;
		foreach (var npc in item)
		{
			if (FindMatchingTemplate(parser, npc) is null)
			{
				var sb = new StringBuilder();
				BuildTemplate(sb, npc);
				var text = sb.ToString();
				var newNodes = parser.Parse(text);
				parser.InsertRange(insertPos, newNodes);
				insertPos += newNodes.Count;
			}
		}
	}
	#endregion

	#region Internal Record Structs
	internal record struct Npc(string FormID, string EditorID, string Name, string Race, bool? Gender, bool Dead, IReadOnlyList<string> Factions, string Health);
	#endregion

	#region Internal Classes
	internal sealed class Npcs : List<Npc>
	{
	}
	#endregion
}