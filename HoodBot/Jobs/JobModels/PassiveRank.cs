namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using System.Data;

	internal sealed class PassiveRank
	{
		public PassiveRank(IDataRecord data)
		{
			this.Id = (int)data["id"];
			this.Rank = (sbyte)data["rank"];
			this.LearnedLevel = (int)data["learnedLevel"];
			var description = (string)data["coefDescription"];
			if (string.IsNullOrWhiteSpace(description))
			{
				description = (string)data["description"];
			}

			this.Description = EsoSpace.HarmonizeDescription(description);

			List<Coefficient> coefficients = new(3);
			for (var i = '1'; i <= '3'; i++)
			{
				Coefficient coefficient = new(data, i);
				if (coefficient.IsValid)
				{
					coefficients.Add(coefficient);
				}
			}

			coefficients.TrimExcess();
			this.Coefficients = coefficients;
		}

		public IReadOnlyList<Coefficient> Coefficients { get; }

		public string Description { get; }

		public int Id { get; }

		public int LearnedLevel { get; }

		public sbyte Rank { get; }
	}
}
