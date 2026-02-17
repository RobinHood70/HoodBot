namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

[method: JobInfo("Daggerfall NPCs")]
internal sealed class DaggerfallNpcs(JobManager jobManager) : CreateOrUpdateJob<DaggerfallNpcs.DaggerfallNpc>(jobManager)
{
	#region Private Constants
	private const string TemplateName = "NPC Summary";
	#endregion

	#region Fields
	private readonly List<DaggerfallNpc> npcs = [];
	#endregion

	#region Protected Override Methods
	protected override string? GetDisambiguator(DaggerfallNpc item) => "NPC";

	protected override string GetEditSummary(Page page) => "Create NPC Page";

	protected override TitleDictionary<DaggerfallNpc> GetExistingItems() => [];
	//// var pages = new PageCollection(this.Site);
	//// pages.GetBacklinks("NPC Summary", BacklinksTypes.EmbeddedIn, false, Filter.Exclude); // Add ns filter if there is one.

	protected override void GetExternalData()
	{
		var csvFile = new CsvFile(LocalConfig.BotDataSubPath("factions_output.csv"));
		foreach (var row in csvFile.ReadRows())
		{
			var npc = GetNpcInfo(row);
			this.npcs.Add(npc);
		}
	}

	protected override TitleDictionary<DaggerfallNpc> GetNewItems()
	{
		var retval = new TitleDictionary<DaggerfallNpc>();
		foreach (var npc in this.npcs)
		{
			var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Daggerfall], npc.Name);
			if (!retval.TryAdd(title, npc))
			{
				Debug.WriteLine("Duplicate name: " + title);
			}
		}

		return retval;
	}

	protected override string GetNewPageText(Title title, DaggerfallNpc npc) =>
		$$$"""
		{{{{{TemplateName}}}
		|id={{{npc.Id}}}
		|lorelink=
		|city=
		|loc=
		|race={{{npc.Race}}}
		|region={{{npc.Region}}}
		|gender=
		|type={{{npc.Type}}}
		|faction=
		|sgroup={{{npc.Sgroup}}}
		|ggroup={{{npc.Ggroup}}}
		|parentfaction={{{npc.ParentFaction}}}
		|ally1={{{npc.Ally1}}}
		|ally2={{{npc.Ally2}}}
		|enemy1={{{npc.Enemy1}}}
		|enemy2={{{npc.Enemy2}}}
		|enemy3={{{npc.Enemy3}}}
		|power={{{npc.Power}}}
		|minf={{{npc.MinF}}}
		|maxf={{{npc.MaxF}}}
		|summon={{{npc.Summon}}}
		|image=<!--DF-npc-{{{npc.Name}}}.png-->
		|imgdesc=
		}}
		""";
	#endregion

	#region Private Static Methods
	private static DaggerfallNpc GetNpcInfo(CsvRow row) => new(
		Id: row["ID"],
		Name: row["Name"],
		Type: row["Type"].TrimStart([';', ' ']),
		Summon: row["Provides Summons?"].OrdinalEquals("Cannot Summon") ? string.Empty : row["Provides Summons?"],
		Region: row["Region"].OrdinalEquals("No Region") ? string.Empty : row["Region"],
		Power: row["Power"],
		FaceId: row["Face ID"],
		Race: row["Race"].OrdinalEquals("None") ? string.Empty : row["Race"],
		Flat1: row["Flat 1"],
		Flat2: row["Flat 2"],
		Sgroup: row["Sgroup"].OrdinalEquals("None") ? string.Empty : row["Sgroup"],
		Ggroup: row["Ggroup"].OrdinalEquals("None") ? string.Empty : row["Ggroup"],
		Ally1: row["Ally1"],
		Ally2: row["Ally2"],
		Enemy1: row["Enemy1"],
		Enemy2: row["Enemy2"],
		Enemy3: row["Enemy3"],
		Flags: row["Flags"],
		Rep: row["Rep"],
		MinF: row["MinF"],
		MaxF: row["MaxF"],
		ParentFaction: row["Parent Faction"]);
	#endregion

	#region Private Classes
	internal sealed record DaggerfallNpc(string Id, string Name, string Type, string Summon, string Region, string Power, string FaceId, string Race, string Flat1, string Flat2, string Sgroup, string Ggroup, string Ally1, string Ally2, string Enemy1, string Enemy2, string Enemy3, string Flags, string Rep, string MinF, string MaxF, string ParentFaction);
	#endregion
}