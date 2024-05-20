﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;

	[method: JobInfo("Castles Rulings", "|Castles")]
	internal sealed class CastlesRulings(JobManager jobManager) : EditJob(jobManager)
	{
		#region Private Constants
		private const string NotFound = " - NOT FOUND!!!";
		private const string Language = "en-US";
		#endregion

		#region Fields
		private readonly Dictionary<int, string> archetypes = [];
		// private readonly Dictionary<int, string> eventReports = [];
		private readonly Dictionary<int, string> groups = [];
		private readonly List<string>[] propGroups = new List<string>[21];
		private readonly string[] propGroupSummaries = ["Kitchen", "Oil Press", "Smelter", "Loom", "Mill", "Workshop", "Sewing Table", "Smithy", "Bookshelf", "Music Station", "Art Studio", "Buffet", "Bath", "Throne", "Bed", "War Table", "Decoration", "Shrine of Mara", "Dragon Lair", string.Empty, "Gauntlet"];
		private readonly Dictionary<int, string> propData = [];
		private readonly Dictionary<int, string> quests = [];
		private readonly Dictionary<int, string> rulingFlags = [];
		private readonly Dictionary<string, dynamic> rulingsGroups = [];
		private readonly string[] rulingsGroupNames = ["_requiredRulings", "_randomRulings", "_personalRulings", "_instantRulings", "_rewardRulings"];
		private readonly string[] races = ["Argonian", "Breton", "Dark Elf", "High Elf", "Imperial", "Khajiit", "Nord", "Orc", "Redguard", "Wood Elf"];
		private readonly Dictionary<int, string> tags = [];
		private readonly Dictionary<int, string> taskPools = [];
		private readonly Dictionary<string, string[]> terms = new(StringComparer.Ordinal);
		private readonly Dictionary<int, string> traits = [];
		private readonly Dictionary<string, string> translations = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string[]> variations = new(StringComparer.Ordinal);
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			this.LoadTranslations(Language);
			this.LoadTerms(Language);
			this.LoadVariations(Language);

			// this.LoadEventReports();
			this.LoadGroups();
			this.LoadPropData();
			this.LoadQuests();
			this.LoadRulingGroups();
			this.LoadSubjectArchetypes();
			this.LoadTags();
			this.LoadTaskPools();
			this.LoadTraits();

			this.WriteFile(@"D:\Castles\Rulings.txt");
		}

		protected override string GetEditSummary(Page page) => "Update rulings";

		protected override void LoadPages()
		{
		}

		protected override void PageLoaded(Page page) => throw new NotImplementedException();
		#endregion

		#region Private Static Methods
		private static void AddBool(List<string> list, string text, dynamic value)
		{
			var valueInt = (int)value;
			if (valueInt != 0)
			{
				list.Add(text + "Yes");
			}
		}

		private static void AddDecimal(List<string> list, string text, dynamic value)
		{
			var valueFloat = (float)value;
			if (valueFloat != 0.0)
			{
				list.Add(text + valueFloat.ToString(CultureInfo.CurrentCulture));
			}
		}

		private static void AddInt(List<string> list, string text, dynamic value)
		{
			var valueInt = (int)value;
			if (valueInt != 0)
			{
				list.Add(text + valueInt.ToString("n0", CultureInfo.CurrentCulture));
			}
		}

		private static void AddResult(List<string> list, string result)
		{
			if (result is not null)
			{
				list.Add(result);
			}
		}

		private static TimeSpan DaysToYears(int time)
		{
			var ts = TimeSpan.FromSeconds(time);
			if (ts.Hours > 0 || ts.Minutes > 0 || ts.Seconds > 0)
			{
				throw new InvalidOperationException("Partial time: " + ts.ToString());
			}

			ts = TimeSpan.FromSeconds(time * 365);
			return ts;
		}

		private static bool DictEquals(Dictionary<int, int> x, Dictionary<int, int> y)
		{
			ArgumentNullException.ThrowIfNull(x);
			ArgumentNullException.ThrowIfNull(y);
			if (x.Count != y.Count)
			{
				return false;
			}

			foreach (var kvp in x)
			{
				if (kvp.Value != y[kvp.Key])
				{
					return false;
				}
			}

			return true;
		}

		private static string? GetRelationshipConditions(string intro, dynamic relationships)
		{
			if (relationships.Count == 0)
			{
				return null;
			}

			string[] otherLookup = ["Ruler", "Requester", "Co-Requester"];
			string[] relationshipLookup = ["Parent", "Child", "Sibling", "Aunt/Uncle", "Nephew/Niece", "Cousin", "Grandparent", "Grandchild", "Spouse", "Friend", "Lover", "Enemy"];
			var list = new List<string>();
			foreach (var relationship in relationships)
			{
				var value = relationshipLookup[(int)relationship._relationshipToOtherSubject];
				var subject = otherLookup[(int)relationship._otherSubject];
				var condition = (int)relationship._condition == 0 ? "not " : string.Empty;
				list.Add($"relationship to {subject} is {condition}{value}");
			}

			return intro + string.Join(", ", list);
		}

		private static string? GetRulerAssassinationChance(dynamic choice)
		{
			var chance = (int)choice._rulerAssassinationPercentChance;
			if (chance == 0)
			{
				return null;
			}

			var result = "Ruler Assassination Chance: " + chance.ToString("n0", CultureInfo.CurrentCulture) + '%';
			var maxDelay = (int)choice._rulerAssassinationMaxDelaySecs;
			var minDelay = (int)choice._rulerAssassinationMinDelaySecs;
			var range = GetRangeText(minDelay, maxDelay);
			if (maxDelay != 0 || minDelay != 0)
			{
				result += $" (delay: {range} seconds)";
			}

			return result;
		}

		private static string GetRangeText(int from, int to) => GetRangeText(from, to, string.Empty, string.Empty);

		private static string GetRangeText(int from, int to, string fromText, string toText) => from == to
			? fromText + from.ToStringInvariant()
			: toText + from.ToStringInvariant() + '-' + to.ToStringInvariant();

		private static string GetTime(TimeSpan time)
		{
			var times = new List<string>();
			if (time.Days >= 365)
			{
				var years = time.Days / 365;
				time = time.Subtract(TimeSpan.FromDays(years * 365));
				var text = years.ToStringInvariant() + " year";
				if (years != 1)
				{
					text += 's';
				}

				times.Add(text);
			}

			if (time.Days > 0 || times.Count > 0)
			{
				var text = time.Days.ToStringInvariant() + " day";
				if (time.Days != 1)
				{
					text += 's';
				}

				times.Add(text);
			}

			if (time.Hours > 0 || times.Count > 0)
			{
				var text = time.Hours.ToStringInvariant() + " hour";
				if (time.Hours != 1)
				{
					text += 's';
				}

				times.Add(text);
			}

			if (time.Minutes > 0 || times.Count > 0)
			{
				var text = time.Minutes.ToStringInvariant() + " minute";
				if (time.Minutes != 1)
				{
					text += 's';
				}

				times.Add(text);
			}

			// Don't add seconds if == 0 because it'll just get stripped right back off again, UNLESS it's the only unit there is.
			if (time.Seconds > 0 || times.Count == 0)
			{
				var text = time.Seconds.ToStringInvariant() + " second";
				if (time.Seconds != 1)
				{
					text += 's';
				}

				times.Add(text);
			}

			while (times.Count > 1 && times[0].StartsWith('0'))
			{
				times.RemoveAt(0);
			}

			while (times.Count > 1 && times[^1].StartsWith('0'))
			{
				times.RemoveAt(times.Count - 1);
			}

			return string.Join(", ", times);
		}
		#endregion

		#region Private Methods
		private void AddActivationConditions(List<string> list, dynamic conditions)
		{
			var time = (double)conditions._minRecurrenceTimeSec;
			if (time > 0)
			{
				var ts = TimeSpan.FromSeconds(time);
				AddResult(list, "Min. Recurrence Time: " + GetTime(ts));
			}

			var id = (int)conditions._groupUid._uid.id;
			if (id != 0)
			{
				AddResult(list, "Group: " + this.groups[id]);
			}

			AddInt(list, "Group Happiness > ", conditions._groupHappinessGreaterThan);
			AddInt(list, "Group Happiness < ", conditions._groupHappinessLessThan);
			this.AddTags(list, '>', conditions._perCastleLevelTagQuantitiesGreaterThan);
			this.AddTags(list, '<', conditions._perCastleLevelTagQuantitiesLessThan);
			AddInt(list, "Castle Level > ", conditions._castleLevelGreaterThan);
			AddInt(list, "Castle Level < ", conditions._castleLevelLessThan);
			AddResult(list, this.GetRulingFlags("Ruling Flags", conditions._rulingFlags));
			this.AddGenericConditions(list, "Ruler Conditions", conditions._rulerConditions);
			AddInt(list, "Alliance Member: ", conditions._allianceMember);
			AddResult(list, this.GetPropConditions("Placed Prop - Any of: ", conditions._anyOfPlacedPropConditions));
			AddResult(list, this.GetPropConditions("Placed Prop - All of: ", conditions._allOfPlacedPropConditions));
			//// AddResult(list, this.GetEventConditions("Castle Events - Any of: ", conditions._anyOfCastleWideEventConditions));
			//// AddResult(list, this.GetEventConditions("Castle Events - All of: ", conditions._allOfCastleWideEventConditions));
			AddResult(list, this.GetArchetypes("Present in Castle - Any of: ", conditions._anyOfPresentInCastleSubjectArchetypeConditions));
			AddResult(list, this.GetArchetypes("Present in Castle - All of: ", conditions._allOfPresentInCastleSubjectArchetypeConditions));
			AddInt(list, "Oil% > ", conditions._oilPercentageGreaterThan);
			AddInt(list, "Oil% < ", conditions._oilPercentageLessThan);
			AddInt(list, "Food% > ", conditions._foodPercentageGreaterThan);
			AddInt(list, "Food% < ", conditions._foodPercentageLessThan);
			AddResult(list, this.GetQuestConditions("Quests - Any of: ", conditions._anyOfCompletedQuestConditions));
			AddResult(list, this.GetQuestConditions("Quests - All of: ", conditions._allOfCompletedQuestConditions));
			//// Ignored as unused: _inProgressUnclaimedTask
			AddResult(list, this.GetTaskPools("In-progress unclaimed task pool: ", conditions._inProgressUnclaimedTaskPool));
		}

		private void AddGenericConditions(List<string> list, string intro, dynamic conditions)
		{
			intro += " - ";
			AddInt(list, intro + "Gender: ", conditions._gender);
			var time = (int)conditions._ageGreaterThanSecs;
			if (time > 0)
			{
				var ts = DaysToYears(time);
				var text = GetTime(ts);
				AddResult(list, $"{intro}Age > {text}");
			}

			time = (int)conditions._ageLessThanSecs;
			if (time > 0)
			{
				var sign = "<";
				if (time % 60 == 1)
				{
					sign += '=';
					time--;
				}

				var ts = DaysToYears(time);
				var text = GetTime(ts);
				AddResult(list, $"{intro}Age {sign} {text}");
			}

			AddInt(list, intro + "Happiness > ", conditions._happinessGreaterThan);
			AddInt(list, intro + "Happiness < ", conditions._happinessLessThan);
			AddResult(list, this.GetRulingFlags(intro + "Subject Ruling Flags", conditions._subjectRulingFlags));
			AddResult(list, this.GetTraits(intro + "Traits include any of: ", conditions._anyOfTraitConditions));
			AddResult(list, this.GetTraits(intro + "Traits include all of: ", conditions._allOfTraitConditions));
			AddResult(list, this.GetRaces(intro + "Race is ", conditions._anyOfRaceConditions));
			AddResult(list, this.GetRaces(intro + "Race is not ", conditions._allOfRaceConditions));
			AddResult(list, this.GetPropConditions(intro + "Props: ", conditions._anyOfAssignedToPropConditions));
			AddResult(list, this.GetPropConditions(intro + "Props: ", conditions._allOfAssignedToPropConditions));
			AddResult(list, this.GetArchetypes(intro + "Any of: ", conditions._anyOfArchetypeConditions));
			AddResult(list, this.GetArchetypes(intro + "Not any of: ", conditions._allOfArchetypeConditions));
			AddResult(list, GetRelationshipConditions(intro + "Any of: ", conditions._anyOfRelationshipConditions));
			AddResult(list, GetRelationshipConditions(intro + "All of: ", conditions._allOfRelationshipConditions));
			AddInt(list, intro + "Last Subject Killer: ", conditions._lastSubjectKiller);
			AddResult(list, this.GetGroups(intro + "Group any of: ", conditions._anyOfGroupConditions));
			AddResult(list, this.GetGroups(intro + "Group all of: ", conditions._allOfGroupConditions));
			AddResult(list, this.GetPropConditions(intro + "Requester any of same props: ", conditions._anyOfRequesterAssignedToSamePropConditions));
		}

		private void AddTags(List<string> list, char sign, dynamic items)
		{
			if (items.Count == 0)
			{
				return;
			}

			var last = new Dictionary<int, int>();
			var minLevel = 1;
			string desc;
			string levels;
			int level;
			var index = 0;
			var current = new Dictionary<int, int>();
			do
			{
				var item = items[index];
				level = item._castleLevel;
				var tagQuantities = item._tagQuantities;
				current.Clear();
				foreach (var tag in tagQuantities)
				{
					var tagId = (int)tag._anyOfItemsWithTagId._uid.id;
					var quantity = (int)tag._quantity;
					current[tagId] = quantity;
				}

				if (index == 0)
				{
					last = current;
				}

				if (!DictEquals(last, current))
				{
					levels = GetRangeText(minLevel, level - 1, "level ", "levels ");
					foreach (var entry in last)
					{
						desc = this.tags[entry.Key];
						list.Add($"{desc} {sign} {entry.Value} ({levels})");
					}

					last = current;
					minLevel = level + 1;
				}

				index++;
			}
			while (index < items.Count);

			levels = GetRangeText(minLevel, level, "level ", "levels ");
			foreach (var entry in current)
			{
				desc = this.tags[entry.Key];
				list.Add($"{desc} {sign} {entry.Value} ({levels})");
			}
		}

		private string? GetArchetypes(string intro, dynamic items)
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

			return intro + string.Join(", ", list);
		}

		private string? GetGroups(string intro, dynamic items)
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
				var text = this.groups[id];
				list.Add(condition + text);
			}

			return intro + string.Join(", ", list);
		}

		/*
		private string? GetEventConditions(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._castleWideEventUid._uid.id;
				var text = this.eventReports[id];
				var condition = item._condition == 0 ? "not " : string.Empty;
				list.Add(condition + text);
			}

			return intro + string.Join(", ", list);
		}
		*/

		private string? GetPropConditions(string intro, dynamic items)
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
					? "any " + this.propGroupSummaries[(int)item._anyOfPropsInGroup]
					: this.propData[id];
				var condition = item._condition == 0 ? "not " : string.Empty;
				list.Add(condition + text);
			}

			return intro + string.Join(", ", list);
		}

		private string? GetQuestConditions(string intro, dynamic items)
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

			return intro + string.Join(", ", list);
		}

		private string? GetRaces(string intro, dynamic items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			var list = new List<string>();
			foreach (var item in items)
			{
				var id = (int)item._race;
				var text = this.races[id];
				list.Add(text);
			}

			return intro + string.Join(", ", list);
		}

		private string? GetRulingFlags(string intro, dynamic items)
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

			return intro + ": " + string.Join(", ", flagList);
		}

		private string? GetTaskPools(string intro, dynamic item)
		{
			var id = (int)item._uid.id;
			if (id == 0)
			{
				return null;
			}

			var text = this.taskPools[id];
			return intro + text;
		}

		private string? GetUniqueTerms(string id)
		{
			if (!this.terms.TryGetValue(id, out var term))
			{
				return null;
			}

			var unique = new List<string>();
			foreach (var item in term)
			{
				if (!unique.Contains(item, StringComparer.Ordinal))
				{
					unique.Add(item);
				}
			}

			return string.Join(" / ", unique);
		}

		private string? GetTerm(string id, bool isMale, bool isPlural)
		{
			var index = (isMale ? 0 : 2) + (isPlural ? 1 : 0);
			return !this.terms.TryGetValue(id, out var term)
				? null
				: term[index];
		}

		private string? GetTranslation(string id)
		{
			_ = this.translations.TryGetValue(id, out var retval);
			return retval;
		}

		private string? GetTraits(string intro, dynamic items)
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

			return intro + string.Join(", ", list);
		}

		private void LoadGroups()
		{
			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\GroupDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			string[] groupCats = ["_allSubjectsGroups", "_socialClassGroup", "_raceGroups"];
			foreach (var groupCat in groupCats)
			{
				var items = obj[groupCat];
				foreach (var item in items)
				{
					var id = (int)item._groupUid.id;
					var title = (string)item._displayName;
					var desc = this.translations[title];
					this.groups.Add(id, desc);
				}
			}
		}

		private void LoadPropData()
		{
			for (var i = 0; i <= 20; i++)
			{
				this.propGroups[i] = [];
			}

			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\PropDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._props;
			foreach (var item in items)
			{
				var id = (int)item._propUid.id;
				var title = (string)item._displayName;
				var desc = this.translations[title];
				this.propData.Add(id, desc);
				var group = (int)item._propGroupData._propGroup;
				var list = this.propGroups[group];
				list.Add(desc);
			}
		}

		/*
		private void LoadEventReports()
		{
			dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(@"D:\Castles\MonoBehaviour\ReportEntriesDataDefault2.json")) ?? throw new InvalidOperationException();
			foreach (var item in obj._castleWideEventReportEntries)
			{
				var id = (int)item._eventUid._uid.id;
				var title = (string)item._reportMessage;
				var desc = this.translations[title];
				this.eventReports.TryAdd(id, desc); // TryAdd due to identical duplicate entry
			}
		}
		*/

		private void LoadRulingGroups()
		{
			dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(@"D:\Castles\MonoBehaviour\RulingsDefault2.json")) ?? throw new InvalidOperationException();
			foreach (var item in obj._rulingFlagDefinitions)
			{
				var id = (int)item._rulingFlagUid.id;
				var name = (string)item._expressionName;
				this.rulingFlags.Add(id, name);
			}

			foreach (var rulingsGroupName in this.rulingsGroupNames)
			{
				this.rulingsGroups.Add(rulingsGroupName, obj[rulingsGroupName]);
			}
		}

		private void LoadQuests()
		{
			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\QuestData.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._quests;
			foreach (var item in items)
			{
				var id = (int)item._questUid.id;
				var title = (string)item._nameLocalizationKey;
				if (!this.translations.TryGetValue(title, out var desc))
				{
					desc = title + NotFound;
				}

				this.quests.Add(id, desc);
			}
		}

		private void LoadSubjectArchetypes()
		{
			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\SubjectArchetypesDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._subjectArchetypesList;
			foreach (var item in items)
			{
				var id = (int)item._subjectArchetypeUid.id;
				var firstName = (string)item._firstname;
				if (firstName.Length > 0 && this.translations.TryGetValue(firstName, out var translation))
				{
					firstName = translation;
				}

				var lastName = (string)item._lastname;
				lastName = lastName.Replace("\u200B", string.Empty, StringComparison.Ordinal);
				if (lastName.Length > 0 && this.translations.TryGetValue(lastName, out translation))
				{
					lastName = translation;
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
			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\TagDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._tags;
			foreach (var item in items)
			{
				var id = (int)item._tagUid.id;
				var desc = (string)item._description;
				this.tags.Add(id, desc);
			}
		}

		private void LoadTaskPools()
		{
			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\TasksDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._missionTaskPools;
			foreach (var item in items)
			{
				var id = (int)item._taskPoolUid.id;
				var title = (string)item._name;
				var desc = this.translations[title];
				this.taskPools.Add(id, desc);
			}
		}

		private void LoadTraits()
		{
			var text = File.ReadAllText(@"D:\Castles\MonoBehaviour\TraitDataDefault2.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			string[] traitCats = ["_dramaticTraits", "_jealousTraits", "_mightyTraits", "_deftTraits", "_lazyTraits", "_musicianTraits", "_academicTraits", "_generousTraits", "_bullyTraits", "_emotionalTraits", "_attentiveTraits", "_hauntedTraits", "_inspiringTraits", "_jesterTraits", "_pyromaniacTraits", "_stunningTraits", "_theatricalTraits", "_adamantTraits", "_preciseTraits", "_recklessTraits", "_leaderTraits", "_followerTraits", "_heartlessTraits", "_treacherousTraits", "_intenseTraits", "_gourmetTraits", "_influentialTraits", "_durableTraits", "_tribalTraits"];
			foreach (var traitCat in traitCats)
			{
				var items = obj[traitCat];
				foreach (var item in items)
				{
					var id = (int)item._traitUid.id;
					var title = (string)item._title;
					var desc = this.translations[title];
					this.traits.Add(id, desc);
				}
			}
		}

		private void LoadTerms(string language)
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\terms_{language}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				var data = (string)item._data;
				var split = data.Split(TextArrays.Pipe);
				this.terms.Add((string)item._referenceId, split);
			}
		}

		private void LoadTranslations(string language)
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\sentences_{language}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				this.translations.Add((string)item._referenceId, (string)item._data);
			}

			text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\terms_{language}.json");
			obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			items = obj._rawData;
			foreach (var item in items)
			{
				var data = (string)item._data;
				var split = data.Split(TextArrays.Comma);
				var varList = new SortedSet<string>(split, StringComparer.Ordinal);
				this.translations.Add((string)item._referenceId, string.Join('/', varList));
			}

			text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\LanguageDatabase_Gameplay_{language}.json");
			obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			items = obj.entries;
			foreach (var item in items)
			{
				this.translations.TryAdd((string)item.key, (string)item.entry);
			}
		}

		private void LoadVariations(string language)
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\variations_{language}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				var data = (string)item._data;
				var split = data.Split(TextArrays.Comma);
				var varList = new List<string>(split.Length);
				foreach (var id in split)
				{
					var unique = this.GetTranslation(id) ?? this.GetUniqueTerms(id);
					if (unique != null)
					{
						varList.Add(unique);
					}
				}

				this.variations.Add((string)item._referenceId, [.. varList]);
			}
		}

		private void WriteChoice(IndentedTextWriter stream, dynamic choice)
		{
			var choiceId = (string)choice._rulingChoiceDescription;
			if (!this.translations.TryGetValue(choiceId, out var choiceDesc))
			{
				choiceDesc = choiceId + NotFound;
			}

			stream.WriteLine("* " + choiceDesc);
			stream.Indent++;
			var list = new List<string>();

			this.AddActivationConditions(list, choice._rulingChoiceActivationConditions);
			AddResult(list, GetRulerAssassinationChance(choice));
			AddResult(list, this.GetRulingFlags("Effect Flag Conditions", choice._effectRulingFlags));
			AddResult(list, this.GetRulingFlags("Effect Requester Flags", choice._effectRequesterRulingFlags));
			AddResult(list, this.GetRulingFlags("Effect Co-Requester Flags", choice._effectCoRequesterRulingFlags));
			AddResult(list, this.GetRulingFlags("Effect Flags Changed", choice._effectRulerRulingFlags));
			this.AddGenericConditions(list, "Requester", choice._requesterConditions);
			this.AddGenericConditions(list, "Co-Requester", choice._coRequesterConditions);
			var killChance = (int)choice._subjectKillPercentChance;
			if (killChance > 0)
			{
				AddResult(list, "Subject Kill Chance: " + killChance.ToStringInvariant() + '%');
			}

			AddInt(list, "Subject Killer: ", choice._subjectKiller);
			AddInt(list, "Subject Killed: ", choice._subjectKilled);
			var minDelay = (int)choice._subjectKillMinDelaySecs;
			var maxDelay = (int)choice._subjectKillMaxDelaySecs;
			var range = GetRangeText(minDelay, maxDelay);
			if (!string.Equals(range, "0", StringComparison.Ordinal))
			{
				AddResult(list, $"Subject Kill Delay: {range} seconds");
			}

			AddBool(list, "Allow Costs to Reduce to Inventory Amount: ", choice._allowCostsToReduceToInventoryAmount);
			AddInt(list, "Send Next Subject to Throne Line Delay: ", choice._sendNextSubjectToThroneLineDelay);

			foreach (var entry in list)
			{
				stream.WriteLine(entry);
			}

			stream.Indent--;
		}

		private void WriteFile(string fileName)
		{
			using var baseStream = File.CreateText(fileName);
			using var stream = new IndentedTextWriter(baseStream);
			foreach (var name in this.rulingsGroupNames)
			{
				stream.WriteLine(name);
				stream.Indent++;
				foreach (var ruling in this.rulingsGroups[name])
				{
					this.WriteRuling(stream, ruling);
				}

				stream.Indent--;
			}

			stream.Close();
			baseStream.Close();
		}

		private void WriteRuling(IndentedTextWriter stream, dynamic ruling)
		{
			var descId = (string)ruling._rulingDescription;
			stream.WriteLine(this.translations[descId]);
			stream.Indent++;
			this.WriteRulingConditions(stream, ruling);
			foreach (var choice in ruling._rulingChoices)
			{
				this.WriteChoice(stream, choice);
			}

			stream.Indent--;
			stream.WriteLine();
		}

		private void WriteRulingConditions(IndentedTextWriter stream, dynamic ruling)
		{
			// Different ruling groups have different conditions, but follow the same overall structure, so check each property for existence.
			var list = new List<string>();
			if (ruling._rulingActivationConditions is not null)
			{
				this.AddActivationConditions(list, ruling._rulingActivationConditions);
			}

			if (ruling._requesterConditions is not null)
			{
				this.AddGenericConditions(list, "Ruling Requester", ruling._requesterConditions);
			}

			if (ruling._coRequesterConditions is not null)
			{
				this.AddGenericConditions(list, "Ruling Co-requester", ruling._coRequesterConditions);
			}

			if (ruling._randomWeight is not null)
			{
				AddDecimal(list, "Random Weight: ", ruling._randomWeight);
			}

			if (ruling._trigger is not null)
			{
				AddInt(list, "Trigger: ", ruling._trigger);
			}

			if (list.Count > 0)
			{
				foreach (var entry in list)
				{
					stream.WriteLine(entry);
				}
			}
		}
		#endregion
	}
}