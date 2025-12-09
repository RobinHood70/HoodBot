namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using RobinHood70.CommonCode;

internal sealed class PassiveRank(IReadOnlyList<Coefficient> coefficients, string description, long id, int learnedLevel, sbyte rank)
{
	#region Public Properties
	public IReadOnlyList<Coefficient> Coefficients { get; } = coefficients;

	public string Description { get; } = description;

	public long Id { get; } = id;

	public int LearnedLevel { get; } = learnedLevel;

	public sbyte Rank { get; } = rank;
	#endregion

	#region Public Methods
	public ChangeType GetChangeType(PassiveRank previous)
	{
		if (this.Rank != previous.Rank)
		{
			throw new InvalidOperationException("I don't think this is possible.");
		}

		if (this.Description.OrdinalEquals(previous.Description))
		{
			return ChangeType.None;
		}

		var curSkill = Skill.HighlightVar.Replace(this.Description, " ");
		var oldSkill = Skill.HighlightVar.Replace(previous.Description, " ");
		return curSkill.OrdinalICEquals(oldSkill)
			? ChangeType.Minor
			: ChangeType.Major;
	}
	#endregion

	#region Private Methods
	public string FormatDescription()
	{
		var retval = new List<string>();
		var splitDescription = Coefficient.RawCoefficient.Split(this.Description);
		for (var i = 0; i < splitDescription.Length; i++)
		{
			var text = splitDescription[i];
			if ((i & 1) == 1)
			{
				var coefNum = int.Parse(splitDescription[i], CultureInfo.InvariantCulture) - 1;
				var coef = this.Coefficients[coefNum];
				text = coef.SkillDamageText(1.0);
				text = coef.CoefficientType == -75
					? EsoLog.FloatFinder.Replace(text, "'''$&'''")
					: $"'''{text}'''{coef.DamageSuffix}";
			}

			retval.Add(text);
		}

		return string.Concat(retval);
	}
	#endregion
}