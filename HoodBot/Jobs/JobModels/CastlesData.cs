namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using RobinHood70.WikiCommon;

internal sealed class CastlesData
{
	#region Private Constants
	public const string CommaSpace = ", ";
	public const string NotFound = " - NOT FOUND!";
	#endregion

	#region Static Fields
	private static readonly string[] PropGroupSummaries = ["Kitchen", "Oil Press", "Smelter", "Loom", "Mill", "Workshop", "Sewing Table", "Smithy", "Bookshelf", "Music Station", "Art Studio", "Buffet", "Bath", "Throne", "Bed", "War Table", "Decoration", "Shrine of Mara", "Dragon Lair", string.Empty, "Gauntlet"];
	private static readonly string[] Races = ["Argonian", "Breton", "Dark Elf", "High Elf", "Imperial", "Khajiit", "Nord", "Orc", "Redguard", "Wood Elf"];
	#endregion

	#region Fields
	private readonly Dictionary<int, string> archetypes = [];
	private readonly List<string>[] propGroups = new List<string>[21];
	private readonly Dictionary<int, string> propData = [];
	private readonly Dictionary<int, string> quests = [];
	private readonly Dictionary<int, string> rulingFlags = [];
	private readonly Dictionary<int, string> taskPools = [];
	private readonly Dictionary<int, string> traits = [];
	#endregion

	#region Constructors
	public CastlesData(CultureInfo culture)
	{
		this.Translator = new CastlesTranslator(culture);
		this.LoadGroups();
		this.LoadPropData();
		this.LoadQuests();
		this.LoadRulingFlags();
		this.LoadSubjectArchetypes();
		this.LoadTags();
		this.LoadTaskPools();
		this.LoadTraits();
	}
	#endregion

	#region Public Properties
	public Dictionary<int, string> Groups { get; } = [];

	public Dictionary<int, string> Tags { get; } = [];

	public CastlesTranslator Translator { get; }
	#endregion

	#region Public Static Methods
	public static string? GetRaces(string intro, JToken items)
	{
		var list = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHaveInt("_race");
			var text = Races[id];
			list.Add(text);
		}

