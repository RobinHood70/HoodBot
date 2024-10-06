namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Linq;
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
			string[] costValues;
			string[] costTimeValues;
			string[] mechanicValues;
			var costField = row["cost"];
			var costTimeField = row["costTime"];
			var mechanicField = row["mechanic"];
			if (costField is string costText)
			{
				costValues = costText.Split(TextArrays.Comma);
				costTimeValues = ((string)costTimeField).Split(TextArrays.Comma);
				mechanicValues = EsoLog.ConvertEncoding((string)row["mechanic"]).Split(TextArrays.Comma);
			}
			else
			{
				costValues = [((int)costField).ToStringInvariant()];
				costTimeValues = [((int)costTimeField).ToStringInvariant()];
				mechanicValues = [((int)mechanicField).ToStringInvariant()];
			}

			if (costValues.Length != mechanicValues.Length)
			{
				throw new InvalidOperationException("Costs and mechanics have different lengths.");
			}

			for (var i = 0; i < costValues.Length; i++)
			{
				var costValue = costValues[i];
				costValue = costValue.Length > 0
					? costValue.Split(TextArrays.Period)[0].TrimStart('0')
					: null;
				var costTimeValue = costTimeValues.Length > i
					? costTimeValues[i]
					: null;
				costTimeValue = costTimeValue?.Length > 0
					? costTimeValue.Split(TextArrays.Period)[0].TrimStart('0')
					: null;
				var mechanic = int.Parse(mechanicValues[i], CultureInfo.InvariantCulture);
				var mechanicText = EsoLog.MechanicNames[mechanic];

				this.Costs.Add(new Cost(costValue, costTimeValue, mechanicText));
			}
		}
		#endregion

		#region Public Properties
		public string ChannelTime { get; }

		public IList<Cost> Costs { get; } = [];

		public string Duration { get; }

		public string Radius { get; }

		public string Range { get; }
		#endregion

		#region Public Methods
		public Cost GetCostSplit()
		{
			if (this.Costs.Count == 1)
			{
				return this.Costs[0];
			}

			var valueText = string.Join(", ", this.Costs.Select(c => c.Value));
			var valuePerTimeText = string.Join(", ", this.Costs.Select(c => c.ValuePerTime));
			var mechanicText = this.Costs.Count switch
			{
				1 => this.Costs[0].MechanicText,
				2 =>
					this.Costs[0].MechanicText + " and " +
					this.Costs[1].MechanicText,
				3 =>
					this.Costs[0].MechanicText + ", " +
					this.Costs[1].MechanicText + ", and " +
					this.Costs[2].MechanicText,
				_ => throw new InvalidOperationException($"Costs has {this.Costs.Count} values which is not currently handled")
			};

			return new Cost(valueText, valuePerTimeText, mechanicText);
		}
		#endregion
	}
}