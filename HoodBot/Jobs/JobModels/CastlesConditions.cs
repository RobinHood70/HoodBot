namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon;

internal sealed class CastlesConditions(CastlesData data, CultureInfo gameCulture) : List<string>
{
	#region Public Propeties
	public List<string> EffectFlags { get; } = [];
	#endregion

	#region Public Methods
	public void AddChoiceInfo(JToken choice)
	{
		this.AddActivationConditions(choice.MustHave("_rulingChoiceActivationConditions"));
		this.AddResult(this.GetRulerAssassinationChance(choice));
		this.AddEffectFlagsResult(data.GetRulingFlags("Effect Flags", choice.MustHave("_effectRulingFlags")));
		this.AddEffectFlagsResult(data.GetRulingFlags("Ruler Flags", choice.MustHave("_effectRulerRulingFlags")));
		this.AddEffectFlagsResult(data.GetRulingFlags("Requester Flags", choice.MustHave("_effectRequesterRulingFlags")));
		this.AddEffectFlagsResult(data.GetRulingFlags("Co-Requester Flags", choice.MustHave("_effectCoRequesterRulingFlags")));
		this.AddGenericConditions("Requester ", choice.MustHave("_requesterConditions"));
		this.AddGenericConditions("Co-Requester ", choice.MustHave("_coRequesterConditions"));
		var killChance = choice.MustHaveInt("_subjectKillPercentChance");
		if (killChance > 0)
		{
			this.AddResult("Subject Kill Chance: " + killChance.ToStringInvariant() + '%');
		}

		this.AddInt("Subject Killer: ", choice.MustHave("_subjectKiller"));
		this.AddInt("Subject Killed: ", choice.MustHave("_subjectKilled"));
		var range = GetRangeText(choice.MustHaveInt("_subjectKillMinDelaySecs"), choice.MustHaveInt("_subjectKillMaxDelaySecs"));
		if (range is not null)
		{
			this.AddResult($"Subject Kill Delay: {range} seconds");
		}

		this.AddBool("Allow Costs to Reduce to Inventory Amount: ", choice.MustHave("_allowCostsToReduceToInventoryAmount"));
		this.AddInt("Send Next Subject to Throne Line Delay: ", choice.MustHave("_sendNextSubjectToThroneLineDelay"));
	}

	public void AddRulingInfo(JToken ruling)
	{
		// Different ruling groups have different conditions, but follow the same overall structure, so check each property for existence.
		if (ruling["_rulingActivationConditions"] is not null)
		{
			this.AddActivationConditions(ruling.MustHave("_rulingActivationConditions"));
		}

		if (ruling["_requesterConditions"] is not null)
		{
			this.AddGenericConditions("Ruling Requester ", ruling.MustHave("_requesterConditions"));
		}

		if (ruling["_coRequesterConditions"] is not null)
		{
			this.AddGenericConditions("Ruling Co-requester ", ruling.MustHave("_coRequesterConditions"));
		}

		if (ruling["_randomWeight"] is not null)
		{
			this.AddDecimal("Random Weight: ", ruling.MustHave("_randomWeight"));
		}

		if (ruling["_trigger"] is not null)
		{
			this.AddInt("Trigger: ", ruling.MustHave("_trigger"));
		}
	}
	#endregion

	#region Private Static Methods
	private static TimeSpan DaysToYears(int time)
	{
		var add = 0;
		if (time > 1 && time % 60 == 1)
		{
			// Preserve X+1s concept instead of multiplying it by 365.
			add = 1;
			time--;
		}

		var ts = TimeSpan.FromSeconds(time);
		if (ts.Hours > 0 || ts.Minutes > 0 || ts.Seconds > 0)
		{
			throw new InvalidOperationException("Partial time: " + ts.ToString());
		}

		ts = TimeSpan.FromSeconds(time * 365);
		ts.Add(TimeSpan.FromSeconds(add));
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

		foreach (var (key, value) in x)
		{
			if (!y.TryGetValue(key, out var result) || (value != result))
			{
				return false;
			}
		}

		return true;
	}

