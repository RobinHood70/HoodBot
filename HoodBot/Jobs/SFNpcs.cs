namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
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
		protected override string? Disambiguator => "NPC";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create NPC page";

		protected override bool IsValid(ContextualParser parser, Npcs item) => parser.FindSiteTemplate("NPC Summary") is not null;

		protected override IDictionary<Title, Npcs> LoadItems()
		{
			var races = GetRaces();

			var items = new Dictionary<Title, Npcs>();
			var csv = new CsvFile(Starfield.ModFolder + "Npcs.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			csv.Load();
			foreach (var row in csv)
			{
				var name = row["Name"];
				if (name.Length > 0)
				{
					var acbs = Convert.ToInt32(row["Acbs1"], 16);
					var gender = (acbs & 1) != 0;
					var dead = (acbs & 0x200000) != 0;
					var formId = row["FormID"]
						.Replace("0x", string.Empty, StringComparison.Ordinal);
					var editorId = row["EditorID"];
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
						formId,
						editorId,
						name,
						race,
						gender,
						dead,
						factions,
						string.Empty);
					var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
					var list = items.TryGetValue(title, out var npcs) ? npcs : [];
					list.Add(npc);
					items[title] = list;
				}
			}

			return items;
		}
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
				.Append("}}\n\n")
				.Append("{{NewLine}}\n");
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
				sb.Remove(sb.Length - 12, 12);
			}

			sb.Append("{{Npc Navbox}}\n\n{{Stub|NPC}}");
			return sb.ToString();
		}

		private static Dictionary<string, string> GetRaces()
		{
			var dict = new Dictionary<string, string>(StringComparer.Ordinal);
			GetRaces(dict, Starfield.BaseFolder);
			GetRaces(dict, Starfield.ModFolder);

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

		private static void UpdateNpcs(ContextualParser parser, Npcs item)
		{
			// Currently designed for insert only, no updating. Template code has to be duplicated here as well as on NewPageText so that it passes validity checks but also handles insertion correctly.
			var insertPos = parser.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Item Summary"));
			foreach (var npc in item)
			{
				if (FindMatchingTemplate(parser, npc) is null)
				{
					var sb = new StringBuilder();
					BuildTemplate(sb, npc);
					var newNodes = parser.Parse(sb.ToString());
					parser.InsertRange(insertPos, newNodes);
					insertPos += newNodes.Count;
				}
			}
		}

		private static SiteTemplateNode? FindMatchingTemplate(ContextualParser parser, Npc search)
		{
			var templates = parser.FindSiteTemplates("NPC Summary");
			foreach (var template in templates)
			{
				if (string.Equals(template.GetValue("eid")?.Trim(), search.EditorID, StringComparison.OrdinalIgnoreCase))
				{
					return template;
				}
			}

			return null;
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
}