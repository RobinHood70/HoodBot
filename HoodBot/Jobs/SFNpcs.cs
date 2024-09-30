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
		[JobInfo("NPCs", "Starfield")]
		public SFNpcs(JobManager jobManager)
			: base(jobManager)
		{
			this.Clobber = true;
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "NPC";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create NPC page";

		protected override bool IsValid(ContextualParser parser, Npcs item) => true; // parser.FindSiteTemplate("NPC Summary") is not null || parser.Page.IsMissing;

		protected override IDictionary<Title, Npcs> LoadItems()
		{
			var items = new Dictionary<Title, Npcs>();
			var csv = new CsvFile
			{
				Encoding = Encoding.GetEncoding(1252),
				FieldSeparator = '\t'
			};
			csv.Load(Starfield.ModFolder + "Npcs.csv", true);
			foreach (var row in csv)
			{
				var name = row["Full Name: Full Name <<TESFullName_Component>>"];
				if (name.Length > 0)
				{
					// var acbs = Convert.ToInt32(row["Acbs1"], 16);
					// var gender = (acbs & 1) != 0;
					// var dead = (acbs & 0x200000) != 0;
					var factionText = string.Empty; // row["Factions"];
					var factions = factionText.Length == 0
						? []
						: factionText.Split(TextArrays.Comma);
					var attribField = row["Property Sheet: ActorValues <<BGSPropertySheet_Component>>"];
					var attribsSplit = attribField.Split(", ");
					var attribs = new Dictionary<string, string>(StringComparer.Ordinal);
					foreach (var attrib in attribsSplit)
					{
						var attribValue = attrib.Split("::");
						attribs.Add(attribValue[0], attribValue[1]);
					}

					var healthText = attribs["Health"];
					var health = (int)double.Parse(healthText, this.Site.Culture);
					var dead = health <= 0;
					var npc = new Npc(
						row["Numeric ID"].Trim(TextArrays.Parentheses),
						row["Editor ID"].Trim(),
						name,
						row["Race: Race <<TESRace_Component>>"].Replace("Race", string.Empty, StringComparison.Ordinal),
						null /*gender*/,
						dead,
						factions,
						health);
					var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
					var list = items.TryGetValue(title, out var npcs) ? npcs : [];
					list.Add(npc);
					items[title] = list;
				}
			}

			return items;
		}

		protected override void PageLoaded(ContextualParser parser, Npcs item)
		{
			// parser.Page.Text = this.NewPageText(parser.Page.Title, item);
			// parser.ReparsePageText(WikiCommon.Parser.InclusionType.Raw, false);
		}

		protected override string NewPageText(Title title, Npcs item)
		{
			var sb = new StringBuilder();
			foreach (var npc in item)
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
					.Append("{{NewLine}}\n")
					.Append("{{NPC Summary\n")
					.Append($"|eid={npc.EditorID}\n")
					.Append($"|race={npc.Race}\n")
					.Append($"|baseid={npc.FormID}\n")
					.Append($"|gender={gender}\n")
					.Append(this.Site.Culture, $"|health={npc.Health}\n");
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
		#endregion

		#region Internal Record Structs
		internal record struct Npc(string FormID, string EditorID, string Name, string Race, bool? Gender, bool Dead, IReadOnlyList<string> Factions, int Health);
		#endregion

		#region Internal Classes
		internal sealed class Npcs : List<Npc>
		{
		}
		#endregion
	}
}