	private static string? GetCorrectedRangeText(int minLevel, int maxLevel, int castleLevelGT, int castleLevelLT)
	{
		if (castleLevelGT > 0 && maxLevel >= 0 && maxLevel <= castleLevelGT)
		{
			// Conditions not possible
			return null;
		}

		if (castleLevelLT > 0 && minLevel >= castleLevelLT)
		{
			// Conditions not possible
			return null;
		}

		if (maxLevel == 200)
		{
			maxLevel = int.MaxValue;
		}
		else if (castleLevelLT != 0 && maxLevel >= castleLevelLT)
		{
			maxLevel = castleLevelLT - 1;
		}
		else

		if (castleLevelGT != 0 && minLevel <= castleLevelGT)
		{
			minLevel = castleLevelGT + 1;
		}

		return maxLevel == -1
			? string.Empty
			: " (" + GetRangeText(minLevel, maxLevel, "level ", "levels ", string.Empty) + ')';
	}

	private static string? GetRangeText(int from, int to) => GetRangeText(from, to, string.Empty, string.Empty, string.Empty);

	private static string? GetRangeText(int from, int to, string singleEntryText, string rangeText, string trailer)
	{
		var fromIsMin = from is 0 or int.MinValue;
		var toIsMax = to is 0 or int.MaxValue;
		return
			fromIsMin && toIsMax ? null :
			to == from ? singleEntryText + from.ToStringInvariant() :
			fromIsMin ? $"{rangeText}<= {to.ToStringInvariant()}{trailer}" :
			toIsMax ? $"{rangeText}>= {from.ToStringInvariant()}{trailer}" :
			$"{rangeText}{from.ToStringInvariant()}-{to.ToStringInvariant()}{trailer}";
	}

	private static string? GetRangeTextGTLT(int gt, int lt, string trailer)
	{
		if (gt != 0)
		{
			gt++;
		}

		if (lt != 0)
		{
			lt--;
		}

		return GetRangeText(gt, lt, string.Empty, string.Empty, trailer);
	}

	private static string? GetRelationshipConditions(string intro, JToken relationships)
	{
		var list = new List<string>();
		foreach (var relationship in relationships)
		{
			var value = CastlesTranslator.GetRelatonship(relationship.MustHaveInt("_relationshipToOtherSubject"));
			var subject = CastlesTranslator.GetRequester(relationship.MustHaveInt("_otherSubject"));
			var condition = relationship.MustHaveInt("_condition") == 0 ? "not " : string.Empty;
			list.Add($"relationship to {subject} is {condition}{value}");
		}

		return list.Count == 0
			? null
			: intro + string.Join(CastlesData.CommaSpace, list);
	}

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

