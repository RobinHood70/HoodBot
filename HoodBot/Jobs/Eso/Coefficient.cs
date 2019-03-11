namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.WikiCommon;

	internal class Coefficient
	{
		public Coefficient(IDataRecord data, char num)
		{
			this.A = (float)data["a" + num];
			if (this.IsValid)
			{
				this.B = (float)data["b" + num];
				this.C = (float)data["c" + num];
				//// this.R = (float)data["R" + num];
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

		public string MechanicName => EsoGeneral.MechanicNames[this.TypeNumber];

		//// public float R { get; }

		public int TypeNumber { get; }
		#endregion

		#region Public Static Methods
		public static Coefficient FromCollection(IReadOnlyList<Coefficient> list, string text)
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
			// Dave's source code for this is found in ComputeEsoSkillValue() here:
			// https://bitbucket.org/uesp/esolog/src/3f1e33ac0339b75c788259af35597662c7c6ef80/resources/esoskills.js?at=default
			switch (this.TypeNumber)
			{
				case -2:
					return (int)Math.Round(this.A * Ability.Health + this.C);
				case -56:
				case -50:
				case 0:
				case 6:
				case 10:
					return (int)Math.Round(this.A * Ability.Stat + this.B * Ability.Damage + this.C);
				case var n when n >= -67 && n <= -51:
					return (int)this.A;
				case -68:
				case -69:
					var value = (int)Math.Round(this.A * Ability.Stat);
					var maxValue = (int)Math.Round(this.B * Ability.Health);
					return value > maxValue ? maxValue : value;
				default:
					throw new InvalidOperationException($"Invalid {nameof(this.TypeNumber)} {this.TypeNumber}");
			}
		}
		#endregion
	}
}
