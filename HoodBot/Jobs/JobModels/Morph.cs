namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.CommonCode;

	internal sealed class Morph
	{
		#region Constructors
		public Morph(IDataRecord data)
		{
			this.Name = (string)data["name"];
			this.CastingTime = EsoGeneral.TimeToText((int)data["castTime"]);
			this.EffectLine = (string)data["effectLines"];
			this.Target = (string)data["target"];
			this.Mechanic = (int)data["mechanic"];
		}
		#endregion

		#region Public Properties
		public IList<Ability> Abilities { get; } = new List<Ability>(4);

		//// public string BaseCost => this.FullName(this.Costs.ToString());

		public string CastingTime { get; }

		public VariableData<string> ChannelTimes { get; } = new VariableData<string>();

		public VariableData<int> Costs { get; } = new VariableData<int>();

		public string? Description { get; private set; }

		public VariableData<string> Durations { get; } = new VariableData<string>();

		public string EffectLine { get; }

		public int Mechanic { get; }

		public string Name { get; }

		public VariableData<string> Radii { get; } = new VariableData<string>();

		public VariableData<string> Ranges { get; } = new VariableData<string>();

		public string Target { get; }
		#endregion

		#region Public Methods
		public string FullCost()
		{
			VariableData<int> calcCosts = new();
			foreach (var maxCost in this.Costs)
			{
				// Actual cost is from ComputeEsoSkillCost() in ESO Log. Actual formula as of this writing is: maxCost * level / 72 + maxCost / 12, which reduces to maxCost * (level + 6) / 72). When level is set to a constant of 66 (the maximum level, as used on the wiki), this collapses to just maxCost.
				calcCosts.Add(maxCost);
			}

			return this.FullName(calcCosts.ToString() ?? string.Empty);
		}

		public string FullName(string cost) => string.Equals(cost, "0", StringComparison.Ordinal)
			? "Free"
			: string.Concat(cost, " ", EsoLog.MechanicNames[this.Mechanic]);
		#endregion

		#region Private Methods
		internal void ParseDescription()
		{
			var splitDescriptions = this.GetDescriptions();
			var splitLength = splitDescriptions[0].Length;

			var errors = false;
			List<VariableData<string>> variableDescriptions = new(splitLength);
			for (var i = 0; i < splitLength; i++)
			{
				VariableData<string> data = new();
				for (var j = 0; j < this.Abilities.Count; j++)
				{
					try
					{
						Coefficient? coef = Coefficient.FromCollection(this.Abilities[j].Coefficients, splitDescriptions[j][i]);
						if (coef != null)
						{
							splitDescriptions[j][i] = coef.SkillDamageText();
							if (coef.TypeNumber < -50)
							{
								// This gets executed redundantly at each ability, but I couldn't figure out a better way to do this.
								data.TextBefore = "(";
								data.TextAfter = " × " + coef.MechanicName + ")";
							}
						}
					}
					catch (IndexOutOfRangeException)
					{
						Debug.WriteLine($"Coefficient error on Dave's end in {this.Name}.");
					}

					data.Add(splitDescriptions[j][i]);
				}

				if ((i & 1) == 0 && !data.AllEqual)
				{
					Debug.WriteLine($"\nDescription mismatches in {this.Name}.");
					foreach (var abil in this.Abilities)
					{
						Debug.WriteLine($"{abil.Id.ToStringInvariant()}: {abil.Description}");
					}

					foreach (var dataItem in data)
					{
						Debug.WriteLine(dataItem);
					}

					errors = true;
				}
				else
				{
					variableDescriptions.Add(data);
				}
			}

			if (!errors)
			{
				List<string> descriptions = new();
				for (var i = 0; i < variableDescriptions.Count; i++)
				{
					// Descriptions used to be done with Join("'''") but in practice, this is unintuitive, so we surround every other value with bold instead.
					var fragment = variableDescriptions[i].ToString() ?? string.Empty;
					descriptions.Add((i & 1) == 1 ? "'''" + fragment + "'''" : fragment);
				}

				this.Description = string.Concat(descriptions);
			}
		}

		private IList<string[]> GetDescriptions()
		{
			List<string[]> splitDescriptions = new(this.Abilities.Count);
			var splitLength = 0;
			var isBad = false;
			foreach (var ability in this.Abilities)
			{
				var split = Skill.Highlight.Split(ability.Description);
				if (split.Length != splitLength)
				{
					if (splitLength == 0)
					{
						splitLength = split.Length;
					}
					else
					{
						isBad = true;
					}
				}

				splitDescriptions.Add(split);
			}

			if (isBad)
			{
				Debug.WriteLine($"Split lengths not equal for {this.Name}");
				foreach (var abil in this.Abilities)
				{
					Debug.WriteLine($"{abil.Id.ToStringInvariant()}: {abil.Description}");
				}

				throw new InvalidOperationException("Split lengths not equal!");
			}

			return splitDescriptions;
		}
		#endregion
	}
}