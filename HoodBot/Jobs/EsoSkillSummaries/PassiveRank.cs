namespace RobinHood70.HoodBot.Jobs.EsoSkillSummaries
{
	using System.Collections.Generic;
	using System.Data;

	internal class PassiveRank
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

			this.Description = EsoGeneral.HarmonizeDescription(description);

			var coefficients = new List<Coefficient>(3);
			for (var i = '1'; i <= '3'; i++)
			{
				var coefficient = new Coefficient(data, i);
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
