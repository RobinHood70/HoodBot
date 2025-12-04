namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
	#region Static Fields
	private static readonly Dictionary<int, string> DamageTypes = new()
	{
		[0] = "<None>",
		[1] = string.Empty,
		[2] = "Physical",
		[3] = "Flame",
		[4] = "Shock",
		[5] = "Oblivion",
		[6] = "Frost",
		[7] = "Earth",
		[8] = "Magic",
		[9] = "Drown",
		[10] = "Disease",
		[11] = "Poison",
		[12] = "Bleed",
		[515] = "Flame",
	};

	private static readonly Dictionary<(int, sbyte), (string From, string To)> ValueReplacements = new()
	{
		[(23234, 1)] = ("1 second", "1 seconds"),
		[(24574, 1)] = ("1 minute", "1 minutes"),
		[(32166, 2)] = ("1 minute", "60 seconds"),
		[(33195, 3)] = ("1 second", "1 seconds"),
		[(37631, 2)] = ("1 minute", "60 seconds"),
		[(41567, 2)] = ("1 minute", "60 seconds"),
		[(93914, 3)] = ("1 minute", "60 seconds"),
	};
	#endregion

	#region Fields
	private int? newTicks;
	#endregion

	#region Constructors
	public Coefficient(IDataRecord row)
	{
		this.A = (float)row["a"];
		this.AbilityId = (int)row["abilityId"];
		this.B = (float)row["b"];
		this.C = (float)row["c"];
		this.CoefficientType = (sbyte)row["coefType"];
		this.Cooldown = (int)row["cooldown"];
		this.DamageType = (int)row["dmgType"];
		this.Duration = (int)row["duration"];
		this.HasRankMod = (bool)row["hasRankMod"];
		this.Index = (sbyte)row["idx"];
		this.IsADE = (bool)row["isAOE"];
		this.IsDamage = (bool)row["isDmg"];
		this.IsDamageShield = (bool)row["isDmgShield"];
		this.IsElfBane = (bool)row["isElfBane"];
		this.IsFlameAOE = (bool)row["isFlameAOE"];
		this.IsHeal = (bool)row["isHeal"];
		this.IsMelee = (bool)row["isMelee"];
		this.IsPlayer = (bool)row["isPlayer"];
		this.R = (float)row["r"];
		this.RawType = (sbyte)row["rawType"];
		this.RawValue1 = (int)row["rawValue1"];
		this.RawValue2 = (int)row["rawValue2"];
		this.StartTime = (int)row["startTime"];
		this.TickTime = (int)row["tickTime"];
		this.UsesManualCoefficient = (bool)row["usesManualCoef"];

		var value = EsoLog.ConvertEncoding((string)row["value"]);
		var key = (this.AbilityId, this.Index);
		if (ValueReplacements.TryGetValue(key, out var replacement))
		{
			if (replacement.From.OrdinalEquals(value))
			{
				value = replacement.To;
			}
			else
			{
				Debug.WriteLine("Replacement " + key + " is out of date.");
			}
		}

		this.Value = value;
	}
	#endregion

	#region Public Static Properties
	public static Regex RawCoefficient { get; } = new(@"<<(?<index>\d+)>>", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Public Properties
	public float A { get; }

	public int AbilityId { get; }

	public float B { get; }

	public float C { get; }

	public sbyte CoefficientType { get; }

	public int Cooldown { get; }

	public string? DamageSuffix =>
		this.IsDamage &&
		DamageTypes[this.DamageType] is var damageType &&
		damageType.Length > 0
			? ' ' + damageType + " Damage"
			: string.Empty;

	public int DamageType { get; }

	public int Duration { get; }

	public bool HasRankMod { get; }

	public sbyte Index { get; }

	public bool IsADE { get; }

	public bool IsDamage { get; }

	public bool IsDamageShield { get; }

	public bool IsElfBane { get; }

	public bool IsFlameAOE { get; }

	public bool IsHeal { get; }

	public bool IsMelee { get; }

	public bool IsPlayer { get; }

	public float R { get; }

	public sbyte RawType { get; }

	public int RawValue1 { get; }

	public int RawValue2 { get; }

	public int StartTime { get; }

	public int TickTime { get; }

	public bool UsesManualCoefficient { get; }

	public string Value { get; }
	#endregion

	#region Public Methods
	public bool IsValid() => this.A != -1.0 || this.B != -1.0 || this.C != -1.0;

	public string SkillDamageText(double rankFactor)
	{
		var inputValues = new InputValues();
		double value;
		switch (this.CoefficientType)
		{
			case -2: // Health
			case 32: // Update 34
				value = Math.Floor(this.A * inputValues.Health) + this.C;
				break;
			case 0: // Magicka
			case 1: // Update 34
				value = Math.Floor(this.A * inputValues.Magicka) + Math.Floor(this.B * inputValues.SpellDamage) + this.C;
				break;
			case 6: // Stamina
			case 4: // Update 34
				value = Math.Floor(this.A * inputValues.Stamina) + Math.Floor(this.B * inputValues.WeaponDamage) + this.C;
				break;
			case 10: // Ultimate
			case 8: // Update 34
				value =
					Math.Floor(this.A * Math.Max(inputValues.Magicka, inputValues.Stamina)) +
					Math.Floor(this.B * Math.Max(inputValues.SpellDamage, inputValues.WeaponDamage)) +
					this.C;
				break;
			case -50: // Ultimate Soul Tether
				value =
					Math.Floor(this.A * Math.Max(inputValues.Magicka, inputValues.Stamina)) +
					Math.Floor(this.B * inputValues.SpellDamage) +
					this.C;
				break;
			case -51: // Light Armor
				if (inputValues.LightArmor is int lightArmor)
				{
					value = this.A * lightArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:g5} * LightArmor)"
					: $"({this.A:g5} * LightArmor + {this.C:g5})";
			case -52: // Medium Armor
				if (inputValues.MediumArmor is int mediumArmor)
				{
					value = this.A * mediumArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:g5} * MediumArmor)"
					: $"({this.A:g5} * MediumArmor + {this.C:g5})";
			case -53: // Heavy Armor
				if (inputValues.HeavyArmor is int heavyArmor)
				{
					value = this.A * heavyArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:g5} * HeavyArmor)"
					: $"({this.A:g5} * HeavyArmor + {this.C:g5})";
			case -54: // Daggers
				if (inputValues.DaggerWeapon is int daggerWeapon)
				{
					value = this.A * daggerWeapon;
					break;
				}

				return $"({this.A:g5} * Dagger)";
			case -55: // Armor Types
				if (inputValues.ArmorTypes is int armorTypes)
				{
					value = this.A * armorTypes;
					break;
				}

				return $"({this.A:g5} * ArmorTypes)";
			case -56: // Spell + Weapon Damage
				value = Math.Floor(this.A * inputValues.SpellDamage) + Math.Floor(this.B * inputValues.WeaponDamage) + this.C;
				break;
			case -57:
				value = this.A * inputValues.AssassinSkills;
				break;
			case -58:
				value = this.A * inputValues.FightersGuildSkills;
				break;
			case -59:
				value = this.A * inputValues.DraconicPowerSkills;
				break;
			case -60:
				value = this.A * inputValues.ShadowSkills;
				break;
			case -61:
				value = this.A * inputValues.SiphoningSkills;
				break;
			case -62:
				value = this.A * inputValues.SorcererSkills;
				break;
			case -63:
				value = this.A * inputValues.MagesGuildSkills;
				break;
			case -64:
				value = this.A * inputValues.SupportSkills;
				break;
			case -65:
				value = this.A * inputValues.AnimalCompanionSkills;
				break;
			case -66:
				value = this.A * inputValues.GreenBalanceSkills;
				break;
			case -67:
				value = this.A * inputValues.WintersEmbraceSkills;
				break;
			case -68: // Magicka with Capped Health
				value = Math.Min(Math.Floor(this.A * inputValues.Magicka), Math.Floor(this.B * inputValues.Health));
				break;
			case -69:
				value = this.A * inputValues.BoneTyrantSkills;
				break;
			case -70:
				value = this.A * inputValues.GraveLordSkills;
				break;
			case -71: // Capped Spell Damage
				value = Math.Min(Math.Floor(this.A * inputValues.SpellDamage) + this.B, this.C);
				break;
			case -72: // Magicka and Weapon Damage
				value = Math.Floor(this.A * inputValues.Magicka) + Math.Floor(this.B * inputValues.WeaponDamage) + this.C;
				break;
			case -73: // Capped Magicka and Spell Damage
				var halfMax = this.C / 2;
				value = Math.Min(
					Math.Min(Math.Floor(this.A * inputValues.Magicka), halfMax) +
					Math.Min(Math.Floor(this.B * inputValues.SpellDamage), halfMax),
					this.C);
				break;
			case -74: // Weapon Power
				value = Math.Floor(this.A * inputValues.WeaponPower) + this.C;
				break;
			case -75: // Constant (Dave handles this at the top, but for my purposes, this seems sufficient)
				return this.Value;
			case -76: // Health or Spell Damage
				value = Math.Max(
					Math.Floor(this.A * inputValues.SpellDamage),
					Math.Floor(this.B * inputValues.Health)) +
					this.C;
				break;
			case -79: // Health or Spell/Weapon Damage
				value = Math.Max(
					Math.Floor(this.A * inputValues.SpellDamage) + Math.Floor(this.B * inputValues.WeaponDamage),
					Math.Floor(this.C * inputValues.Health));
				break;
			case -77: // Max Resistance
				value = Math.Floor(this.A * Math.Max(inputValues.SpellResist, inputValues.PhysicalResist)) + this.C;
				break;
			case -78: // Magicka and Light Armor (Health Capped)
				if (inputValues.LightArmor is null)
				{
					value = Math.Min(Math.Floor(this.A * inputValues.Magicka), this.C * inputValues.Health);
				}
				else
				{
					value = Math.Min(Math.Floor(this.A * inputValues.Magicka) * (1.0 + this.B * inputValues.LightArmor.Value), this.C * inputValues.Health);  // TODO: Check rounding order
				}

				break;
			case -80: // Herald of the Tome skills slotted
				value = this.A * inputValues.HeraldoftheTomeSkills;
				break;
			case -81: // Soldier of Apocrypha skills slotted
				value = this.A * inputValues.SoldierofApocryphaSkills;
				break;
			case -82: // Health or Magicka with Health Cap
				value = Math.Max(
					Math.Floor(this.A * inputValues.Magicka),
					Math.Floor(this.B * inputValues.Health));
				var maxValue = Math.Floor(this.C * inputValues.Health);
				if (maxValue > 0)
				{
					value = Math.Max(value, maxValue);
				}

				break;
			default:
				throw new InvalidOperationException($"Unrecognized coefficient type {this.CoefficientType}");
		}

		if (this.HasRankMod)
		{
			value = Math.Floor(value * rankFactor);
		}

		value = this.RawType == 92
			? Math.Floor(value * 10) / 10
			: Math.Floor(value);

		if (value < 0)
		{
			value = 0;
		}

		if ((this.RawType == 49 || this.RawType == 53) && this.Duration > 0)
		{
			var duration = +this.Duration;

			double dotFactor;
			if (this.newTicks is not null)
			{
				dotFactor = this.newTicks.Value;
				this.newTicks = null;
			}
			else
			{
				dotFactor = this.TickTime > 0
					? (duration + +this.TickTime) / +this.TickTime
					: duration / 1000;
			}

			if (dotFactor != 1)
			{
				value = Math.Floor(Math.Floor(value) * dotFactor);
			}
		}

		return $"{value:g5}";
	}
	#endregion

	#region Private Classes
	private sealed class InputValues
	{
		#region Fields
		private const int DefaultArmor = 10000;
		private const int DefaultDamage = 3000;
		private const int DefaultHealth = 16000;
		private const int DefaultLevel = 66;
		private const int DefaultMagStam = 30000;
		#endregion

		#region Public Properties
		public int AnimalCompanionSkills { get; } = 0;

		public int? ArmorTypes { get; } = 0;

		public int AssassinSkills { get; } = 0;

		public int BoneTyrantSkills { get; } = 0;

		public object[] ChannelDamageDone { get; } = [];

		public int? DaggerWeapon { get; } = 0;

		public object[] Damage { get; } = [];

		public int DamageShield { get; } = 0;

		public object[] DotDamageDone { get; } = [];

		public int DraconicPowerSkills { get; } = 0;

		public int EffectiveLevel { get; } = DefaultLevel;

		public int FightersGuildSkills { get; } = 0;

		public int GraveLordSkills { get; } = 0;

		public int GreenBalanceSkills { get; } = 0;

		public object[] Healing { get; } = [new object[] { "Done", 0 }];

		public int Health { get; } = DefaultHealth;

		public int? HeavyArmor { get; } = DefaultArmor;

		public int HeraldoftheTomeSkills { get; } = 0;

		public int? LightArmor { get; } = DefaultArmor;

		public int MagesGuildSkills { get; } = 0;

		public int Magicka { get; } = DefaultMagStam;

		public int MaxDamage => Math.Max(this.SpellDamage, this.WeaponDamage);

		public int MaxStat => Math.Max(this.Magicka, this.Stamina);

		public int? MediumArmor { get; } = DefaultArmor;

		public int PhysicalResist { get; } = DefaultArmor;

		public int ShadowSkills { get; } = 0;

		public int SiphoningSkills { get; } = 0;

		public object[] SkillDamage { get; } = [];

		public object[] SkillHealing { get; } = [];

		public object[] SkillSpellDamage { get; } = [];

		public int SoldierofApocryphaSkills { get; } = 0;

		public int SorcererSkills { get; } = 0;

		public int SpellDamage { get; } = DefaultDamage;

		public int SpellResist { get; } = DefaultArmor;

		public int Stamina { get; } = DefaultMagStam;

		public int SupportSkills { get; } = 0;

		public int WeaponDamage { get; } = DefaultDamage;

		public int WeaponPower { get; } = 0;

		public int WintersEmbraceSkills { get; } = 0;
		#endregion
	}
	#endregion
}