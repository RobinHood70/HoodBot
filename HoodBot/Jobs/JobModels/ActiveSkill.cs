namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon.Parser;

internal sealed class ActiveSkill(IDataRecord row) : Skill(row)
{
	#region Fields
	private readonly int learnedLevel = (int)row["learnedLevel"];
	private readonly string skillType = ((string)row["icon"]).Contains("_artifact_", StringComparison.OrdinalIgnoreCase)
		? "Artifact"
		: EsoLog.ConvertEncoding((string)row["type"]);

	private readonly Morph[] morphs = new Morph[2];
	private Morph? baseSkill;
	#endregion

	#region Private Properties
	private IEnumerable<Morph> AllMorphs
	{
		get
		{
			if (this.baseSkill is null)
			{
				yield break;
			}

			yield return this.baseSkill;
			yield return this.morphs[0];
			yield return this.morphs[1];
		}
	}
	#endregion

	#region Public Override Methods
	public override void AddData(IDataRecord row, List<Coefficient> coefficients)
	{
		ArgumentNullException.ThrowIfNull(row);
		var morph = new Morph(row, coefficients);
		if (morph.Index == 0)
		{
			this.baseSkill ??= morph;
		}
		else
		{
			this.morphs[morph.Index - 1] ??= morph;
		}

		morph.Ranks.Add(new ActiveRank(row));
	}

	public override bool IsValid()
	{
		var morphCount = 0;
		foreach (var morph in this.AllMorphs)
		{
			if (morph.Description == null)
			{
				Debug.WriteLine($"Warning: {this.Name} - {morph.Name} has no description.");
				return false;
			}
		}

		return morphCount != 0;
	}

	public override void PostProcess()
	{
		foreach (var morph in this.morphs)
		{
			morph.Description = morph.GetParsedDescription();
		}
	}

	public override ChangeType GetChangeType(Skill oldVer)
	{
		if (oldVer is not ActiveSkill oldSkill || this.baseSkill is null || oldSkill.baseSkill is null)
		{
			throw new InvalidOperationException();
		}

		var retval = this.baseSkill.GetChangeType(oldSkill.baseSkill);
		retval |= this.morphs[0].GetChangeType(oldSkill.morphs[0]);
		retval |= this.morphs[1].GetChangeType(oldSkill.morphs[1]);
		return retval;
	}
	#endregion

	#region Protected Override Methods
	protected override void UpdateTemplate(Site site, ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(template);
		if (this.baseSkill is null)
		{
			throw new InvalidOperationException();
		}

		var baseRank = this.baseSkill.Ranks[^1];
		template.Update("id", baseRank.Id.ToStringInvariant(), ParameterFormat.OnePerLine, true);
		var baseRankCost = Cost.GetCostText(baseRank.Costs);
		this.UpdateMorphs(site, template, baseRankCost);
		template.Update("casttime", FormatSeconds(EsoSpace.TimeToText(baseRank.CastingTime)), ParameterFormat.OnePerLine, true);
		template.Update("linerank", this.learnedLevel.ToStringInvariant(), ParameterFormat.OnePerLine, true);
		template.Update("cost", baseRankCost, ParameterFormat.OnePerLine, true);
		if (template.Find("cost")?.Value is WikiNodeCollection paramValue)
		{
			// Cost is an oddball where we don't need/want to do all replacements, just the global ones.
			UespReplacer.ReplaceGlobal(paramValue);
			UespReplacer.ReplaceEsoLinks(site, paramValue);
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
				var oldValue = radiusParam.GetValue();
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

		template.UpdateOrRemove("duration", FormatSeconds(EsoSpace.TimeToText(baseRank.Duration)), ParameterFormat.OnePerLine, baseRank.Duration == 0);
		template.UpdateOrRemove("channeltime", FormatSeconds(EsoSpace.TimeToText(baseRank.ChannelTime)), ParameterFormat.OnePerLine, baseRank.ChannelTime == 0);
		template.Update("target", this.baseSkill.Target, ParameterFormat.OnePerLine, true);
		template.UpdateOrRemove("type", this.skillType, ParameterFormat.OnePerLine, this.skillType.OrdinalEquals("Active"));
	}

	#endregion

	#region Private Methods
	private void UpdateMorph(ITemplateNode template, string baseSkillCost, TitleCollection usedList, Morph? morph, CultureInfo culture)
	{
		ArgumentNullException.ThrowIfNull(morph);
		if (this.baseSkill is null)
		{
			throw new InvalidOperationException();
		}

		var baseRank = this.baseSkill.Ranks[^1];
		List<string> descriptions = [];
		if (morph.Ranks[^1].CastingTime != baseRank.CastingTime)
		{
			descriptions.Add("Casting Time: " + FormatSeconds(EsoSpace.TimeToText(baseRank.CastingTime)));
		}

		var morphChannelTime = Morph.NowrapSameString(morph.Ranks.Select(r => EsoSpace.TimeToText(r.ChannelTime)));
		if (!morphChannelTime.OrdinalEquals(EsoSpace.TimeToText(baseRank.ChannelTime)))
		{
			descriptions.Add("Channel Time: " + FormatSeconds(morphChannelTime));
		}

		var morphSkillCosts = morph.ConsolidateCosts();
		var morphSkillCost = Cost.GetCostText(morphSkillCosts);
		if (!morphSkillCost.OrdinalEquals(baseSkillCost))
		{
			descriptions.Add("Cost: " + morphSkillCost);
		}

		var morphDuration = Morph.NowrapSameString(morph.Ranks.Select(r => EsoSpace.TimeToText(r.Duration)));
		if (!morphDuration.OrdinalEquals(EsoSpace.TimeToText(baseRank.Duration)) && !morphDuration.OrdinalEquals("0"))
		{
			descriptions.Add("Duration: " + FormatSeconds(morphDuration));
		}

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

		if (!morph.Target.OrdinalEquals(this.baseSkill.Target))
		{
			descriptions.Add("Target: " + morph.Target);
		}

		var extras = string.Join(", ", descriptions);
		if (extras.Length > 0)
		{
			extras += ".<br>";
		}

		var paramName = "desc" + morph.ParamText;
		UpdateParameter(template, paramName, extras + (morph.Description ?? string.Empty), usedList, this.Name);
		if (morph != this.baseSkill)
		{
			var morphName = "morph" + morph.ParamText;
			template.Update(morphName + "name", morph.Name, ParameterFormat.OnePerLine, true);
			template.Update(morphName + "id", morph.Ranks[^1].Id.ToStringInvariant(), ParameterFormat.OnePerLine, true);
			var iconValue = MakeIcon(this.SkillLine, morph.Name);
			template.Update(morphName + "icon", IconValueFixup(template.Find(morphName + "icon"), iconValue), ParameterFormat.OnePerLine, true);
			UpdateParameter(template, morphName + "desc", morph.EffectLine, usedList, this.Name);
		}
	}

	private void UpdateMorphs(Site site, ITemplateNode template, string baseSkillCost)
	{
		TitleCollection usedList = new(site);
		this.UpdateMorph(template, baseSkillCost, usedList, this.baseSkill, site.Culture);
		this.UpdateMorph(template, baseSkillCost, usedList, this.morphs[0], site.Culture);
		this.UpdateMorph(template, baseSkillCost, usedList, this.morphs[1], site.Culture);
	}
	#endregion
}