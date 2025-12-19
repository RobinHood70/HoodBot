namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;

internal sealed class ActiveRank(int castingTime, int channelTime, IReadOnlyList<Coefficient> coefficients, IReadOnlyList<Cost> costs, string description, int duration, long id, string radius, string range, sbyte rank)
{
	#region Public Properties
	public int CastingTime { get; } = castingTime;

	public int ChannelTime { get; } = channelTime;

	public IReadOnlyList<Coefficient> Coefficients { get; } = coefficients;

	public IReadOnlyList<Cost> Costs { get; } = costs;

	public string Description { get; } = description;

	public int Duration { get; } = duration;

	public long Id { get; } = id;

	public sbyte Rank { get; } = rank;

	public string Radius { get; } = radius;

	public string Range { get; } = range;
	#endregion

	#region Public Methods
	public ChangeType GetChangeType(ActiveRank previous)
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
}