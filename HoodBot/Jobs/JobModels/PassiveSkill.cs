namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon.Parser;

internal sealed class PassiveSkill(string name, string pageName, string skillClass, string skillLine, sbyte maxRank) : Skill(name, pageName, skillClass, skillLine)
{
	#region Public Properties
	public sbyte MaxRank { get; } = maxRank;

	internal List<PassiveRank> Ranks { get; } = [];
	#endregion

	#region Public Override Methods
	public override void AddData(IDataRecord row, Dictionary<long, List<Coefficient>> coefficients) => this.Ranks.Add(RankFromRow(row, coefficients));

	public override ChangeType GetChangeType(Skill previous)
	{
		if (previous is not PassiveSkill prevSkill)
		{
			throw new InvalidOperationException();
		}

		if (this.Ranks.Count != prevSkill.Ranks.Count)
		{
			Debug.WriteLine($"[[{this.Name}]] changed # of ranks between current and previous version. This is probably a sign of a bug somewhere.");
			return ChangeType.Major;
		}

		var retval = ChangeType.None;
		for (var i = 0; i < this.Ranks.Count; i++)
		{
			var curRank = this.Ranks[i];
			var prevRank = prevSkill.Ranks[i];
			var changeType = curRank.GetChangeType(prevRank);
			if (changeType > retval)
			{
				retval |= changeType;
			}
		}

		return retval;
	}

	public override bool IsValid()
	{
		var retval = true;
		if (this.Ranks[^1].Rank != this.MaxRank)
		{
			Debug.WriteLine($"Warning: {this.Name} has the wrong maximum rank ({this.Ranks[^1].Rank} vs. {this.MaxRank}).");
			retval = false;
		}

		foreach (var rank in this.Ranks)
		{
			if (string.IsNullOrWhiteSpace(rank.Description))
			{
				Debug.WriteLine($"Warning: {this.Name} - Rank {rank.Rank.ToStringInvariant()} has no description.");
				retval = false;
			}
		}

		return retval;
	}

	public override void PostProcess()
	{
	}
	#endregion

	#region Protected Override Methods
	protected override void UpdateTemplate(Site site, ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(template);
		template.Update("type", "Passive", ParameterFormat.OnePerLine, true);
		template.Update("id", this.Ranks[^1].Id.ToStringInvariant(), ParameterFormat.OnePerLine, true);
		TitleCollection usedList = new(site);
		foreach (var rank in this.Ranks)
		{
			var rankText = rank.Rank.ToStringInvariant();
			var paramName = "desc" + (rank.Rank == 1 ? string.Empty : rankText);
			var description = rank.FormatDescription();
			UpdateParameter(template, paramName, description, usedList, this.Name);
			template.Update("linerank" + rankText, rank.LearnedLevel.ToStringInvariant(), ParameterFormat.OnePerLine, true);
		}
	}
	#endregion

	#region Private Static Methods
	private static PassiveRank RankFromRow(IDataRecord row, Dictionary<long, List<Coefficient>> coefficients)
	{
		var id = (long)row["abilityId"];
		return new PassiveRank(
			coefficients: coefficients.GetValueOrDefault(id, []),
			description: EsoLog.GetRankDescription(id, row),
			id: id,
			rank: (sbyte)row["rank"],
			learnedLevel: (int)row["learnedLevel"]);
	}
	#endregion
}