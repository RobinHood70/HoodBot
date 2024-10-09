namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using RobinHood70.CommonCode;

	internal sealed class ActiveRank : Rank
	{
		#region Constructors
		public ActiveRank(IDataRecord row)
			: base(row)
		{
			static string FormatRange(int num) => ((double)num / 100).ToString("0.##", CultureInfo.InvariantCulture);

			var channelTime = (int)row["channelTime"];
			this.ChannelTime = EsoSpace.TimeToText(channelTime < 0 ? 0 : channelTime);
			this.Duration = EsoSpace.TimeToText((int)row["duration"]);
			this.Radius = FormatRange((int)row["radius"]);
			var maxRange = FormatRange((int)row["maxRange"]);
			var minRange = FormatRange((int)row["minRange"]);
			this.Range = string.Equals(minRange, "0", StringComparison.Ordinal) ? maxRange : string.Concat(minRange, "-", maxRange);

			this.Costs.AddRange(GetCostSplit(row, "cost", "mechanic", false));
			this.Costs.AddRange(GetCostSplit(row, "costTime", "mechanicTime", true));
		}
		#endregion

		#region Public Properties
		public string ChannelTime { get; }

		public IList<Cost> Costs { get; } = [];

		public string Duration { get; }

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
}