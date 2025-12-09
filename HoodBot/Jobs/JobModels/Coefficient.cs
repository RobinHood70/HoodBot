namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

internal sealed class Coefficient(float a, int abilityId, float b, float c, sbyte coefficientType, int cooldown, int damageType, int duration, bool hasRankMod, sbyte index, bool isADE, bool isDamage, bool isDamageShield, bool isElfBane, bool isFlameAOE, bool isHeal, bool isMelee, bool isPlayer, float r, sbyte rawType, int rawValue1, int rawValue2, int startTime, int tickTime, bool usesManualCoefficient, string value)
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
	#endregion

	#region Fields
	private int? newTicks;
	#endregion

	#region Public Static Properties
	public static Regex RawCoefficient { get; } = new(@"<<(?<index>\d+)>>", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Public Properties
	public float A { get; } = a;

	public int AbilityId { get; } = abilityId;

	public float B { get; } = b;

	public float C { get; } = c;

	public sbyte CoefficientType { get; } = coefficientType;

	public int Cooldown { get; } = cooldown;

	public string? DamageSuffix =>
		this.IsDamage &&
		DamageTypes[this.DamageType] is var damageType &&
		damageType.Length > 0
			? ' ' + damageType + " Damage"
			: string.Empty;

	public int DamageType { get; } = damageType;

	public int Duration { get; } = duration;

	public bool HasRankMod { get; } = hasRankMod;

	public sbyte Index { get; } = index;

	public bool IsADE { get; } = isADE;

	public bool IsDamage { get; } = isDamage;

	public bool IsDamageShield { get; } = isDamageShield;

	public bool IsElfBane { get; } = isElfBane;

	public bool IsFlameAOE { get; } = isFlameAOE;

	public bool IsHeal { get; } = isHeal;

	public bool IsMelee { get; } = isMelee;

	public bool IsPlayer { get; } = isPlayer;

	public float R { get; } = r;

	public sbyte RawType { get; } = rawType;

	public int RawValue1 { get; } = rawValue1;

	public int RawValue2 { get; } = rawValue2;

	public int StartTime { get; } = startTime;

	public int TickTime { get; } = tickTime;

	public bool UsesManualCoefficient { get; } = usesManualCoefficient;

	public string Value { get; } = value;
	#endregion

	#region Public Methods
	public bool IsValid() => this.A != -1.0 || this.B != -1.0 || this.C != -1.0;

	public string SkillDamageText(double rankFactor)
	{
		// Original code: EsoLog => esoSkillToolTips.js => window.ComputeEsoSkillTooltipCoefDescription2
		var inputValues = new InputValues();
		double retval;
		switch (this.CoefficientType)
		{
			case -2: // Health
			case 32: // Update 34
				retval = Math.Floor(this.A * inputValues.Health) + this.C;
				break;
			case 0: // Magicka
			case 1: // Update 34
				retval = Math.Floor(this.A * inputValues.Magicka) + Math.Floor(this.B * inputValues.SpellDamage) + this.C;
				break;
			case 6: // Stamina
			case 4: // Update 34
				retval = Math.Floor(this.A * inputValues.Stamina) + Math.Floor(this.B * inputValues.WeaponDamage) + this.C;
				break;
			case 10: // Ultimate
			case 8: // Update 34
				retval =
					Math.Floor(this.A * Math.Max(inputValues.Magicka, inputValues.Stamina)) +
					Math.Floor(this.B * Math.Max(inputValues.SpellDamage, inputValues.WeaponDamage)) +
					this.C;
				break;
			case -50: // Ultimate Soul Tether
				retval =
					Math.Floor(this.A * Math.Max(inputValues.Magicka, inputValues.Stamina)) +
					Math.Floor(this.B * inputValues.SpellDamage) +
					this.C;
				break;
			case -51: // Light Armor
				if (inputValues.LightArmor is int lightArmor)
				{
					retval = this.A * lightArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:g5} * LightArmor)"
					: $"({this.A:g5} * LightArmor + {this.C:g5})";
			case -52: // Medium Armor
				if (inputValues.MediumArmor is int mediumArmor)
				{
					retval = this.A * mediumArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:g5} * MediumArmor)"
					: $"({this.A:g5} * MediumArmor + {this.C:g5})";
			case -53: // Heavy Armor
				if (inputValues.HeavyArmor is int heavyArmor)
				{
					retval = this.A * heavyArmor + this.C;
					break;
				}

				return this.C == 0
					? $"({this.A:g5} * HeavyArmor)"
					: $"({this.A:g5} * HeavyArmor + {this.C:g5})";
			case -54: // Daggers
				if (inputValues.DaggerWeapon is int daggerWeapon)
				{
					retval = this.A * daggerWeapon;
					break;
				}

				return $"({this.A:g5} * Dagger)";
			case -55: // Armor Types
				if (inputValues.ArmorTypes is int armorTypes)
				{
					retval = this.A * armorTypes;
					break;
				}

				return $"({this.A:g5} * ArmorTypes)";
			case -56: // Spell + Weapon Damage
				retval = Math.Floor(this.A * inputValues.SpellDamage) + Math.Floor(this.B * inputValues.WeaponDamage) + this.C;
				break;
			case -57:
				retval = this.A * inputValues.AssassinSkills;
				break;
			case -58:
				retval = this.A * inputValues.FightersGuildSkills;
				break;
			case -59:
				retval = this.A * inputValues.DraconicPowerSkills;
				break;
			case -60:
				retval = this.A * inputValues.ShadowSkills;
				break;
			case -61:
				retval = this.A * inputValues.SiphoningSkills;
				break;
			case -62:
				retval = this.A * inputValues.SorcererSkills;
				break;
			case -63:
				retval = this.A * inputValues.MagesGuildSkills;
				break;
			case -64:
				retval = this.A * inputValues.SupportSkills;
				break;
			case -65:
				retval = this.A * inputValues.AnimalCompanionSkills;
				break;
			case -66:
				retval = this.A * inputValues.GreenBalanceSkills;
				break;
			case -67:
				retval = this.A * inputValues.WintersEmbraceSkills;
				break;
			case -68: // Magicka with Capped Health
				retval = Math.Min(Math.Floor(this.A * inputValues.Magicka), Math.Floor(this.B * inputValues.Health));
				break;
			case -69:
				retval = this.A * inputValues.BoneTyrantSkills;
				break;
			case -70:
				retval = this.A * inputValues.GraveLordSkills;
				break;
			case -71: // Capped Spell Damage
				retval = Math.Min(Math.Floor(this.A * inputValues.SpellDamage) + this.B, this.C);
				break;
			case -72: // Magicka and Weapon Damage
				retval = Math.Floor(this.A * inputValues.Magicka) + Math.Floor(this.B * inputValues.WeaponDamage) + this.C;
				break;
			case -73: // Capped Magicka and Spell Damage
				var halfMax = this.C / 2;
				retval = Math.Min(
					Math.Min(Math.Floor(this.A * inputValues.Magicka), halfMax) +
					Math.Min(Math.Floor(this.B * inputValues.SpellDamage), halfMax),
					this.C);
				break;
			case -74: // Weapon Power
				retval = Math.Floor(this.A * inputValues.WeaponPower) + this.C;
				break;
			case -75: // Constant (Dave handles this at the top, but for my purposes, this seems sufficient)
				return this.Value;
			case -76: // Health or Spell Damage
				retval = Math.Max(
					Math.Floor(this.A * inputValues.SpellDamage),
					Math.Floor(this.B * inputValues.Health)) +
					this.C;
				break;
			case -79: // Health or Spell/Weapon Damage
				retval = Math.Max(
					Math.Floor(this.A * inputValues.SpellDamage) + Math.Floor(this.B * inputValues.WeaponDamage),
					Math.Floor(this.C * inputValues.Health));
				break;
			case -77: // Max Resistance
				retval = Math.Floor(this.A * Math.Max(inputValues.SpellResist, inputValues.PhysicalResist)) + this.C;
				break;
			case -78: // Magicka and Light Armor (Health Capped)
				if (inputValues.LightArmor is null)
				{
					retval = Math.Min(Math.Floor(this.A * inputValues.Magicka), this.C * inputValues.Health);
				}
				else
				{
					retval = Math.Min(Math.Floor(this.A * inputValues.Magicka) * (1.0 + this.B * inputValues.LightArmor.Value), this.C * inputValues.Health);  // TODO: Check rounding order
				}

				break;
			case -80: // Herald of the Tome skills slotted
				retval = this.A * inputValues.HeraldoftheTomeSkills;
				break;
			case -81: // Soldier of Apocrypha skills slotted
				retval = this.A * inputValues.SoldierofApocryphaSkills;
				break;
			case -82: // Health or Magicka with Health Cap
				retval = Math.Max(
					Math.Floor(this.A * inputValues.Magicka),
					Math.Floor(this.B * inputValues.Health));
				var maxValue = Math.Floor(this.C * inputValues.Health);
				if (maxValue > 0)
				{
					retval = Math.Max(retval, maxValue);
				}

				break;
			default:
				throw new InvalidOperationException($"Unrecognized coefficient type {this.CoefficientType}");
		}

		if (this.HasRankMod)
		{
			retval = Math.Floor(retval * rankFactor);
		}

		retval = this.RawType == 92
			? Math.Floor(retval * 10) / 10
			: Math.Floor(retval);

		if (retval < 0)
		{
			retval = 0;
		}

		if ((this.RawType == 49 || this.RawType == 53) && this.Duration > 0)
		{
			var dur = this.Duration;

			double dotFactor;
			if (this.newTicks is not null)
			{
				// TODO: Figure out what this was supposed to do. Right now, newTicks is never set. Looks like this code is incomplete.
				dotFactor = this.newTicks.Value;
				this.newTicks = null;
			}
			else
			{
				dotFactor = this.TickTime > 0
					? (dur + this.TickTime) / this.TickTime
					: dur / 1000;
			}

			if (dotFactor != 1)
			{
				retval = Math.Floor(Math.Floor(retval) * dotFactor);
			}
		}

		return $"{retval:g5}";
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