		return string.Join(CastlesData.CommaSpace, times);
	}
	#endregion

	#region Private Methods
	private void AddActivationConditions(JToken conditions)
	{
		var time = conditions.MustHaveDouble("_minRecurrenceTimeSec");
		if (time > 0)
		{
			var ts = TimeSpan.FromSeconds(time);
			this.AddResult("Min. Recurrence Time: " + GetTime(ts));
		}

		var id = conditions.MustHave("_groupUid").MustHave("_uid").MustHaveInt("id");
		if (id != 0)
		{
			this.AddResult("Group: " + data.Groups[id]);
		}

		this.AddResult("Group Happiness ", GetRangeTextGTLT(conditions.MustHaveInt("_groupHappinessGreaterThan"), conditions.MustHaveInt("_groupHappinessLessThan"), string.Empty));
		this.AddTags('>', conditions.MustHaveInt("_castleLevelGreaterThan"), conditions.MustHaveInt("_castleLevelLessThan"), conditions.MustHave("_perCastleLevelTagQuantitiesGreaterThan"));
		this.AddTags('<', conditions.MustHaveInt("_castleLevelGreaterThan"), conditions.MustHaveInt("_castleLevelLessThan"), conditions.MustHave("_perCastleLevelTagQuantitiesLessThan"));
		this.AddResult("Dynasty Level ", GetRangeTextGTLT(conditions.MustHaveInt("_castleLevelGreaterThan"), conditions.MustHaveInt("_castleLevelLessThan"), string.Empty));
		this.AddResult(data.GetRulingFlags("Ruling Flags", conditions.MustHave("_rulingFlags")));
		this.AddGenericConditions("Ruler ", conditions.MustHave("_rulerConditions"));
		this.AddBool("Alliance Member: ", conditions.MustHave("_allianceMember"));
		this.AddResult(data.GetPropConditions("Placed Prop - Any of: ", conditions.MustHave("_anyOfPlacedPropConditions")));
		this.AddResult(data.GetPropConditions("Placed Prop - All of: ", conditions.MustHave("_allOfPlacedPropConditions")));
		//// AddResult(list, this.GetEventConditions("Castle Events - Any of: ", conditions.MustHave("_anyOfCastleWideEventConditions")));
		//// AddResult(list, this.GetEventConditions("Castle Events - All of: ", conditions.MustHave("_allOfCastleWideEventConditions")));
		this.AddResult(data.GetArchetypes("Present in Castle - Any of: ", conditions.MustHave("_anyOfPresentInCastleSubjectArchetypeConditions")));
		this.AddResult(data.GetArchetypes("Present in Castle - All of: ", conditions.MustHave("_allOfPresentInCastleSubjectArchetypeConditions")));
		this.AddResult("Oil ", GetRangeTextGTLT(conditions.MustHaveInt("_oilPercentageGreaterThan"), conditions.MustHaveInt("_oilPercentageLessThan"), "%"));
		this.AddResult("Food ", GetRangeTextGTLT(conditions.MustHaveInt("_foodPercentageGreaterThan"), conditions.MustHaveInt("_foodPercentageLessThan"), "%"));
		this.AddResult(data.GetQuestConditions("Quests - Any of: ", conditions.MustHave("_anyOfCompletedQuestConditions")));
		this.AddResult(data.GetQuestConditions("Quests - All of: ", conditions.MustHave("_allOfCompletedQuestConditions")));
		//// Ignored as unused: _inProgressUnclaimedTask
		this.AddResult(data.GetTaskPools("In-progress unclaimed task pool: ", conditions.MustHave("_inProgressUnclaimedTaskPool")));
	}

	private void AddBool(string text, JToken value)
	{
		if ((int)value != 0)
		{
			this.Add(text + "Yes");
		}
	}

	private void AddConditionText(Dictionary<int, int> entries, char sign, string levels)
	{
		foreach (var entry in entries)
		{
			var desc = data.Tags[entry.Key].Trim();
			this.Add($"{desc} {sign} {entry.Value}{levels}");
		}
	}

	private void AddDecimal(string text, JToken value)
	{
		var valueFloat = (float)value;
		if (valueFloat != 0.0)
		{
			this.Add(text + valueFloat.ToString(gameCulture));
		}
	}

	private void AddEffectFlagsResult(string? result)
	{
		if (result is not null)
		{
			this.EffectFlags.Add(result);
		}
	}

	private void AddGenericConditions(string intro, JToken conditions)
	{
		this.AddInt(intro + "gender is ", conditions.MustHave("_gender"));
		var time = conditions.MustHaveInt("_ageGreaterThanSecs");
		if (time > 0)
		{
			var ts = DaysToYears(time);
			var text = GetTime(ts);
			this.AddResult($"{intro}is older than {text}");
		}

		time = conditions.MustHaveInt("_ageLessThanSecs");
		if (time > 0)
		{
			var ts = DaysToYears(time);
			var text = GetTime(ts);
			this.AddResult($"{intro}is younger than {text}");
		}

		this.AddResult(intro + "Happiness ", GetRangeTextGTLT(conditions.MustHaveInt("_happinessGreaterThan"), conditions.MustHaveInt("_happinessLessThan"), string.Empty));
		this.AddResult(data.GetRulingFlags(intro + "Subject Ruling Flags", conditions.MustHave("_subjectRulingFlags")));
		this.AddResult(data.GetTraits(intro + "Traits include any of: ", conditions.MustHave("_anyOfTraitConditions")));
		this.AddResult(data.GetTraits(intro + "Traits include all of: ", conditions.MustHave("_allOfTraitConditions")));
		this.AddResult(CastlesData.GetRaces(intro + "Race is ", conditions.MustHave("_anyOfRaceConditions")));
		this.AddResult(CastlesData.GetRaces(intro + "Race is not ", conditions.MustHave("_allOfRaceConditions")));
		this.AddResult(data.GetPropConditions(intro + "Props: ", conditions.MustHave("_anyOfAssignedToPropConditions")));
		this.AddResult(data.GetPropConditions(intro + "Props: ", conditions.MustHave("_allOfAssignedToPropConditions")));
		this.AddResult(data.GetArchetypes(intro + "Any of: ", conditions.MustHave("_anyOfArchetypeConditions")));
		this.AddResult(data.GetArchetypes(intro + "Not any of: ", conditions.MustHave("_allOfArchetypeConditions")));
		this.AddResult(GetRelationshipConditions(intro + "Any of: ", conditions.MustHave("_anyOfRelationshipConditions")));
		this.AddResult(GetRelationshipConditions(intro + "All of: ", conditions.MustHave("_allOfRelationshipConditions")));
		this.AddInt(intro + "Last Subject Killer: ", conditions.MustHave("_lastSubjectKiller"));
		this.AddResult(data.GetGroups(intro + "Group any of: ", conditions.MustHave("_anyOfGroupConditions")));
		this.AddResult(data.GetGroups(intro + "Group all of: ", conditions.MustHave("_allOfGroupConditions")));
		this.AddResult(data.GetPropConditions(intro + "Requester any of same props: ", conditions.MustHave("_anyOfRequesterAssignedToSamePropConditions")));
	}

	private void AddInt(string text, JToken value)
	{
		var valueInt = (int)value;
		if (valueInt != 0)
		{
			this.Add(text + valueInt.ToString("n0", gameCulture));
		}
	}

	private void AddResult(string? result)
	{
		if (result is not null)
		{
			this.Add(result);
		}
	}

	private void AddResult(string intro, string? result)
	{
		if (result is not null)
		{
			this.Add(intro + result);
		}
	}

	private void AddTags(char sign, int castleLevelGT, int castleLevelLT, JToken items)
	{
		var last = new Dictionary<int, int>();
		var minLevel = 1;
		var level = 0;
		var index = 0;
		var current = new Dictionary<int, int>();
		var listCount = this.Count;
		foreach (var item in items)
		{
			level = item.MustHaveInt("_castleLevel");
			var tagQuantities = item.MustHave("_tagQuantities");
			current.Clear();
			foreach (var tag in tagQuantities)
			{
				var tagId = tag.MustHave("_anyOfItemsWithTagId").MustHave("_uid").MustHaveInt("id");
				var quantity = tag.MustHaveInt("_quantity");
				current[tagId] = quantity;
			}

			if (index == 0)
			{
				foreach (var kvp in current)
				{
					last[kvp.Key] = kvp.Value;
				}
			}

			if (!DictEquals(last, current))
			{
				var levels1 = GetCorrectedRangeText(minLevel, level - 1, castleLevelGT, castleLevelLT);
				if (levels1 is not null)
				{
					this.AddConditionText(last, sign, levels1);
				}

				last.Clear();
				foreach (var kvp in current)
				{
					last[kvp.Key] = kvp.Value;
				}

				minLevel = level;
			}

			index++;
		}

		if (this.Count == listCount && level == 1)
		{
			level = -1;
		}
		else if (level < castleLevelLT)
		{
			level = castleLevelLT;
		}

		var levels2 = GetCorrectedRangeText(minLevel, level, castleLevelGT, castleLevelLT);
		if (levels2 is not null)
		{
			this.AddConditionText(current, sign, levels2);
		}
	}

	private string? GetRulerAssassinationChance(JToken choice)
	{
		var chance = choice.MustHaveInt("_rulerAssassinationPercentChance");
		if (chance == 0)
		{
			return null;
		}

		var result = "Ruler Assassination Chance: " + chance.ToString("n0", gameCulture) + '%';
		var range = GetRangeText(choice.MustHaveInt("_rulerAssassinationMinDelaySecs"), choice.MustHaveInt("_rulerAssassinationMaxDelaySecs"));
		if (range is not null)
		{
			result += $" (delay: {range} seconds)";
		}

		return result;
	}
	#endregion
}