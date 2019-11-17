namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

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

		protected override void UpdateSkillTemplate(ActiveSkill skillBase, Template template, HashSet<string> replacements)
		{
			ThrowNull(skillBase, nameof(skillBase));
			ThrowNull(template, nameof(template));
			ThrowNull(replacements, nameof(replacements));
			ThrowNull(this.PatchVersion, nameof(EsoActiveSkillSummaries), nameof(this.PatchVersion));
			var baseMorph = skillBase.Morphs[0];
			template.AddOrChange("id", baseMorph.Abilities[3].Id);
			var baseSkillCost = baseMorph.FullName(baseMorph.CalculatedCost(baseMorph.Costs[3], this.PatchVersion));
			this.UpdateMorphs(skillBase, template, baseMorph, baseSkillCost);

			template.AddOrChange("casttime", FormatSeconds(baseMorph.CastingTime));
			template.AddOrChange("linerank", skillBase.LearnedLevel);
			template.AddOrChange("cost", baseSkillCost);
			template.RemoveOrChange("range", FormatMeters(baseMorph.Ranges[3]), baseMorph.Ranges[3] == "0");

			if (baseMorph.Radii[3] == "0")
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

			template.RemoveOrChange("duration", FormatSeconds(baseMorph.Durations[3]), baseMorph.Durations[3] == "0");
			template.RemoveOrChange("channeltime", FormatSeconds(baseMorph.ChannelTimes[3]), baseMorph.ChannelTimes[3] == "0");
			template.AddOrChange("target", baseMorph.Target);
			template.RemoveOrChange("type", skillBase.SkillType, skillBase.SkillType == "Active");
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

		private static string FormatMeters(string? value)
		{
			ThrowNull(value, nameof(value));
			return $"{value} meter{(value == "1" ? string.Empty : "s")}";
		}

		private static string FormatSeconds(string? value)
		{
			ThrowNull(value, nameof(value));
			return value == "0" ? "Instant" : $"{value} second{(value == "1" ? string.Empty : "s")}";
		}
		#endregion

		#region Private Methods
		private void UpdateMorphs(ActiveSkill skillBase, Template template, Morph baseMorph, string baseSkillCost)
		{
			for (var morphCounter = 0; morphCounter < skillBase.Morphs.Count; morphCounter++)
			{
				var descriptions = new List<string>();
				var morphNum = morphCounter == 0 ? string.Empty : morphCounter.ToStringInvariant();
				var morph = skillBase.Morphs[morphCounter];

				if (morph.CastingTime != baseMorph.CastingTime)
				{
					descriptions.Add("Casting Time: " + FormatSeconds(morph.CastingTime));
				}

				var morphChannelTime = morph.ChannelTimes.ToString();
				if (morphChannelTime != baseMorph.ChannelTimes[3])
				{
					descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
				}

				var morphSkillCost = morph.FullCost(this.PatchVersion!);
				if (morph.Costs[0] != baseMorph.Costs[3] || morphSkillCost != baseSkillCost)
				{
					descriptions.Add("Cost: " + morphSkillCost);
				}

				var morphDuration = morph.Durations.ToString();
				if (morphDuration != baseMorph.Durations[3] && morphDuration != "0")
				{
					descriptions.Add("Duration: " + FormatSeconds(morphDuration));
				}

				var morphRadius = morph.Radii.ToString();
				if (morphRadius != baseMorph.Radii[3] && morphRadius != "0" && morph.Target != "Self")
				{
					var word = template.FindFirst("radius", "area")?.Name?.UpperFirst(this.Site.Culture) ?? "Area";
					descriptions.Add($"{word}: {FormatMeters(morphRadius)}");
				}

				var morphRange = morph.Ranges.ToString();
				if (morphRange != baseMorph.Ranges[3] && morphRange != "0" && morph.Target != "Self")
				{
					descriptions.Add("Range: " + FormatMeters(morphRange));
				}

				if (morph.Target != baseMorph.Target)
				{
					descriptions.Add("Target: " + morph.Target);
				}

				var extras = string.Join(", ", descriptions);
				if (extras.Length > 0)
				{
					extras += ".<br>";
				}

				var parameterName = "desc" + morphNum;
				template.AddOrChange(parameterName, extras + EsoReplacer.ReplaceLink(morph.Description ?? string.Empty));

				if (morphCounter > 0)
				{
					var morphName = "morph" + morphNum;
					template.AddOrChange(morphName + "name", morph.Name);
					template.AddOrChange(morphName + "id", morph.Abilities[3].Id);
					var iconValue = MakeIcon(skillBase.SkillLine, morph.Name);
					template.AddOrChange(morphName + "icon", IconValueFixup(template[morphName + "icon"]?.Value, iconValue));
					template.AddOrChange(morphName + "desc", EsoReplacer.ReplaceLink(morph.EffectLine));
				}
			}
		}
		#endregion
	}
}