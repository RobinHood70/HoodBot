namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
	public Coefficient(IDataRecord row, int index)
	{
		if (index is < 0 or > 5)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		var num = "123456"[index];
		this.A = (float)row["a" + num];
		this.B = (float)row["b" + num];
		this.C = (float)row["c" + num];
		//// this.R = (float)row["R" + num];
		this.Mechanic = (sbyte)row["type" + num];

		if (this.IsValid() && this.Mechanic == -1)
		{
			var mechanicType = row.GetDataTypeName(row.GetOrdinal("mechanic"));
			var mechanicText = mechanicType.OrdinalEquals("INT")
				? ((int)row["mechanic"]).ToStringInvariant()
				: EsoLog.ConvertEncoding((string)row["mechanic"]);
			if (mechanicText.Contains(',', StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Type == -1 and Mechanic is split. It's unclear how this should be handled.");
			}

			this.Mechanic = int.Parse(mechanicText, CultureInfo.InvariantCulture);
		}
	}
	#endregion

	#region Public Properties
	public float A { get; }

	public float B { get; }

	public float C { get; }

	// Type, or mechanic if type not defined. Called Mechanic to make it obvious that they serve the same purpose.
	public int Mechanic { get; }

	//// public float R { get; }
	#endregion

	#region Public Static Methods
	public static IReadOnlyList<Coefficient> GetCoefficientList(IDataRecord row)
	{
		var coefficients = new Coefficient[6];
		for (var index = 0; index <= 5; index++)
		{
			coefficients[index] = new Coefficient(row, index);
		}

		return coefficients;
	}

	public static string GetCoefficientText(IReadOnlyList<Coefficient> coefs, string text, string skillName)
	{
		if (text.Length == 2 && text[0] == '$')
		{
			var index = text[1] - '1';
			var coef = coefs[index];
			if (!coef.IsValid())
			{
				Debug.WriteLine($"Coefficient error in {skillName}, coefficient {text[1]}.");
				return text;
			}

			return coef.SkillDamageText();
		}

		return text;
	}
	#endregion

	#region Public Methods

	public bool IsValid() => this.A != -1 || this.B != -1 || this.C != -1;

	// BitField Damage temporarily handles 0, 6, 10. After that's resolved, the check for BitFieldDamage should be altered to Mechanic > 0 rather than >=, or it might be possible to remove this check altogether and just move the BitField code up here.
	public string SkillDamageText() => this.Mechanic switch
	{
		> -1 => this.BitFieldDamage(),
		< -1 => this.IndexedDamage(),
		_ => throw new InvalidOperationException("Mechanic is invalid."),
	};
	#endregion

	#region Private Methods
	private static double ToPrecision(double num, int digits)
	{
		if (num == 0)
		{
			return 0;
		}

		var unsig = Math.Floor(Math.Log10(Math.Abs(num))) + 1;
		var scale = Math.Pow(10, unsig);
		return scale * Math.Round(num / scale, digits);
	}
	#endregion

	#region Private Methods
	private string BitFieldDamage()
	{
		var a = this.A; // ToPrecision(this.A, 5);
		var b = this.B; // ToPrecision(this.B, 5);
		var c = this.C; // ToPrecision(this.C, 5);

		//// var r = this.R; // ToPrecision(this.R, 5);

		double CappedStat()
		{
			var value = a * Stat;
			var maxValue = b * Damage;
			return value + maxValue + c;
		}

		string ToText(double result) => ((int)Math.Round(result)).ToStringInvariant();

		var resultList = new List<string>();
		switch (this.Mechanic)
		{
			case 0: // Old Magicka
			case 6: // Old Stamina
			case 10: // Old Ultimate
				var value = a * Stat;
				var maxValue = b * Health;
				//// Debug.WriteLine("Old stamina/ultimate (mechanic = 0/6/10) used!");
				resultList.Add(ToText(value > maxValue ? maxValue : value));
				break;
			default:
				var mechanicFlags = (MechanicTypes)this.Mechanic;
				foreach (var mechanicType in mechanicFlags.GetUniqueFlags())
				{
					var result = mechanicType switch
					{
						MechanicTypes.None => throw new InvalidOperationException("No bits were set."),
						MechanicTypes.Magicka => a * Stat,
						MechanicTypes.Werewolf => a * Damage, // Werewolf
						MechanicTypes.Stamina => a * Stat,
						MechanicTypes.Ultimate => CappedStat(),
						MechanicTypes.MountStamina => 0, // Mount Stamina
						MechanicTypes.Health =>
							a * Health + c,
						MechanicTypes.Daedric => 0, // Daedric
						_ => throw new InvalidOperationException("A bit was specified that has no current value."),
					};

					resultList.Add(ToText(result));
				}

				break;
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
		var a = ToPrecision(this.A, 5);
		var b = ToPrecision(this.B, 5);
		var c = ToPrecision(this.C, 5);
		//// var r = ToPrecision(this.R, 5);

		var sb = new StringBuilder();
		switch (this.Mechanic)
		{
			case -2: // Health
				sb.Append((int)Math.Round(a * Health + c));
				break;
			case -56: // Spell + Weapon Damage
			case -50: // Ultimate (no weapon damage)
			case -68: // Magicka with Health Cap
				value = (int)Math.Round(a * Stat);
				maxValue = (int)Math.Round(b * Health);
				sb.Append(value > maxValue ? maxValue : value);
				break;
			case -71: // Spell Damage Capped
				value = (int)Math.Round(a * Damage + b);
				maxValue = (int)c;
				sb.Append(value > maxValue ? maxValue : value);
				break;
			case -72: // Magicka and Weapon Damage
				sb.Append((int)Math.Round(a * Stat + b * Damage + c));
				break;
			case -73: // Magicka and Spell Damage
				var halfMax = c / 2;
				var statDamage = a * Stat;
				if (statDamage > halfMax)
				{
					statDamage = halfMax;
				}

				var dmgDamage = b * Damage;
				if (dmgDamage > halfMax)
				{
					dmgDamage = halfMax;
				}

				value = (int)Math.Round(statDamage + dmgDamage);
				maxValue = (int)c;
				sb.Append(value > maxValue ? maxValue : value);
				break;
			case -74: // Weapon Power
			case -75: // Constant Value
			case -76: // Health or Spell Damage
			case -78: // Magicka and Light Armor(Health Capped)
			case -79: // Health or Weapon/Spell Damage
				throw new InvalidOperationException("Formula unknown");
			case -77: // Max Resistance
				sb.Append((int)Math.Round(a * Resist + c));
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
				sb
					.Append('(')
					.Append(((double)this.A).RoundSignificant(3))
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