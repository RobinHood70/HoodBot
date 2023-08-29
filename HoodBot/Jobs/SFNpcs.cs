namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFNpcs : CreateOrUpdateJob<SFNpcs.Npcs>
	{
		#region Constructors
		[JobInfo("SF NPCs")]
		public SFNpcs(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Create Npc page";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, Npcs item) => parser.FindSiteTemplate("NPC Summary") is not null;

		protected override IDictionary<Title, Npcs> LoadItems()
		{
			var items = new Dictionary<Title, Npcs>();
			var csv = new CsvFile();
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Npcs.csv"), true, Encoding.GetEncoding(1252));
			foreach (var row in csv)
			{
				if (row["Name"].Length > 0)
				{
					var acbs = Convert.ToInt32(row["Acbs1"], 16);
					var female = (acbs & 1) != 0;
					var dead = (acbs & 0x200000) != 0;
					var factionText = row["Factions"];
					var factions = factionText.Length == 0
						? Array.Empty<string>()
						: factionText.Split(TextArrays.Comma);
					var npc = new Npc(row["FormID"][2..], row["EditorID"].Trim(), row["Name"], row["Race"], female, dead, factions);
					var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + row["Name"]);
					var list = items.TryGetValue(title, out var npcs) ? npcs : new Npcs();
					list.Add(npc);
					items[title] = list;
				}
			}

			return items;
		}

		protected override string NewPageText(Title title, Npcs item)
		{
			var sb = new StringBuilder();
			foreach (var npc in item)
			{
				var gender = npc.Female ? "Female" : "Male";
				var factions = npc.Factions.Count == 0
					? string.Empty
					: ("{{Faction|" + string.Join("}}, {{Faction|", npc.Factions) + "}}");
				sb
					.Append("{{NewLine}}\n")
					.Append("{{NPC Summary\n")
					.Append($"|eid={npc.EditorID}\n")
					.Append($"|race={npc.Race}\n")
					.Append($"|baseid={npc.FormID}\n")
					.Append($"|gender={gender}\n")
					.Append("|health\n");
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

			var npcs = sb.ToString()[12..];
			return npcs + "\n\n{{Stub|NPC}}";
		}

		protected override void PageLoaded(ContextualParser parser, Npcs item)
		{
		}
		#endregion

		#region Internal Record Structs
		internal record struct Npc(string FormID, string EditorID, string Name, string Race, bool Female, bool Dead, IReadOnlyList<string> Factions);
		#endregion

		#region Internal Classes
		internal sealed class Npcs : List<Npc>
		{
		}
		#endregion
	}
}