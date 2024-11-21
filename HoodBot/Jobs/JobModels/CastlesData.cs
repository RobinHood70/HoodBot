namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using Newtonsoft.Json;

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
		public static string? GetRaces(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._race;
				var text = Races[id];
				list.Add(text);
			}

			return intro + string.Join(CommaSpace, list);
		}
		#endregion

		#region Public Methods
		public string? GetArchetypes(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._subjectArchetypeUid._uid.id;
				var text = this.archetypes[id];
				list.Add(text);
			}

			return intro + string.Join(CommaSpace, list);
		}

		public string? GetGroups(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._groupUid._uid.id;
				var condition = item._condition == 0 ? "not " : string.Empty;
				var text = this.Groups[id];
				list.Add(condition + text);
			}

			return intro + string.Join(CommaSpace, list);
		}

		public string? GetRulingFlags(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var flagList = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._rulingFlagUid._uid.id;
				var text = this.rulingFlags[id];
				if (item._enabled == 0)
				{
					text += " (disabled)";
				}

				flagList.Add(text);
			}

			return intro + ": " + string.Join(CommaSpace, flagList);
		}

		public string? GetPropConditions(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._propUid._uid.id;
				var text = id == 0
					? "any " + PropGroupSummaries[(int)item._anyOfPropsInGroup]
					: this.propData[id];
				var condition = item._condition == 0 ? "not " : string.Empty;
				list.Add(condition + text);
			}

			return intro + string.Join(CommaSpace, list);
		}

		public string? GetQuestConditions(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._questTemplateUid._uid.id;
				var text = this.quests[id];
				var condition = item._condition == 0 ? "not " : string.Empty;
				list.Add(condition + text);
			}

			return intro + string.Join(CommaSpace, list);
		}

		public string? GetTaskPools(string intro, dynamic item)
		{
			var id = (int)item._uid.id;
			if (id == 0)
			{
				return null;
			}

			var text = this.taskPools[id];
			return intro + text;
		}

		public string? GetTraits(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._traitUid._uid.id;
				var condition = item._condition == 0 ? "not " : string.Empty;
				var text = this.traits[id];
				list.Add(condition + text);
			}

			return intro + string.Join(CommaSpace, list);
		}
		#endregion

		#region Private Methods
		private void LoadGroups()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "GroupDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			string[] groupCats = ["_allSubjectsGroups", "_socialClassGroup", "_raceGroups"];
			foreach (var groupCat in groupCats)
			{
				var items = obj[groupCat];
				foreach (var item in items)
				{
					var id = (int)item._groupUid.id;
					var title = (string)item._displayName;
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

			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "PropDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._props;
			foreach (var item in items)
			{
				var id = (int)item._propUid.id;
				var title = (string)item._displayName;
				if (this.Translator.GetLanguageEntry(title) is string desc)
				{
					this.propData.Add(id, desc);
					var group = (int)item._propGroupData._propGroup;
					var list = this.propGroups[group];
					list.Add(desc);
				}
			}
		}

		private void LoadRulingFlags()
		{
			dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(GameInfo.Castles.ModFolder + "RulingsDefault2.json")) ?? throw new InvalidOperationException();
			var items = obj._rulingFlagDefinitions;
			foreach (var item in items)
			{
				var id = (int)item._rulingFlagUid.id;
				var name = (string)item._expressionName;
				this.rulingFlags.Add(id, name);
			}
		}

		private void LoadQuests()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "QuestData.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._quests;
			foreach (var item in items)
			{
				var id = (int)item._questUid.id;
				var title = (string)item._nameLocalizationKey;
				var desc = this.Translator.GetLanguageEntry(title) ?? (title + NotFound);
				this.quests.Add(id, desc);
			}
		}

		private void LoadSubjectArchetypes()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "SubjectArchetypesDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._subjectArchetypesList;
			foreach (var item in items)
			{
				// Not all names are language entries, so warnings coming from this function are normal.
				var id = (int)item._subjectArchetypeUid.id;
				var firstName = (string)item._firstname;
				if (firstName.Length > 0 && this.Translator.GetLanguageEntry(firstName) is string translationFirst)
				{
					firstName = translationFirst;
				}

				var lastName = (string)item._lastname;
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
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "TagDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._tags;
			foreach (var item in items)
			{
				var id = (int)item._tagUid.id;
				var desc = (string)item._description;
				this.Tags.Add(id, desc);
			}
		}

		private void LoadTaskPools()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "TasksDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._missionTaskPools;
			foreach (var item in items)
			{
				var id = (int)item._taskPoolUid.id;
				var title = (string)item._name;
				if (this.Translator.GetLanguageEntry(title) is string desc)
				{
					this.taskPools.Add(id, desc);
				}
			}
		}

		private void LoadTraits()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + "TraitDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			string[] traitCats = ["_dramaticTraits", "_jealousTraits", "_mightyTraits", "_deftTraits", "_lazyTraits", "_musicianTraits", "_academicTraits", "_generousTraits", "_bullyTraits", "_emotionalTraits", "_attentiveTraits", "_hauntedTraits", "_inspiringTraits", "_jesterTraits", "_pyromaniacTraits", "_stunningTraits", "_theatricalTraits", "_adamantTraits", "_preciseTraits", "_recklessTraits", "_leaderTraits", "_followerTraits", "_heartlessTraits", "_treacherousTraits", "_intenseTraits", "_gourmetTraits", "_influentialTraits", "_durableTraits", "_tribalTraits"];
			foreach (var traitCat in traitCats)
			{
				var items = obj[traitCat];
				foreach (var item in items)
				{
					var id = (int)item._traitUid.id;
					var title = (string)item._title;
					if (this.Translator.GetLanguageEntry(title) is string desc)
					{
						this.traits.Add(id, desc);
					}
				}
			}
		}
		#endregion
	}
}