namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.EsoSkillSummaries;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class EsoActiveSkillSummaries : EsoSkillSummaryBase<ActiveSkill>
	{
		#region Constructors
		[JobInfo("Update Active Skills", "ESO")]
		public EsoActiveSkillSummaries(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.Site.EditingDisabled = true;
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

		protected override string TypeText => "active";
		#endregion

		#region Protected Override Methods
		protected override void UpdateSkillTemplate(ActiveSkill skillBase, Template template, HashSet<string> replacements)
		{
			string FormatMeters(string value) => $"{value} meter{(value == "1" ? string.Empty : "s")}";
			string FormatSeconds(string value) => value == "0" ? "Instant" : $"{value} second{(value == "1" ? string.Empty : "s")}";

			ThrowNull(skillBase, nameof(skillBase));
			ThrowNull(template, nameof(template));
			ThrowNull(replacements, nameof(replacements));
			var baseMorph = skillBase.Morphs[0];
			template.AddOrChange("id", baseMorph.Abilities[3].Id);
			var radiusParam = template.FindFirst("radius", "area");
			var baseCost = baseMorph.Costs[3];
			var baseDuration = baseMorph.Durations[3];
			var baseChannelTime = baseMorph.ChannelTimes[3];
			var baseRadius = baseMorph.Radii[3];
			var baseRange = baseMorph.Ranges[3];
			var baseSkillCost = baseMorph.FullName(baseMorph.CalculatedCost(baseCost, this.PatchVersion));
			var baseTarget = baseMorph.Target;
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
				if (morphChannelTime != baseChannelTime)
				{
					descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
				}

				var morphSkillCost = morph.FullCost(this.PatchVersion);
				if (morph.Costs[0] != baseCost || morphSkillCost != baseSkillCost)
				{
					descriptions.Add("Cost: " + morphSkillCost);
				}

				var morphDuration = morph.Durations.ToString();
				if (morphDuration != baseDuration && morphDuration != "0")
				{
					descriptions.Add("Duration: " + FormatSeconds(morphDuration));
				}

				var morphRadius = morph.Radii.ToString();
				if (morphRadius != baseRadius && morphRadius != "0" && morph.Target != "Self")
				{
					var word = radiusParam?.Name?.UpperFirst(this.Site.Culture) ?? "Area";
					descriptions.Add($"{word}: {FormatMeters(morphRadius)}");
				}

				var morphRange = morph.Ranges.ToString();
				if (morphRange != baseRange && morphRange != "0" && morph.Target != "Self")
				{
					descriptions.Add("Range: " + FormatMeters(morphRange));
				}

				if (morph.Target != baseTarget)
				{
					descriptions.Add("Target: " + morph.Target);
				}

				var extras = string.Join(", ", descriptions);
				if (extras.Length > 0)
				{
					extras += ".<br>";
				}

				var parameterName = "desc" + morphNum;
				template.AddOrChange(parameterName, extras + EsoReplacer.ReplaceLink(morph.Description));

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

			template.AddOrChange("casttime", FormatSeconds(baseMorph.CastingTime));
			template.AddOrChange("linerank", skillBase.LearnedLevel);
			template.AddOrChange("cost", baseSkillCost);
			template.RemoveOrChange("range", FormatMeters(baseRange), baseRange == "0");

			if (baseRadius == "0")
			{
				template.Remove("radius");
				template.Remove("area");
			}
			else
			{
				var newValue = FormatMeters(baseRadius);
				if (radiusParam == null)
				{
					template.Add("area", newValue);
				}
				else
				{
					radiusParam.Value = newValue;
				}
			}

			template.RemoveOrChange("duration", FormatSeconds(baseDuration), baseDuration == "0");
			template.RemoveOrChange("channeltime", FormatSeconds(baseChannelTime), baseChannelTime == "0");
			template.AddOrChange("target", baseTarget);
			template.RemoveOrChange("type", skillBase.SkillType, skillBase.SkillType == "Active");
			template["cost"].Value = EsoReplacer.ReplaceGlobal(template["cost"].Value, null);
			template["desc"].Value = EsoReplacer.ReplaceGlobal(template["desc"].Value, skillBase.Name);
			template["desc1"].Value = EsoReplacer.ReplaceGlobal(template["desc1"].Value, skillBase.Name);
			template["desc2"].Value = EsoReplacer.ReplaceGlobal(template["desc2"].Value, skillBase.Name);
		}
		#endregion
	}
}