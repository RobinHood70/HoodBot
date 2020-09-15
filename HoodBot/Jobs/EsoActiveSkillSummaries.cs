namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class EsoActiveSkillSummaries : EsoSkillJob<ActiveSkill>
	{
		#region Constructors
		[JobInfo("Update Active Skills", "ESO")]
		public EsoActiveSkillSummaries(JobManager jobManager)
			: base(jobManager)
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

		protected override bool UpdateSkillTemplate(ActiveSkill skillBase, ITemplateNode template)
		{
			ThrowNull(skillBase, nameof(skillBase));
			ThrowNull(template, nameof(template));
			var baseMorph = skillBase.Morphs[0];
			var bigChange = this.TrackedUpdate(template, "id", baseMorph.Abilities[3].Id.ToStringInvariant());
			var baseSkillCost = baseMorph.FullName(baseMorph.CalculatedCost(baseMorph.Costs[3], EsoGeneral.GetPatchVersion(this)));
			this.UpdateMorphs(skillBase, template, baseMorph, baseSkillCost);

			bigChange |= this.TrackedUpdate(template, "casttime", FormatSeconds(baseMorph.CastingTime));
			bigChange |= this.TrackedUpdate(template, "linerank", skillBase.LearnedLevel.ToStringInvariant());
			bigChange |= this.TrackedUpdate(template, "cost", baseSkillCost);
			if (template.Find("cost")?.Value is NodeCollection paramValue)
			{
				// Cost is an oddball where we don't need/want to do all replacements, just the global ones.
				EsoReplacer.ReplaceGlobal(paramValue);
				EsoReplacer.ReplaceEsoLinks(this.Site, paramValue);
			}

			bigChange |= this.TrackedUpdate(template, "range", FormatMeters(baseMorph.Ranges[3]), string.Equals(baseMorph.Ranges[3], "0", StringComparison.Ordinal));

			if (string.Equals(baseMorph.Radii[3], "0", StringComparison.Ordinal))
			{
				template.Remove("radius");
				template.Remove("area");
			}
			else
			{
				var newValue = FormatMeters(baseMorph.Radii[3]);
				if (template.Find("radius", "area") is IParameterNode radiusParam)
				{
					var oldValue = radiusParam.ValueToText()?.Trim();
					if (string.Equals(oldValue, newValue, StringComparison.OrdinalIgnoreCase))
					{
						radiusParam.SetValue(newValue + '\n');
						bigChange = true;
					}
				}
				else
				{
					template.Add("area", newValue + '\n');
					bigChange = true;
				}
			}

			bigChange |= this.TrackedUpdate(template, "duration", FormatSeconds(baseMorph.Durations[3]), string.Equals(baseMorph.Durations[3], "0", StringComparison.Ordinal));
			bigChange |= this.TrackedUpdate(template, "channeltime", FormatSeconds(baseMorph.ChannelTimes[3]), string.Equals(baseMorph.ChannelTimes[3], "0", StringComparison.Ordinal));
			bigChange |= this.TrackedUpdate(template, "target", baseMorph.Target);
			bigChange |= this.TrackedUpdate(template, "type", skillBase.SkillType, string.Equals(skillBase.SkillType, "Active", StringComparison.Ordinal));

			return bigChange;
		}
		#endregion

		#region Private Static Methods
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
		private bool UpdateMorphs(ActiveSkill skillBase, ITemplateNode template, Morph baseMorph, string baseSkillCost)
		{
			var bigChange = false;
			var usedList = new TitleCollection(this.Site);
			for (var morphCounter = 0; morphCounter < skillBase.Morphs.Count; morphCounter++)
			{
				var descriptions = new List<string>();
				var morphNum = morphCounter == 0 ? string.Empty : morphCounter.ToStringInvariant();
				var morph = skillBase.Morphs[morphCounter];

				if (!string.Equals(morph.CastingTime, baseMorph.CastingTime, StringComparison.Ordinal))
				{
					descriptions.Add("Casting Time: " + FormatSeconds(morph.CastingTime));
				}

				var morphChannelTime = morph.ChannelTimes.ToString();
				if (!string.Equals(morphChannelTime, baseMorph.ChannelTimes[3], StringComparison.Ordinal))
				{
					descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
				}

				var morphSkillCost = morph.FullCost(EsoGeneral.GetPatchVersion(this));
				if (morph.Costs[0] != baseMorph.Costs[3] || !string.Equals(morphSkillCost, baseSkillCost, StringComparison.Ordinal))
				{
					descriptions.Add("Cost: " + morphSkillCost);
				}

				var morphDuration = morph.Durations.ToString();
				if (!string.Equals(morphDuration, baseMorph.Durations[3], StringComparison.Ordinal) && !string.Equals(morphDuration, "0", StringComparison.Ordinal))
				{
					descriptions.Add("Duration: " + FormatSeconds(morphDuration));
				}

				var morphRadius = morph.Radii.ToString();
				if (!string.Equals(morphRadius, baseMorph.Radii[3], StringComparison.Ordinal) && !string.Equals(morphRadius, "0", StringComparison.Ordinal) && !string.Equals(morph.Target, "Self", StringComparison.Ordinal))
				{
					var word = template.Find("radius", "area")?.NameToText()?.UpperFirst(this.Site.Culture) ?? "Area";
					descriptions.Add($"{word}: {FormatMeters(morphRadius)}");
				}

				var morphRange = morph.Ranges.ToString();
				if (!string.Equals(morphRange, baseMorph.Ranges[3], StringComparison.Ordinal) && !string.Equals(morphRange, "0", StringComparison.Ordinal) && !string.Equals(morph.Target, "Self", StringComparison.Ordinal))
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

				var parameterName = "desc" + morphNum;
				bigChange = this.TrackedUpdate(template, parameterName, extras + morph.Description ?? string.Empty, usedList, skillBase.Name);
				if (morphCounter > 0)
				{
					var morphName = "morph" + morphNum;
					bigChange |= this.TrackedUpdate(template, morphName + "name", morph.Name);
					bigChange |= this.TrackedUpdate(template, morphName + "id", morph.Abilities[3].Id.ToStringInvariant());
					var iconValue = MakeIcon(skillBase.SkillLine, morph.Name);
					bigChange |= this.TrackedUpdate(template, morphName + "icon", IconValueFixup(template.Find(morphName + "icon")?.ValueToText(), iconValue));
					bigChange |= this.TrackedUpdate(template, morphName + "desc", morph.EffectLine, usedList, skillBase.Name);
				}
			}

			return bigChange;
		}
		#endregion
	}
}