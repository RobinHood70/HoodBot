namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.CommonCode;

	internal sealed class Coefficient
	{
		public Coefficient(IDataRecord data, char num)
		{
			this.A = (float)data['a' + num];
			if (this.IsValid)
			{
				this.B = (float)data['b' + num];
				this.C = (float)data['c' + num];
				//// this.R = (float)data['R' + num];
				var typeNumber = (int)(sbyte)data["type" + num];
				if (typeNumber == -1)
				{
					typeNumber = (int)data["mechanic"];
				}

				this.TypeNumber = typeNumber;
			}
		}

		#region Public Properties
		public float A { get; }

		public float B { get; }

		public float C { get; }

		public bool IsValid => this.A != -1;

		public string MechanicName => EsoLog.MechanicNames[this.TypeNumber];

		//// public float R { get; }

		public int TypeNumber { get; }
		#endregion

		#region Public Static Methods
		public static Coefficient? FromCollection(IReadOnlyList<Coefficient> list, string text)
		{
			if (text?.Length > 0 && text[0] == '$')
			{
				var coefNumText = text[1];
				var coefNum = (int)char.GetNumericValue(coefNumText) - 1;
				var coef = list[coefNum];

				return coef;
			}

			return null;
		}
		#endregion

		#region Public Methods
		public string SkillDamageText() => this.SkillDamage().ToStringInvariant();
		#endregion

		#region Private Methods
		private int SkillDamage()
		{
			int value;
			int maxValue;

			// Dave's source code for this is found in ComputeEsoSkillValue() in the (already cloned) esolog repository at:
			// https://github.com/uesp/uesp-esolog
			// Note that Dave's calculations are significantly more complex, as they involve variable amounts. For the bot's purposes, constant amounts are used for key values, which simplifies many of the formulae.
			switch (this.TypeNumber)
			{
				case -2:
					return (int)Math.Round(this.A * Ability.Health + this.C);
				case -56:
				case -50:
				case 0:
				case 6:
				case 10:
				case -68:
					value = (int)Math.Round(this.A * Ability.Stat);
					maxValue = (int)Math.Round(this.B * Ability.Health);
					return value > maxValue ? maxValue : value;
				case -71:
					value = (int)Math.Round(this.A * Ability.Damage + this.B);
					maxValue = (int)this.C;
					return value > maxValue ? maxValue : value;
				case -72:
					return (int)Math.Round(this.A * Ability.Stat + this.B * Ability.Damage + this.C);
				case -73:
					var halfMax = this.C / 2;
					var statDamage = this.A * Ability.Stat;
					if (statDamage > halfMax)
					{
						statDamage = halfMax;
					}

					var dmgDamage = this.B * Ability.Damage;
					if (dmgDamage > halfMax)
					{
						dmgDamage = halfMax;
					}

					value = (int)Math.Round(statDamage + dmgDamage);
					maxValue = (int)this.C;
					return value > maxValue ? maxValue : value;
				case -77:
					return (int)Math.Round(this.A * Ability.Resist + this.C);
				case var n when n is >= -70 and <= -51:
					return (int)this.A;
				default:
					throw new InvalidOperationException($"Invalid {nameof(this.TypeNumber)} {this.TypeNumber.ToStringInvariant()}");
			}
		}
		#endregion
	}
}
