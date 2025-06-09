namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
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

internal sealed class Coefficient(IDataRecord row)
{
	#region Static Fields
	private static readonly Dictionary<short, string> DamageTypes = new()
	{
		[0] = "None",
		[1] = "Generic",
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
	#endregion

	#region Fields
	private int? newTicks;
	#endregion

	#region Public Static Properties
	public static Regex RawCoefficient { get; } = new(@"<<(?<index>\d+)>>", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Public Properties
	public float A { get; } = (float)row["a"];

	public float B { get; } = (float)row["b"];

	public float C { get; } = (float)row["c"];

	public sbyte CoefficientType { get; } = (sbyte)row["coefType"];

	public int Cooldown { get; } = (int)row["cooldown"];

	public short DamageType { get; } = (short)row["damageType"];

	public int Duration { get; } = (int)row["duration"];

	public bool HasRankMod { get; } = (bool)row["hasRankMod"];

	public byte Index { get; } = (byte)row["idx"];

	public bool IsADE { get; } = (bool)row["isAOE"];

	public bool IsDDT { get; } = (bool)row["isDDT"];

	public bool IsDamage { get; } = (bool)row["isDmg"];

	public bool IsDmgShield { get; } = (bool)row["isDmgShield"];

	public bool IsElfBane { get; } = (bool)row["isElfBane"];

	public bool IsFlameAOE { get; } = (bool)row["isFlameAOE"];

	public bool IsHeal { get; } = (bool)row["isHeal"];

	public bool IsMelee { get; } = (bool)row["isMelee"];

	public bool IsPlayer { get; } = (bool)row["isPlayer"];

	public byte RawType { get; } = (byte)row["rawType"];

	public int RawValue1 { get; } = (int)row["rawValue1"];

	public int RawValue2 { get; } = (int)row["rawValue2"];

	public int StartTime { get; } = (int)row["startTime"];

	public int TickTime { get; } = (int)row["tickTime"];

	public bool UsesManualCoefficient { get; } = (bool)row["usesManualCoefficient"];

	public string Value { get; } = EsoLog.ConvertEncoding((string)row["value"]);

	public float R { get; } = (float)row["r"];
	#endregion

	#region Public Methods
	public bool IsValid() => this.A != -1 || this.B != -1 || this.C != -1;

	public string SkillDamageText(double rankFactor)
	{
		var inputValues = new InputValues();
		if (this.CoefficientType == -75)
		{
			throw new NotSupportedException();
		}

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
					? $"({this.A:n5} * LightArmor)"
					: $"({this.A:n5} * LightArmor + {this.C:n5})";
			case -52: // Medium Armor
				if (inputValues.MediumArmor is int mediumArmor)
				{
					value = this.A * mediumArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:n5} * MediumArmor)"
					: $"({this.A:n5} * MediumArmor + {this.C:n5})";
			case -53: // Heavy Armor
				if (inputValues.HeavyArmor is int heavyArmor)
				{
					value = this.A * heavyArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:n5} * HeavyArmor)"
					: $"({this.A:n5} * HeavyArmor + {this.C:n5})";
			case -54: // Daggers
				if (inputValues.DaggerWeapon is int daggerWeapon)
				{
					value = this.A * daggerWeapon;
					break;
				}

				return $"({this.A:n5} * Dagger)";
			case -55: // Armor Types
				if (inputValues.ArmorTypes is int armorTypes)
				{
					value = this.A * armorTypes;
					break;
				}

				return $"({this.A:n5} * ArmorTypes)";
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
			case -75: // Constant (should be handled before this point)
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

		return $"{value:n5}";
	}
	#endregion

	#region Private Classes
	private sealed class InputValues
	{
		#region Fields
		private const int DefaultArmor = 11000;
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