namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Text;
	using RobinHood70.CommonCode;

	[Flags]
	public enum MechanicTypes
	{
		None = 0,
		Magicka = 1,
		Werewolf = 1 << 1,
		Stamina = 1 << 2,
		Ultimate = 1 << 3,
		MountStamina = 1 << 4,
		Health = 1 << 5,
		Daedric = 1 << 6,
	}

	internal sealed class Coefficient
	{
		#region Private Constants
		// private const int Level = 50 + 16;
		private const int Damage = 3000;
		private const int Health = 16000;
		private const int Resist = 10000;
		private const int Stat = 30000;
		#endregion

		#region Constructors
		public Coefficient(IDataRecord data, char num)
		{
			this.A = (float)data["a" + num];
			if (this.IsValid)
			{
				this.B = (float)data["b" + num];
				this.C = (float)data["c" + num];
				this.Mechanic = (sbyte)data["type" + num];
				if (this.Mechanic == -1)
				{
					var mechanicType = data.GetDataTypeName(data.GetOrdinal("mechanic"));
					var mechanicText = string.Equals(mechanicType, "INT", StringComparison.Ordinal)
						? ((int)data["mechanic"]).ToStringInvariant()
						: (string)data["mechanic"];
					if (mechanicText.Contains(',', StringComparison.Ordinal))
					{
						throw new InvalidOperationException("Type == -1 and Mechanic is split. It's unclear how this should be handled.");
					}

					this.Mechanic = int.Parse(mechanicText, CultureInfo.InvariantCulture);
				}
			}
		}
		#endregion

		#region Public Properties
		public float A { get; }

		public float B { get; }

		public float C { get; }

		public bool IsValid => this.A != -1;

		// Type, or mechanic if type not defined. Called Mechanic to make it obvious that they serve the same purpose.
		public int Mechanic { get; }

		//// public float R { get; }
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

		public static List<Coefficient> GetCoefficientList(IDataRecord data)
		{
			var coefficients = new List<Coefficient>(6);
			for (var num = '1'; num <= '6'; num++)
			{
				Coefficient coefficient = new(data, num);
				if (coefficient.IsValid)
				{
					coefficients.Add(coefficient);
				}
			}

			coefficients.TrimExcess();
			return coefficients;
		}
		#endregion

		#region Public Methods
		public string SkillDamageText()
		{
			// BitField Damage temporarily handles 0, 6, 10. After that's resolved, the check for BitFieldDamage should be altered to Mechanic > 0 rather than >=, or it might be possible to remove this check altogether and just move the BitField code up here.
			return
				this.Mechanic >= 0 ? this.BitFieldDamage() :
				this.Mechanic < -1 ? this.IndexedDamage() :
				throw new InvalidOperationException("Mechanic is invalid.");
		}
		#endregion

		#region Private Methods
		private string BitFieldDamage()
		{
			int CappedStat()
			{
				var value = (int)Math.Round(this.A * Stat);
				var maxValue = (int)Math.Round(this.B * Health);
				return value > maxValue ? maxValue : value;
			}

			switch (this.Mechanic)
			{
				case 0: // Old Magicka
				case 6: // Old Stamina
				case 10: // Old Ultimate
					var value = (int)Math.Round(this.A * Stat);
					var maxValue = (int)Math.Round(this.B * Health);
					//// Debug.WriteLine("Old stamina/ultimate (mechanic = 0/6/10) used!");
					return (value > maxValue ? maxValue : value).ToStringInvariant();
			}

			var resultList = new List<string>();
			var mechanicFlags = (MechanicTypes)this.Mechanic;
			foreach (var mechanicType in mechanicFlags.GetUniqueFlags())
			{
				var result = mechanicType switch
				{
					MechanicTypes.None => throw new InvalidOperationException("No bits were set."),
					MechanicTypes.Magicka => ((int)Math.Round(this.A * Stat)).ToStringInvariant(),
					MechanicTypes.Werewolf => ((int)Math.Round(this.A * Damage)).ToStringInvariant(), // Werewolf
					MechanicTypes.Stamina => ((int)Math.Round(this.A * Stat)).ToStringInvariant(),
					MechanicTypes.Ultimate => CappedStat().ToStringInvariant(),
					MechanicTypes.MountStamina => "0", // Mount Stamina
					MechanicTypes.Health =>
						((int)Math.Round(this.A * Health + this.C)).ToStringInvariant(),
					MechanicTypes.Daedric => "0", // Daedric
					_ => throw new InvalidOperationException("A bit was specified that has no current value."),
				};
				resultList.Add(result);
			}

			return string.Join(", ", resultList);
		}

		private string IndexedDamage()
		{
			/*
			 * Dave's source code for this is found in ComputeEsoSkillValue() in the (already cloned) esolog repository at: https://github.com/uesp/uesp-esolog.
			 * Note that Dave's calculations are significantly more complex, as they involve variable amounts. For the bot's purposes, constant amounts are used for key values, which simplifies many of the formulae.
			 */

			int value;
			int maxValue;
			var sb = new StringBuilder();
			switch (this.Mechanic)
			{
				case -2: // Health
					sb.Append((int)Math.Round(this.A * Health + this.C));
					break;
				case -56: // Spell + Weapon Damage
				case -50: // Ultimate (no weapon damage)
				case -68: // Magicka with Health Cap
					value = (int)Math.Round(this.A * Stat);
					maxValue = (int)Math.Round(this.B * Health);
					sb.Append(value > maxValue ? maxValue : value);
					break;
				case -71: // Spell Damage Capped
					value = (int)Math.Round(this.A * Damage + this.B);
					maxValue = (int)this.C;
					sb.Append(value > maxValue ? maxValue : value);
					break;
				case -72: // Magicka and Weapon Damage
					sb.Append((int)Math.Round(this.A * Stat + this.B * Damage + this.C));
					break;
				case -73: // Magicka and Spell Damage
					var halfMax = this.C / 2;
					var statDamage = this.A * Stat;
					if (statDamage > halfMax)
					{
						statDamage = halfMax;
					}

					var dmgDamage = this.B * Damage;
					if (dmgDamage > halfMax)
					{
						dmgDamage = halfMax;
					}

					value = (int)Math.Round(statDamage + dmgDamage);
					maxValue = (int)this.C;
					sb.Append(value > maxValue ? maxValue : value);
					break;
				case -74: // Weapon Power
				case -75: // Constant Value
				case -76: // Health or Spell Damage
				case -78: // Magicka and Light Armor(Health Capped)
				case -79: // Health or Weapon/Spell Damage
					throw new InvalidOperationException("Formula unknown");
				case -77: // Max Resistance
					sb.Append((int)Math.Round(this.A * Resist + this.C));
					break;
				case -51: // Light Armor #
				case -52: // Medium Armor #
				case -53: // Heavy Armor #
				case -54: // Dagger #
				case -55: // Armor Type #
				case -57: // Assassination Skills Slotted
				case -58: // Fighters Guild Skills Slotted
				case -59: // Draconic Power Skills Slotted
				case -60: // Shadow Skills Slotted
				case -61: // Siphoning Skills Slotted
				case -62: // Sorcerer Skills Slotted
				case -63: // Mages Guild Skills Slotted
				case -64: // Support Skills Slotted
				case -65: // Animal Companion Skills Slotted
				case -66: // Green Balance Skills Slotted
				case -67: // Winter's Embrace Slotted
				case -69: // Bone Tyrant Slotted
				case -70: // Grave Lord Slotted
					var roundA = ((double)this.A).RoundSignificant(3);
					sb
						.Append('(')
						.Append(roundA)
						.Append(" × ")
						.Append(EsoLog.MechanicNames[this.Mechanic])
						.Append(')');
					break;
				default:
					throw new InvalidOperationException($"Invalid {nameof(this.Mechanic)} {this.Mechanic.ToStringInvariant()}");
			}

			return sb.ToString();
		}
		#endregion
	}
}
