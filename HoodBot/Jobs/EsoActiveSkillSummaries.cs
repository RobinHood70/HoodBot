namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	internal class EsoActiveSkillSummaries : EsoSkillJob<ActiveSkill>
	{
		#region Constructors
		[JobInfo("Update Active Skills", "ESO")]
		public EsoActiveSkillSummaries(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string Query =>
			@"SELECT
				skillTree.basename, skillTree.skillTypeName, skillTree.learnedLevel, skillTree.type,
				minedSkills.id,
				minedSkills.name,
				minedSkills.castTime,
				minedSkills.effectLines,
				minedSkills.target,
				minedSkills.morph,
				minedSkills.rank,
				minedSkills.channelTime,
				minedSkills.cost,
				minedSkills.duration,
				minedSkills.maxRange,
				minedSkills.minRange,
				minedSkills.radius,
				minedSkills.coefDescription,
				minedSkills.description,
				minedSkills.mechanic,
				a1, b1, c1, R1, type1,
				a2, b2, c2, R2, type2,
				a3, b3, c3, R3, type3,
				a4, b4, c4, R4, type4,
				a5, b5, c5, R5, type5,
				a6, b6, c6, R6, type6
			FROM
				skillTree
					INNER JOIN
				minedSkills ON skillTree.abilityId = minedSkills.id
			WHERE
				!minedSkills.isPassive
					AND minedSkills.isPlayer = 1
					AND skillTree.skillIndex > 0
			ORDER BY baseName, minedSkills.morph, minedSkills.rank;";

		protected override string TypeText => "Active";
		#endregion

		#region Protected Override Methods
		protected override ActiveSkill GetNewSkill(IDataRecord row) => new ActiveSkill(row);

		protected override void UpdateSkillTemplate(ActiveSkill skillBase, Template template)
		{
			ThrowNull(skillBase, nameof(skillBase));
			ThrowNull(template, nameof(template));
			var baseMorph = skillBase.Morphs[0];
			template.AddOrChange("id", baseMorph.Abilities[3].Id);
			var baseSkillCost = baseMorph.FullName(baseMorph.CalculatedCost(baseMorph.Costs[3], EsoGeneral.GetPatchVersion(this)));
			this.UpdateMorphs(skillBase, template, baseMorph, baseSkillCost);

			template.AddOrChange("casttime", FormatSeconds(baseMorph.CastingTime));
			template.AddOrChange("linerank", skillBase.LearnedLevel);
			template.AddOrChange("cost", baseSkillCost);
			template.RemoveOrChange("range", FormatMeters(baseMorph.Ranges[3]), string.Equals(baseMorph.Ranges[3], "0", System.StringComparison.Ordinal));

			if (string.Equals(baseMorph.Radii[3], "0", System.StringComparison.Ordinal))
			{
				template.Remove("radius");
				template.Remove("area");
			}
			else
			{
				var newValue = FormatMeters(baseMorph.Radii[3]);
				if (template.FindFirst("radius", "area") is Parameter radiusParam)
				{
					radiusParam.Value = newValue;
				}
				else
				{
					template.Add("area", newValue);
				}
			}

			template.RemoveOrChange("duration", FormatSeconds(baseMorph.Durations[3]), string.Equals(baseMorph.Durations[3], "0", System.StringComparison.Ordinal));
			template.RemoveOrChange("channeltime", FormatSeconds(baseMorph.ChannelTimes[3]), string.Equals(baseMorph.ChannelTimes[3], "0", System.StringComparison.Ordinal));
			template.AddOrChange("target", baseMorph.Target);
			template.RemoveOrChange("type", skillBase.SkillType, string.Equals(skillBase.SkillType, "Active", System.StringComparison.Ordinal));
			EsoReplace(template["cost"], null);
			EsoReplace(template["desc"], skillBase.Name);
			EsoReplace(template["desc1"], skillBase.Name);
			EsoReplace(template["desc2"], skillBase.Name);
		}
		#endregion

		#region Private Static Methods
		private static void EsoReplace(Parameter? param, string? skillName)
		{
			ThrowNull(param, nameof(param));
			param.Value = EsoReplacer.ReplaceGlobal(param.Value, skillName);
		}

		private static string FormatMeters(string? value) => value switch
		{
			null => throw ArgumentNull(nameof(value)),
			"1" => "1 meter",
			_ => $"{value} meters"
		};

		private static string FormatSeconds(string? value) => value switch
		{
			null => throw ArgumentNull(nameof(value)),
			"0" => "Instant",
			"1" => "1 second",
			_ => $"{value} seconds"
		};
		#endregion

		#region Private Methods
		private void UpdateMorphs(ActiveSkill skillBase, Template template, Morph baseMorph, string baseSkillCost)
		{
			var usedList = new TitleCollection(this.Site);
			for (var morphCounter = 0; morphCounter < skillBase.Morphs.Count; morphCounter++)
			{
				var descriptions = new List<string>();
				var morphNum = morphCounter == 0 ? string.Empty : morphCounter.ToStringInvariant();
				var morph = skillBase.Morphs[morphCounter];

				if (!string.Equals(morph.CastingTime, baseMorph.CastingTime, System.StringComparison.Ordinal))
				{
					descriptions.Add("Casting Time: " + FormatSeconds(morph.CastingTime));
				}

				var morphChannelTime = morph.ChannelTimes.ToString();
				if (!string.Equals(morphChannelTime, baseMorph.ChannelTimes[3], System.StringComparison.Ordinal))
				{
					descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
				}

				var morphSkillCost = morph.FullCost(EsoGeneral.GetPatchVersion(this));
				if (morph.Costs[0] != baseMorph.Costs[3] || !string.Equals(morphSkillCost, baseSkillCost, System.StringComparison.Ordinal))
				{
					descriptions.Add("Cost: " + morphSkillCost);
				}

				var morphDuration = morph.Durations.ToString();
				if (!string.Equals(morphDuration, baseMorph.Durations[3], System.StringComparison.Ordinal) && !string.Equals(morphDuration, "0", System.StringComparison.Ordinal))
				{
					descriptions.Add("Duration: " + FormatSeconds(morphDuration));
				}

				var morphRadius = morph.Radii.ToString();
				if (!string.Equals(morphRadius, baseMorph.Radii[3], System.StringComparison.Ordinal) && !string.Equals(morphRadius, "0", System.StringComparison.Ordinal) && !string.Equals(morph.Target, "Self", System.StringComparison.Ordinal))
				{
					var word = template.FindFirst("radius", "area")?.Name?.UpperFirst(this.Site.Culture) ?? "Area";
					descriptions.Add($"{word}: {FormatMeters(morphRadius)}");
				}

				var morphRange = morph.Ranges.ToString();
				if (!string.Equals(morphRange, baseMorph.Ranges[3], System.StringComparison.Ordinal) && !string.Equals(morphRange, "0", System.StringComparison.Ordinal) && !string.Equals(morph.Target, "Self", System.StringComparison.Ordinal))
				{
					descriptions.Add("Range: " + FormatMeters(morphRange));
				}

				if (!string.Equals(morph.Target, baseMorph.Target, System.StringComparison.Ordinal))
				{
					descriptions.Add("Target: " + morph.Target);
				}

				var extras = string.Join(", ", descriptions);
				if (extras.Length > 0)
				{
					extras += ".<br>";
				}

				var parameterName = "desc" + morphNum;
				template.AddOrChange(parameterName, extras + EsoReplacer.ReplaceFirstLink(morph.Description ?? string.Empty, usedList));

				if (morphCounter > 0)
				{
					var morphName = "morph" + morphNum;
					template.AddOrChange(morphName + "name", morph.Name);
					template.AddOrChange(morphName + "id", morph.Abilities[3].Id);
					var iconValue = MakeIcon(skillBase.SkillLine, morph.Name);
					template.AddOrChange(morphName + "icon", IconValueFixup(template[morphName + "icon"]?.Value, iconValue));
					template.AddOrChange(morphName + "desc", EsoReplacer.ReplaceFirstLink(morph.EffectLine, usedList));
				}
			}
		}
		#endregion
	}
}