namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class ActiveSkill(IDataRecord row) : Skill(row)
	{
		#region Fields
		private readonly int learnedLevel = (int)row["learnedLevel"];
		private readonly List<Morph> morphs = new(3);
		private readonly string skillType = ((string)row["icon"]).Contains("_artifact_", StringComparison.OrdinalIgnoreCase)
			? "Artifact"
			: EsoLog.ConvertEncoding((string)row["type"]);
		#endregion

		#region Public Override Methods
		public override void AddData(IDataRecord row)
		{
			ArgumentNullException.ThrowIfNull(row);
			var morphNum = (sbyte)row["morph"];
			if (morphNum >= this.morphs.Count)
			{
				this.morphs.Add(new Morph(row));
			}

			var morph = this.morphs[^1];
			var rank = new ActiveRank(row);
			morph.Ranks.Add(rank);
		}

		public override bool Check()
		{
			foreach (var morph in this.morphs)
			{
				if (morph.Description == null)
				{
					Debug.WriteLine($"Warning: {this.Name} - {morph.Name} has no description.");
					return true;
				}
			}

			return false;
		}

		public override void PostProcess()
		{
			foreach (var morph in this.morphs)
			{
				morph.Description = morph.GetParsedDescription();
			}
		}

		public override void SetChangeType(Skill previous)
		{
			if (previous is not ActiveSkill prevSkill)
			{
				throw new InvalidOperationException();
			}

			var retval = ChangeType.None;
			for (var i = 0; i < this.morphs.Count; i++)
			{
				var curMorph = this.morphs[i];
				var prevMorph = prevSkill.morphs[i];
				var changeType = curMorph.GetChangeType(prevMorph);
				if (changeType > retval)
				{
					if (changeType == ChangeType.Major)
					{
						this.ChangeType = ChangeType.Major;
						return;
					}

					retval = changeType;
				}
			}

			this.ChangeType = retval;
		}
		#endregion

		#region Protected Override Methods
		protected override void UpdateTemplate(SiteTemplateNode template)
		{
			ArgumentNullException.ThrowIfNull(template);
			var baseMorph = this.morphs[0];
			var baseRank = baseMorph.Ranks[^1];
			template.Update("id", baseRank.Id.ToStringInvariant(), ParameterFormat.OnePerLine, true);
			var baseSkillCost = Cost.GetCostText(baseRank.Costs);
			this.UpdateMorphs(template, baseMorph, baseSkillCost);
			template.Update("casttime", FormatSeconds(baseMorph.CastingTime), ParameterFormat.OnePerLine, true);
			template.Update("linerank", this.learnedLevel.ToStringInvariant(), ParameterFormat.OnePerLine, true);
			template.Update("cost", baseSkillCost, ParameterFormat.OnePerLine, true);
			if (template.Find("cost")?.Value is WikiNodeCollection paramValue)
			{
				// Cost is an oddball where we don't need/want to do all replacements, just the global ones.
				EsoReplacer.ReplaceGlobal(paramValue);
				EsoReplacer.ReplaceEsoLinks(paramValue);
			}

			template.UpdateOrRemove("range", FormatMeters(baseRank.Range), ParameterFormat.OnePerLine, baseRank.Range.OrdinalEquals("0"));

			if (baseRank.Radius.OrdinalEquals("0"))
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
					if (oldValue.OrdinalICEquals(newValue))
					{
						radiusParam.SetValue(newValue, ParameterFormat.Copy);
					}
				}
				else
				{
					template.Add("area", newValue + '\n');
				}
			}

			template.UpdateOrRemove("duration", FormatSeconds(baseRank.Duration), ParameterFormat.OnePerLine, baseRank.Duration.OrdinalEquals("0"));
			template.UpdateOrRemove("channeltime", FormatSeconds(baseRank.ChannelTime), ParameterFormat.OnePerLine, baseRank.ChannelTime.OrdinalEquals("0"));
			template.Update("target", baseMorph.Target, ParameterFormat.OnePerLine, true);
			template.UpdateOrRemove("type", this.skillType, ParameterFormat.OnePerLine, this.skillType.OrdinalEquals("Active"));
		}

		#endregion

		#region Private Methods
		private void UpdateMorph(SiteTemplateNode template, Morph baseMorph, string baseSkillCost, TitleCollection usedList, int morphCounter)
		{
			var baseRank = baseMorph.Ranks[^1];
			List<string> descriptions = [];
			var morphNum = morphCounter == 0 ? string.Empty : morphCounter.ToStringInvariant();
			var morph = this.morphs[morphCounter];
			if (!morph.CastingTime.OrdinalEquals(baseMorph.CastingTime))
			{
				descriptions.Add("Casting Time: " + FormatSeconds(morph.CastingTime));
			}

			var morphChannelTime = Morph.NowrapSameString(morph.Ranks.Select(r => r.ChannelTime));
			if (!morphChannelTime.OrdinalEquals(baseRank.ChannelTime))
			{
				descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
			}

			var morphSkillCosts = morph.ConsolidateCosts();
			var morphSkillCost = Cost.GetCostText(morphSkillCosts);
			if (!morphSkillCost.OrdinalEquals(baseSkillCost))
			{
				descriptions.Add("Cost: " + morphSkillCost);
			}

			var morphDuration = Morph.NowrapSameString(morph.Ranks.Select(r => r.Duration));
			if (!morphDuration.OrdinalEquals(baseRank.Duration) && !morphDuration.OrdinalEquals("0"))
			{
				descriptions.Add("Duration: " + FormatSeconds(morphDuration));
			}

			var culture = template.Title.Site.Culture;
			var morphRadius = Morph.NowrapSameString(morph.Ranks.Select(r => r.Radius));
			if (!morphRadius.OrdinalEquals(baseRank.Radius) && !morphRadius.OrdinalEquals("0") && !morph.Target.OrdinalEquals("Self"))
			{
				var word = template.Find("radius", "area")?.Name?.ToValue().UpperFirst(culture) ?? "Area";
				descriptions.Add($"{word}: {FormatMeters(morphRadius)}");
			}

			var morphRange = Morph.NowrapSameString(morph.Ranks.Select(r => r.Range));
			if (!morphRange.OrdinalEquals(baseRank.Range) && !morphRange.OrdinalEquals("0") && !morph.Target.OrdinalEquals("Self"))
			{
				descriptions.Add("Range: " + FormatMeters(morphRange));
			}

			if (!morph.Target.OrdinalEquals(baseMorph.Target))
			{
				descriptions.Add("Target: " + morph.Target);
			}

			var extras = string.Join(", ", descriptions);
			if (extras.Length > 0)
			{
				extras += ".<br>";
			}

			var paramName = "desc" + morphNum;
			UpdateParameter(template, paramName, extras + (morph.Description ?? string.Empty), usedList, this.Name);
			if (morphCounter > 0)
			{
				var morphName = "morph" + morphNum;
				template.Update(morphName + "name", morph.Name, ParameterFormat.OnePerLine, true);
				template.Update(morphName + "id", morph.Ranks[^1].Id.ToStringInvariant(), ParameterFormat.OnePerLine, true);
				var iconValue = MakeIcon(this.SkillLine, morph.Name);
				template.Update(morphName + "icon", IconValueFixup(template.Find(morphName + "icon"), iconValue), ParameterFormat.OnePerLine, true);
				UpdateParameter(template, morphName + "desc", morph.EffectLine, usedList, this.Name);
			}
		}

		private void UpdateMorphs(SiteTemplateNode template, Morph baseMorph, string baseSkillCost)
		{
			TitleCollection usedList = new(template.Title.Site);
			for (var morphCounter = 0; morphCounter < this.morphs.Count; morphCounter++)
			{
				this.UpdateMorph(template, baseMorph, baseSkillCost, usedList, morphCounter);
			}
		}
		#endregion
	}
}