		return list.Count == 0
			? null
			: intro + string.Join(CommaSpace, list);
	}
	#endregion

	#region Public Methods
	public string? GetArchetypes(string intro, JToken items)
	{
		var list = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHave("_subjectArchetypeUid").MustHave("_uid").MustHaveInt("id");
			var text = this.archetypes[id];
			list.Add(text);
		}

		return list.Count == 0
			? null
			: intro + string.Join(CommaSpace, list);
	}

	public string? GetGroups(string intro, JToken items)
	{
		var list = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHave("_groupUid").MustHave("_uid").MustHaveInt("id");
			var condition = item.MustHaveInt("_condition") == 0 ? "not " : string.Empty;
			var text = this.Groups[id];
			list.Add(condition + text);
		}

		return list.Count == 0
			? null
			: intro + string.Join(CommaSpace, list);
	}

	public string? GetRulingFlags(string intro, JToken items)
	{
		var flagList = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHave("_rulingFlagUid").MustHave("_uid").MustHaveInt("id");
			var text = this.rulingFlags[id];
			if (item.MustHaveInt("_enabled") == 0)
			{
				text += " (disabled)";
			}

			flagList.Add(text);
		}

		return flagList.Count == 0
			? null
			: intro + ": " + string.Join(CommaSpace, flagList);
	}

	public string? GetPropConditions(string intro, JToken items)
	{
		var list = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHave("_propUid").MustHave("_uid").MustHaveInt("id");
			var text = id == 0
				? "any " + PropGroupSummaries[item.MustHaveInt("_anyOfPropsInGroup")]
				: this.propData[id];
			var condition = item.MustHaveInt("_condition") == 0 ? "not " : string.Empty;
			list.Add(condition + text);
		}

		return list.Count == 0
			? null
			: intro + string.Join(CommaSpace, list);
	}

	public string? GetQuestConditions(string intro, JToken items)
	{
		var list = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHave("_questTemplateUid").MustHave("_uid").MustHaveInt("id");
			var text = this.quests[id];
			var condition = item.MustHaveInt("_condition") == 0 ? "not " : string.Empty;
			list.Add(condition + text);
		}

		return list.Count == 0
			? null
			: intro + string.Join(CommaSpace, list);
	}

	public string? GetTaskPools(string intro, JToken item)
	{
		var id = item.MustHave("_uid").MustHaveInt("id");
		return id == 0
			? null
			: intro + this.taskPools[id];
	}

	public string? GetTraits(string intro, JToken items)
	{
		var list = new List<string>();
		foreach (var item in items)
		{
			var id = item.MustHave("_traitUid").MustHave("_uid").MustHaveInt("id");
			var condition = item.MustHaveInt("_condition") == 0 ? "not " : string.Empty;
			var text = this.traits[id];
			list.Add(condition + text);
		}

		return list.Count == 0
			? null
			: intro + string.Join(CommaSpace, list);
	}
	#endregion

	#region Private Methods
	private void LoadGroups()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "GroupDataDefault2.json");
		string[] groupCats = ["_allSubjectsGroups", "_socialClassGroup", "_raceGroups"];
		foreach (var groupCat in groupCats)
		{
			var items = obj.MustHave(groupCat);
			foreach (var item in items)
			{
				var id = item.MustHave("_groupUid").MustHaveInt("id");
				var title = item.MustHaveString("_displayName");
				if (this.Translator.GetLanguageEntry(title) is string desc)
				{
					this.Groups.Add(id, desc);
				}
			}
		}
	}

	private void LoadPropData()
	{
		for (var i = 0; i <= 20; i++)
		{
			this.propGroups[i] = [];
		}

		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "PropDataDefault2.json");
		var items = obj.MustHave("_props");
		foreach (var item in items)
		{
			var id = item.MustHave("_propUid").MustHaveInt("id");
			var title = item.MustHaveString("_displayName");
			if (this.Translator.GetLanguageEntry(title) is string desc)
			{
				this.propData.Add(id, desc);
				var group = item.MustHave("_propGroupData").MustHaveInt("_propGroup");
				var list = this.propGroups[group];
				list.Add(desc);
			}
		}
	}

	private void LoadRulingFlags()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "RulingsDefault2.json");
		var items = obj.MustHave("_rulingFlagDefinitions");
		foreach (var item in items)
		{
			var id = item.MustHave("_rulingFlagUid").MustHaveInt("id");
			var name = item.MustHaveString("_expressionName");
			this.rulingFlags.Add(id, name);
		}
	}

	private void LoadQuests()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "QuestData.json");
		var items = obj.MustHave("_quests");
		foreach (var item in items)
		{
			var id = item.MustHave("_questUid").MustHaveInt("id");
			var title = item.MustHaveString("_nameLocalizationKey");
			var desc = this.Translator.GetLanguageEntry(title) ?? (title + NotFound);
			this.quests.Add(id, desc);
		}
	}

	private void LoadSubjectArchetypes()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "SubjectArchetypesDefault2.json");
		var items = obj.MustHave("_subjectArchetypesList");
		foreach (var item in items)
		{
			// Not all names are language entries, so warnings coming from this function are normal.
			var id = item.MustHave("_subjectArchetypeUid").MustHaveInt("id");
			var firstName = item.MustHaveString("_firstname");
			if (firstName.Length > 0 && this.Translator.GetLanguageEntry(firstName) is string translationFirst)
			{
				firstName = translationFirst;
			}

			var lastName = item.MustHaveString("_lastname");
			lastName = lastName.Replace("\u200B", string.Empty, StringComparison.Ordinal);
			if (lastName.Length > 0 && this.Translator.GetLanguageEntry(lastName) is string translationLast)
			{
				lastName = translationLast;
			}

			if (firstName.Length > 0 && lastName.Length > 0)
			{
				firstName += " ";
			}

			this.archetypes.Add(id, firstName + lastName);
		}
	}

	private void LoadTags()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "TagDataDefault2.json");
		var items = obj.MustHave("_tags");
		foreach (var item in items)
		{
			var id = item.MustHave("_tagUid").MustHaveInt("id");
			var desc = item.MustHaveString("_description");
			this.Tags.Add(id, desc);
		}
	}

	private void LoadTaskPools()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "TasksDefault2.json");
		var items = obj.MustHave("_missionTaskPools");
		foreach (var item in items)
		{
			var id = item.MustHave("_taskPoolUid").MustHaveInt("id");
			var title = item.MustHaveString("_name");
			if (this.Translator.GetLanguageEntry(title) is string desc)
			{
				this.taskPools.Add(id, desc);
			}
		}
	}

	private void LoadTraits()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + "TraitDataDefault2.json");
		string[] traitCats = ["_dramaticTraits", "_jealousTraits", "_mightyTraits", "_deftTraits", "_lazyTraits", "_musicianTraits", "_academicTraits", "_generousTraits", "_bullyTraits", "_emotionalTraits", "_attentiveTraits", "_hauntedTraits", "_inspiringTraits", "_jesterTraits", "_pyromaniacTraits", "_stunningTraits", "_theatricalTraits", "_adamantTraits", "_preciseTraits", "_recklessTraits", "_leaderTraits", "_followerTraits", "_heartlessTraits", "_treacherousTraits", "_intenseTraits", "_gourmetTraits", "_influentialTraits", "_durableTraits", "_tribalTraits"];
		foreach (var traitCat in traitCats)
		{
			var items = obj.MustHave(traitCat);
			foreach (var item in items)
			{
				var id = item.MustHave("_traitUid").MustHaveInt("id");
				var title = item.MustHaveString("_title");
				if (this.Translator.GetLanguageEntry(title) is string desc)
				{
					this.traits.Add(id, desc);
				}
			}
		}
	}
	#endregion
}