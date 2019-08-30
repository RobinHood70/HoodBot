namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;
	using System.Data;

	internal class Ability
	{
		#region Fields
		internal const int Level = 50 + 16;
		internal const int Health = 16000;
		internal const int Damage = 3000;
		internal const int Stat = 30000;
		#endregion

		#region Constructors
		public Ability(IDataRecord data)
		{
			this.Id = (int)data["id"];
			if (!ReplacementData.IdReplacements.TryGetValue(this.Id, out var desc))
			{
				desc = (string)data["coefDescription"];
				if (string.IsNullOrWhiteSpace(desc))
				{
					desc = (string)data["description"];
				}
			}

			desc = EsoGeneral.HarmonizeDescription(desc);
			desc = EsoReplacer.ReplaceGlobal(desc, null);
			this.Description = desc;

			var coefficients = new List<Coefficient>();
			for (var num = '1'; num <= '6'; num++)
			{
				var coefficient = new Coefficient(data, num);
				if (coefficient.IsValid)
				{
					coefficients.Add(coefficient);
				}
			}

			this.Coefficients = coefficients;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<Coefficient> Coefficients { get; }

		public string Description { get; private set; }

		public int Id { get; }
		#endregion
	}
}