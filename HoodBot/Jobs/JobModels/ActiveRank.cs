namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using RobinHood70.CommonCode;

internal sealed class ActiveRank : Rank
{
	#region Constructors
	public ActiveRank(IDataRecord row, List<Coefficient> coefficients)
		: base(row, coefficients)
	{
		static string FormatRange(int num) => ((double)num / 100).ToString("0.##", CultureInfo.InvariantCulture);

		this.ChannelTime = (int)row["channelTime"];
		this.Duration = (int)row["duration"];
		this.Radius = FormatRange((int)row["radius"]);
		var maxRange = FormatRange((int)row["maxRange"]);
		var minRange = FormatRange((int)row["minRange"]);
		this.Range = minRange.OrdinalEquals("0") ? maxRange : string.Concat(minRange, "-", maxRange);

		this.Costs.AddRange(GetCostSplit(row, "cost", "mechanic", false));
		this.Costs.AddRange(GetCostSplit(row, "costTime", "mechanicTime", true));
		this.Factor = this.Index switch
		{
			1 => 1.0,
			2 => 1.011,
			3 => 1.022,
			4 => 1.033,
			_ => throw new InvalidOperationException($"Invalid RankNum: {this.Index}")
		};
	}
	#endregion

	#region Public Properties
	public int ChannelTime { get; }

	public IList<Cost> Costs { get; } = [];

	public int Duration { get; }

	public string Radius { get; }

	public string Range { get; }
	#endregion

	#region Private Static Methods
	private static IEnumerable<Cost> GetCostSplit(IDataRecord row, string costsName, string mechanicsName, bool perTime)
	{
		var costsText = (string)row[costsName];
		if (string.IsNullOrEmpty(costsText))
		{
			yield break;
		}

		var mechanicsText = (string)row[mechanicsName];
		var costs = costsText.Split(TextArrays.Comma);
		var mechanics = mechanicsText.Split(TextArrays.Comma);
		if (costs.Length != mechanics.Length)
		{
			throw new InvalidOperationException("Costs and mechanics have different lengths.");
		}

		for (var i = 0; i < costs.Length; i++)
		{
			if (int.Parse(costs[i], CultureInfo.InvariantCulture) != 0)
			{
				var mechanicNum = int.Parse(mechanics[i], CultureInfo.InvariantCulture);
				var mechanic = EsoLog.MechanicNames[mechanicNum] + (perTime ? " / 1s" : string.Empty);
				yield return new Cost(costs[i], mechanic);
			}
		}
	}
	#endregion
}