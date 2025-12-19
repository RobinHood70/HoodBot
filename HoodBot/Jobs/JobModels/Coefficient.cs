namespace RobinHood70.HoodBot.Jobs.JobModels;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

public enum CoefficientTypes : sbyte
{
	HealthOld = -2,
	Invalid = -1,
	MagickaOld = 0,
	Magicka = 1,
	Werewolf = 2,
	Stamina = 4,
	StaminaOld = 6,
	Ultimate = 8,
	UltimateOld = 10,
	MountStamina = 16,
	Health = 32,
	Daedric = 64,
	SoulTether = -50,
	LightArmor = -51,
	MediumArmor = -52,
	HeavyArmor = -53,
	WeaponDagger = -54,
	ArmorType = -55,
	Damage = -56,
	Assassination = -57,
	FightersGuild = -58,
	DraconicPower = -59,
	Shadow = -60,
	Siphoning = -61,
	Sorcerer = -62,
	MagesGuild = -63,
	Support = -64,
	AnimalCompanion = -65,
	GreenBalance = -66,
	WintersEmbrace = -67,
	MagicHealthCapped = -68,
	BoneTyrant = -69,
	GraveLord = -70,
	SpellDamageCapped = -71,
	MagickaWeaponDamage = -72,
	MagickaSpellDamageCapped = -73,
	WeaponPower = -74,
	ConstantValue = -75,
	HealthOrSpellDamage = -76,
	Resistance = -77,
	MagickaLightArmor = -78,
	HealthOrDamage = -79,
	HeraldOfTheTome = -80,
	SoldierOfApocrypha = -81,
	HealthOrMagickaCapped = -82,
}

public enum RawTypes : sbyte
{
	None = 0,
	Name = 3,
	AbilityName = 8,
	Resource16 = 16,
	Damage17 = 17,
	Damage18 = 18,
	EveryTickTime = 19, // Substitute with index+2
	OverTimeDuration = 20,
	DamageReduction = 21,
	Armor = 22,
	Duration31 = 31,
	Cooldown = 44,
	Duration48 = 48,
	DamageOverTime = 49,
	DamageShort51 = 51,
	DamageShort52 = 52,
	HealOverTime = 53,
	Resource54 = 54,
	StatPercent = 55, // Tooltip Healing
	Duration56 = 56,
	Frequency2 = 57,
	AbsoluteValue = 58,
	ArmorPercent = 73,
	Duration79 = 79,
	Derived = 90,
	Distance = 91,
	DerivedOneDec = 92,
	TotalDuration = 93,
	DerivedPercent96 = 96,
	Derived10Percent = 97,
	Derived25Percent = 104,
	DerivedPercent105 = 105,
	Unknown121 = 121,
	Unknown123 = 123,
}

internal sealed class Coefficient(float a, int abilityId, float b, float c, CoefficientTypes coefficientType, int cooldown, int damageType, int duration, bool hasRankMod, sbyte index, bool isADE, bool isDamage, bool isDamageShield, bool isElfBane, bool isFlameAOE, bool isHeal, bool isMelee, bool isPlayer, float r, RawTypes rawType, int rawValue1, int rawValue2, int startTime, int tickTime, bool usesManualCoefficient, string value)
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

	#region Public Static Properties
	public static Regex RawCoefficient { get; } = new(@"<<(?<index>\d+)>>", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Public Properties
	public float A { get; } = a;

	public int AbilityId { get; } = abilityId;

	public float B { get; } = b;

	public float C { get; } = c;

	public CoefficientTypes CoefficientType { get; } = coefficientType;

	public int Cooldown { get; } = cooldown;

	public string? DamageSuffix =>
		this.IsDamage &&
		DamageTypes[this.DamageType] is var damageType &&
		damageType.Length > 0
			? ' ' + damageType + " Damage"
			: string.Empty;

	public int DamageType { get; } = damageType;

	public int Duration { get; } = duration;

	public double Factor { get; set; } = 1.0; // Settable because it cannot be determined until morph data is known.

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

	public int NewTicks { get; set; } = -1;

	public float R { get; } = r;

	public RawTypes RawType { get; } = rawType;

	public int RawValue1 { get; } = rawValue1;

	public int RawValue2 { get; } = rawValue2;

	public int StartTime { get; } = startTime;

	public int TickTime { get; } = tickTime;

	public bool UsesManualCoefficient { get; } = usesManualCoefficient;

	public string Value { get; } = value;
	#endregion

	#region Public Methods
	public bool IsValid() => this.A != -1.0 || this.B != -1.0 || this.C != -1.0;
	#endregion
}