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

internal sealed class ActiveSkill : Skill
{
	#region Fields
	private readonly int learnedLevel;
	private readonly string skillType;

	private Morph? baseSkill = null;
	#endregion

	#region Constructors
	public ActiveSkill(IDataRecord row)
		: base(row)
	{
		this.learnedLevel = (int)row["learnedLevel"];
		this.skillType = ((string)row["icon"]).Contains("_artifact_", StringComparison.OrdinalIgnoreCase)
		? "Artifact"
		: EsoLog.ConvertEncoding((string)row["type"]);
	}
	#endregion

	#region Private Properties
	public Morph BaseSkill => this.baseSkill ?? throw new InvalidOperationException();

	public Dictionary<string, Morph> Morphs { get; } = new(StringComparer.Ordinal);
	#endregion

	#region Public Override Methods
	public override void AddData(IDataRecord row, Dictionary<long, List<Coefficient>> coefficients)
	{
		ArgumentNullException.ThrowIfNull(row);
		var newMorph = new Morph(row);
		if (!this.Morphs.TryGetValue(newMorph.Name, out var morph))
		{
			morph = newMorph;
			this.Morphs.Add(morph.Name, morph);
			if (morph.Index == 0)
			{
				this.baseSkill = morph;
			}
		}

		var abilityId = (long)row["abilityId"];
		if (!coefficients.TryGetValue(abilityId, out var coefs))
		{
			coefs = [];
		}

		morph.Ranks.Add(new ActiveRank(row, coefs));
	}

	public override bool IsValid()
	{
		var retval = true;
		if (this.baseSkill is null)
		{
			Debug.WriteLine($"{this.Name} has no base skill.");
			retval = false;
		}

		if (this.Morphs.Count == 0)
		{
			Debug.WriteLine($"{this.Name} has no morphs.");
			retval = false;
		}

		foreach (var morph in this.Morphs.Values)
		{
			retval &= morph.IsValid();
		}

		return retval;
	}

	public override void PostProcess()
	{
		foreach (var morph in this.Morphs.Values)
		{
			morph.Description = morph.GetParsedDescription();
		}
	}

	public override ChangeType GetChangeType(Skill oldVer)
	{
		if (oldVer is not ActiveSkill oldSkill)
		{
			throw new InvalidOperationException();
		}

		if (this.Morphs.Count != oldSkill.Morphs.Count)
		{
			return ChangeType.Major;
		}

		var retval = ChangeType.None;
		foreach (var kvp in this.Morphs)
		{
			if (!oldSkill.Morphs.TryGetValue(kvp.Key, out var oldMorph))
			{
				return ChangeType.Major;
			}

			retval |= kvp.Value.GetChangeType(oldMorph);
		}

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
		template.UpdateOrRemove("channeltime", FormatSeconds(EsoSpace.TimeToText(baseRank.ChannelTime)), ParameterFormat.OnePerLine, baseRank.ChannelTime == -1);
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
		var morphCastingTime = Morph.NowrapSameString(morph.Ranks.Select(r => EsoSpace.TimeToText(r.CastingTime)));
		if (morph.Ranks[^1].CastingTime != baseRank.CastingTime)
		{
			descriptions.Add("Casting Time: " + FormatSeconds(morphCastingTime));
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
		foreach (var morph in this.Morphs)
		{
			this.UpdateMorph(template, baseSkillCost, usedList, morph.Value, site.Culture);
		}
	}
	#endregion
}