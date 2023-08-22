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

			this.ChannelTime = EsoSpace.TimeToText((int)row["channelTime"]);
			this.Duration = EsoSpace.TimeToText((int)row["duration"]);
			this.Radius = FormatRange((int)row["radius"]);
			var maxRange = FormatRange((int)row["maxRange"]);
			var minRange = FormatRange((int)row["minRange"]);
			this.Range = string.Equals(minRange, "0", StringComparison.Ordinal) ? maxRange : string.Concat(minRange, "-", maxRange);
			string[] costValues;
			string[] mechanicValues;
			var costField = row["cost"];
			var mechanicField = row["mechanic"];
			if (costField is string costText)
			{
				costValues = costText.Split(TextArrays.Comma);
				mechanicValues = ((string)row["mechanic"]).Split(TextArrays.Comma);
			}
			else
			{
				costValues = new string[] { ((int)costField).ToStringInvariant() };
				mechanicValues = new string[] { ((int)mechanicField).ToStringInvariant() };
			}

			if (costValues.Length != mechanicValues.Length)
			{
				throw new InvalidOperationException("Costs and mechanics have different lengths.");
			}

			for (var i = 0; i < costValues.Length; i++)
			{
				var costValue = costValues[i].Length == 0
					? -1
					: int.Parse(costValues[i], CultureInfo.InvariantCulture);
				var mechanic = int.Parse(mechanicValues[i], CultureInfo.InvariantCulture);
				this.Costs.Add(new Cost(costValue, mechanic));
			}
		}
		#endregion

		#region Public Properties
		public string ChannelTime { get; }

		public IList<Cost> Costs { get; } = new List<Cost>();

		public string Duration { get; }

		public string Radius { get; }

		public string Range { get; }
		#endregion

		#region Public Methods
		public (string ValueText, string MechanicText) GetCostSplit()
		{
			string valueText;
			if (this.Costs.Count == 1)
			{
				var value = this.Costs[0].Value;
				if (value <= 0)
				{
					return ("Free", string.Empty);
				}

				valueText = value.ToStringInvariant();
			}
			else
			{
				valueText = string.Join(", ", this.Costs.Select(c => c.Value.ToStringInvariant()));
			}

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

			return (valueText, mechanicText);
		}
		#endregion
	}
}