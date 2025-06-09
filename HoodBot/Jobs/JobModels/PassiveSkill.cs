namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon.Parser;

internal sealed class PassiveSkill(IDataRecord row, List<Coefficient> coefficients) : Skill(row)
{
	#region Fields
	private readonly List<Rank> ranks = [];
	#endregion

	#region Public Properties
	public IReadOnlyList<Coefficient> Coefficients => coefficients;
	#endregion

	#region Public Override Methods
	public override void AddData(IDataRecord row, List<Coefficient> coefficients)
	{
		var rank = new PassiveRank(row);
		this.ranks.Add(rank);
	}

	public override bool IsValid()
	{
		var retval = false;
		if (this.ranks.Count is < 1 or > 10)
		{
			retval = true;
			Debug.WriteLine($"Warning: {this.Name} has an unusual number of ranks ({this.ranks.Count}).");
		}

		foreach (var rank in this.ranks)
		{
			if (string.IsNullOrWhiteSpace(rank.Description))
			{
				retval = true;
				Debug.WriteLine($"Warning: {this.Name} - Rank {rank.Index.ToStringInvariant()} has no description.");
			}
		}

		return retval;
	}

	public override ChangeType GetChangeType(Skill previous)
	{
		if (previous is not PassiveSkill prevSkill)
		{
			throw new InvalidOperationException();
		}

		if (this.ranks.Count != prevSkill.ranks.Count)
		{
			Debug.WriteLine($"[[{this.PageName}]] changed # of ranks between current and previous version. This is probably a sign of a bug somewhere.");
			return ChangeType.Major;
		}

		var retval = ChangeType.None;
		for (var i = 0; i < this.ranks.Count; i++)
		{
			var curRank = this.ranks[i];
			var prevRank = prevSkill.ranks[i];
			var changeType = curRank.GetChangeType(prevRank);
			if (changeType > retval)
			{
				retval |= changeType;
			}
		}

		return retval;
	}
	#endregion

	#region Protected Override Methods
	protected override void UpdateTemplate(Site site, ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(template);
		template.Update("type", "Passive", ParameterFormat.OnePerLine, true);
		template.Update("id", this.ranks[^1].Id.ToStringInvariant(), ParameterFormat.OnePerLine, true);
		TitleCollection usedList = new(site);
		foreach (var rank in this.ranks)
		{
			var rankText = rank.Index.ToStringInvariant();
			var paramName = "desc" + (rank.Index == 1 ? string.Empty : rankText);
			var description = this.FormatRankDescription(rank);
			UpdateParameter(template, paramName, description, usedList, this.Name);
			if (rank is PassiveRank passiveRank)
			{
				template.Update("linerank" + rankText, passiveRank.LearnedLevel.ToStringInvariant(), ParameterFormat.OnePerLine, true);
			}
		}
	}
	#endregion

	#region Private Methods

	private string FormatRankDescription(Rank rank)
	{
		var retval = new List<string>();
		var splitDescription = Coefficient.RawCoefficient.Split(rank.Description);
		for (var i = 0; i < splitDescription.Length; i++)
		{
			var coefNum = int.Parse(splitDescription[i], CultureInfo.InvariantCulture);
			var coef = this.Coefficients[coefNum];
			var text = coef.SkillDamageText(rank.Factor);

			if ((i & 1) == 1)
			{
				if (i == 1 && splitDescription[0].Length == 0)
				{
					text = "<small>(" + text + ")</small>";
				}

				// Descriptions used to be done with Join("'''") but in practice, this is unintuitive, so we surround every other value with bold instead.
				text = "'''" + text + "'''";
			}

			retval.Add(text);
		}

		return string.Concat(retval);
	}
	#endregion
}