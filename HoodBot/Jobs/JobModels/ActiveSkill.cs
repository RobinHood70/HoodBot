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

	internal sealed class ActiveSkill : Skill
	{
		#region Fields
		private readonly int learnedLevel;
		private readonly List<Morph> morphs = new(3);
		private string skillType;
		#endregion

		#region Constructors
		public ActiveSkill(IDataRecord row)
			: base(row)
		{
			this.learnedLevel = (int)row["learnedLevel"];
			this.skillType = (string)row["type"];
		}
		#endregion

		#region Public Override Methods
		public override void AddData(IDataRecord row)
		{
			row.ThrowNull();
			var morphNum = (sbyte)row["morph"];
			if (morphNum >= this.morphs.Count)
			{
				this.morphs.Add(new Morph(row));
			}

			var morph = this.morphs[^1];
			var rank = new ActiveRank(row);
			if (rank.Costs.Count == 1 && rank.Costs[0].Value == -1)
			{
				this.skillType = "Artifact";
			}

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
		protected override void UpdateTemplate(SiteNodeFactory factory, ITemplateNode template)
		{
			var baseMorph = this.morphs[0];
			var baseRank = baseMorph.Ranks[^1];
			UpdateParameter(factory, template.NotNull(), "id", baseRank.Id.ToStringInvariant());
			var (valueText, mechanicText) = baseRank.GetCostSplit();
			var baseSkillCost = mechanicText.Length == 0
				? valueText
				: valueText + ' ' + mechanicText;
			this.UpdateMorphs(factory, template, baseMorph, baseSkillCost);

			UpdateParameter(factory, template, "casttime", FormatSeconds(baseMorph.CastingTime));
			UpdateParameter(factory, template, "linerank", this.learnedLevel.ToStringInvariant());
			UpdateParameter(factory, template, "cost", baseSkillCost);
			if (template.Find("cost")?.Value is NodeCollection paramValue)
			{
				// Cost is an oddball where we don't need/want to do all replacements, just the global ones.
				EsoReplacer.ReplaceGlobal(paramValue);
				EsoReplacer.ReplaceEsoLinks(factory.Site, paramValue);
			}

			UpdateParameter(factory, template, "range", FormatMeters(baseRank.Range), string.Equals(baseRank.Range, "0", StringComparison.Ordinal));

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

			UpdateParameter(factory, template, "duration", FormatSeconds(baseRank.Duration), string.Equals(baseRank.Duration, "0", StringComparison.Ordinal));
			UpdateParameter(factory, template, "channeltime", FormatSeconds(baseRank.ChannelTime), string.Equals(baseRank.ChannelTime, "0", StringComparison.Ordinal));
			UpdateParameter(factory, template, "target", baseMorph.Target);
			UpdateParameter(factory, template, "type", this.skillType, string.Equals(this.skillType, "Active", StringComparison.Ordinal));
		}

		#endregion

		#region Private Methods
		private void UpdateMorph(SiteNodeFactory factory, ITemplateNode template, Morph baseMorph, string baseSkillCost, TitleCollection usedList, int morphCounter)
		{
			var baseRank = baseMorph.Ranks[^1];
			List<string> descriptions = new();
			var morphNum = morphCounter == 0 ? string.Empty : morphCounter.ToStringInvariant();
			var morph = this.morphs[morphCounter];
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
				var word = template.Find("radius", "area")?.Name?.ToValue().UpperFirst(factory.Site.Culture) ?? "Area";
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
			UpdateParameter(factory, template, paramName, extras + (morph.Description ?? string.Empty), usedList, this.Name);
			if (morphCounter > 0)
			{
				var morphName = "morph" + morphNum;
				UpdateParameter(factory, template, morphName + "name", morph.Name);
				UpdateParameter(factory, template, morphName + "id", morph.Ranks[^1].Id.ToStringInvariant());
				var iconValue = MakeIcon(this.SkillLine, morph.Name);
				UpdateParameter(factory, template, morphName + "icon", IconValueFixup(template.Find(morphName + "icon"), iconValue));
				UpdateParameter(factory, template, morphName + "desc", morph.EffectLine, usedList, this.Name);
			}
		}

		private void UpdateMorphs(SiteNodeFactory factory, ITemplateNode template, Morph baseMorph, string baseSkillCost)
		{
			TitleCollection usedList = new(factory.Site);
			for (var morphCounter = 0; morphCounter < this.morphs.Count; morphCounter++)
			{
				this.UpdateMorph(factory, template, baseMorph, baseSkillCost, usedList, morphCounter);
			}
		}
		#endregion
	}
}