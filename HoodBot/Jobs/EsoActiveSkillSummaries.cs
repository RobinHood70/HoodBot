namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoActiveSkillSummaries : EsoSkillJob<ActiveSkill>
	{
		#region Constructors
		[JobInfo("Update Active Skills", "ESO Update")]
		public EsoActiveSkillSummaries(JobManager jobManager)
			: base(jobManager)
		{
			// jobManager.ShowDiffs = false;
		}
		#endregion

		#region Protected Override Properties
		protected override string Query =>
		"SELECT\n" +
			"skillTree.basename,\n" +
			"skillTree.skillTypeName,\n" +
			"skillTree.learnedLevel,\n" +
			"skillTree.type,\n" +
			"minedSkills.id,\n" +
			"minedSkills.name,\n" +
			"minedSkills.castTime,\n" +
			"minedSkills.effectLines,\n" +
			"minedSkills.target,\n" +
			"minedSkills.morph,\n" +
			"minedSkills.rank,\n" +
			"minedSkills.channelTime,\n" +
			"minedSkills.cost,\n" +
			"minedSkills.duration,\n" +
			"minedSkills.maxRange,\n" +
			"minedSkills.minRange,\n" +
			"minedSkills.radius,\n" +
			"minedSkills.coefDescription,\n" +
			"minedSkills.description,\n" +
			"minedSkills.mechanic,\n" +
			"a1, b1, c1, R1, type1,\n" +
			"a2, b2, c2, R2, type2,\n" +
			"a3, b3, c3, R3, type3,\n" +
			"a4, b4, c4, R4, type4,\n" +
			"a5, b5, c5, R5, type5,\n" +
			"a6, b6, c6, R6, type6\n" +
		"FROM\n" +
			"skillTree\n" +
		"INNER JOIN\n" +
			"minedSkills ON skillTree.abilityId = minedSkills.id\n" +
		"WHERE\n" +
			"!minedSkills.isPassive\n" +
			"AND minedSkills.isPlayer = 1\n" +
			"AND minedSkills.morph >= 0\n" +
		"ORDER BY skillTree.baseName, minedSkills.morph, minedSkills.rank;";

		protected override string TypeText => "Active";
		#endregion

		#region Protected Override Methods
		protected override void AddSkillData(ActiveSkill skill, IDataRecord row)
		{
			skill.ThrowNull();
			var morphNum = (sbyte)row["morph"];
			if (morphNum >= skill.Morphs.Count)
			{
				skill.Morphs.Add(new Morph(row));
			}

			var morph = skill.Morphs[^1];
			var rank = new ActiveRank(row);
			if (rank.Costs.Count == 1 && rank.Costs[0].Value == -1)
			{
				skill.SkillType = "Artifact";
			}

			morph.Ranks.Add(rank);
		}

		protected override ActiveSkill GetNewSkill(IDataRecord row) => new(row);

		protected override void SkillPostProcess(ActiveSkill skill)
		{
			foreach (var morph in skill.Morphs)
			{
				morph.ParseDescription();
			}
		}

		protected override void UpdateSkillTemplate(ActiveSkill skillBase, ITemplateNode template)
		{
			var baseMorph = skillBase.NotNull().Morphs[0];
			var baseRank = baseMorph.Ranks[^1];
			this.UpdateParameter(template.NotNull(), "id", baseRank.Id.ToStringInvariant());
			var (valueText, mechanicText) = baseRank.GetCostSplit();
			var baseSkillCost = mechanicText.Length == 0
				? valueText
				: valueText + ' ' + mechanicText;
			this.UpdateMorphs(skillBase, template, baseMorph, baseSkillCost);

			this.UpdateParameter(template, "casttime", FormatSeconds(baseMorph.CastingTime));
			this.UpdateParameter(template, "linerank", skillBase.LearnedLevel.ToStringInvariant());
			this.UpdateParameter(template, "cost", baseSkillCost);
			if (template.Find("cost")?.Value is NodeCollection paramValue)
			{
				// Cost is an oddball where we don't need/want to do all replacements, just the global ones.
				EsoReplacer.ReplaceGlobal(paramValue);
				EsoReplacer.ReplaceEsoLinks(this.Site, paramValue);
			}

			this.UpdateParameter(template, "range", FormatMeters(baseRank.Range), string.Equals(baseRank.Range, "0", StringComparison.Ordinal));

			if (string.Equals(baseRank.Radius, "0", StringComparison.Ordinal))
			{
				template.Remove("radius");
				template.Remove("area");
			}
			else
			{
				var newValue = FormatMeters(baseRank.Radius);
				if (template.Find("radius", "area") is IParameterNode radiusParam)
				{
					var oldValue = radiusParam.Value.ToValue().Trim();
					if (string.Equals(oldValue, newValue, StringComparison.OrdinalIgnoreCase))
					{
						radiusParam.SetValue(newValue, ParameterFormat.Copy);
					}
				}
				else
				{
					template.Add("area", newValue + '\n');
				}
			}

			this.UpdateParameter(template, "duration", FormatSeconds(baseRank.Duration), string.Equals(baseRank.Duration, "0", StringComparison.Ordinal));
			this.UpdateParameter(template, "channeltime", FormatSeconds(baseRank.ChannelTime), string.Equals(baseRank.ChannelTime, "0", StringComparison.Ordinal));
			this.UpdateParameter(template, "target", baseMorph.Target);
			this.UpdateParameter(template, "type", skillBase.SkillType, string.Equals(skillBase.SkillType, "Active", StringComparison.Ordinal));
		}
		#endregion

		#region Private Static Methods
		private static string FormatMeters(string? value) => string.Equals(value.NotNull(), "1", StringComparison.Ordinal)
			? "1 meter"
			: $"{value} meters";

		private static string FormatSeconds(string? value) => value.NotNull() switch
		{
			"0" => "Instant",
			"1" => "1 second",
			_ => $"{value} seconds"
		};
		#endregion

		#region Private Methods
		private void UpdateMorphs(ActiveSkill skillBase, ITemplateNode template, Morph baseMorph, string baseSkillCost)
		{
			TitleCollection usedList = new(this.Site);
			for (var morphCounter = 0; morphCounter < skillBase.Morphs.Count; morphCounter++)
			{
				this.UpdateMorph(skillBase, template, baseMorph, baseSkillCost, usedList, morphCounter);
			}
		}

		private void UpdateMorph(ActiveSkill skillBase, ITemplateNode template, Morph baseMorph, string baseSkillCost, TitleCollection usedList, int morphCounter)
		{
			var baseRank = baseMorph.Ranks[^1];
			List<string> descriptions = new();
			var morphNum = morphCounter == 0 ? string.Empty : morphCounter.ToStringInvariant();
			var morph = skillBase.Morphs[morphCounter];
			if (!string.Equals(morph.CastingTime, baseMorph.CastingTime, StringComparison.Ordinal))
			{
				descriptions.Add("Casting Time: " + FormatSeconds(morph.CastingTime));
			}

			var morphChannelTime = Morph.NowrapSameString(morph.Ranks.Select(r => r.ChannelTime));
			if (!string.Equals(morphChannelTime, baseRank.ChannelTime, StringComparison.Ordinal))
			{
				descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
			}

			var morphSkillCost = morph.RankCosts();
			if (!string.Equals(morphSkillCost, baseSkillCost, StringComparison.Ordinal))
			{
				descriptions.Add("Cost: " + morphSkillCost);
			}

			var morphDuration = Morph.NowrapSameString(morph.Ranks.Select(r => r.Duration));
			if (!string.Equals(morphDuration, baseRank.Duration, StringComparison.Ordinal) && !string.Equals(morphDuration, "0", StringComparison.Ordinal))
			{
				descriptions.Add("Duration: " + FormatSeconds(morphDuration));
			}

			var morphRadius = Morph.NowrapSameString(morph.Ranks.Select(r => r.Radius));
			if (!string.Equals(morphRadius, baseRank.Radius, StringComparison.Ordinal) && !string.Equals(morphRadius, "0", StringComparison.Ordinal) && !string.Equals(morph.Target, "Self", StringComparison.Ordinal))
			{
				var word = template.Find("radius", "area")?.Name?.ToValue().UpperFirst(this.Site.Culture) ?? "Area";
				descriptions.Add($"{word}: {FormatMeters(morphRadius)}");
			}

			var morphRange = Morph.NowrapSameString(morph.Ranks.Select(r => r.Range));
			if (!string.Equals(morphRange, baseRank.Range, StringComparison.Ordinal) && !string.Equals(morphRange, "0", StringComparison.Ordinal) && !string.Equals(morph.Target, "Self", StringComparison.Ordinal))
			{
				descriptions.Add("Range: " + FormatMeters(morphRange));
			}

			if (!string.Equals(morph.Target, baseMorph.Target, StringComparison.Ordinal))
			{
				descriptions.Add("Target: " + morph.Target);
			}

			var extras = string.Join(", ", descriptions);
			if (extras.Length > 0)
			{
				extras += ".<br>";
			}

			var paramName = "desc" + morphNum;
			this.UpdateParameter(template, paramName, extras + (morph.Description ?? string.Empty), usedList, skillBase.Name);
			if (morphCounter > 0)
			{
				var morphName = "morph" + morphNum;
				this.UpdateParameter(template, morphName + "name", morph.Name);
				this.UpdateParameter(template, morphName + "id", morph.Ranks[^1].Id.ToStringInvariant());
				var iconValue = MakeIcon(skillBase.SkillLine, morph.Name);
				this.UpdateParameter(template, morphName + "icon", IconValueFixup(template.Find(morphName + "icon"), iconValue));
				this.UpdateParameter(template, morphName + "desc", morph.EffectLine, usedList, skillBase.Name);
			}
		}
		#endregion
	}
}