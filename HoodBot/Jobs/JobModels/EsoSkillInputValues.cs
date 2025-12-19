namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;

public sealed class EsoSkillInputValues(int defaultArmor, int defaultDamage, int defaultHealth, int defaultLevel, int defaultMagStam)
{
	#region Static Properties
	public static EsoSkillInputValues SkillBrowserDefault { get; } = new EsoSkillInputValues(11000, 2000, 20000, 66, 20000);

	public static EsoSkillInputValues WikiDefault { get; } = new EsoSkillInputValues(10000, 3000, 16000, 66, 30000);
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

	public int EffectiveLevel { get; } = defaultLevel;

	public int FightersGuildSkills { get; } = 0;

	public int GraveLordSkills { get; } = 0;

	public int GreenBalanceSkills { get; } = 0;

	public object[] Healing { get; } = [new object[] { "Done", 0 }];

	public int Health { get; } = defaultHealth;

	public int? HeavyArmor { get; } = defaultArmor;

	public int HeraldoftheTomeSkills { get; } = 0;

	public int? LightArmor { get; } = defaultArmor;

	public int MagesGuildSkills { get; } = 0;

	public int Magicka { get; } = defaultMagStam;

	public int MaxDamage => Math.Max(this.SpellDamage, this.WeaponDamage);

	public int MaxStat => Math.Max(this.Magicka, this.Stamina);

	public int? MediumArmor { get; } = defaultArmor;

	public int PhysicalResist { get; } = defaultArmor;

	public int ShadowSkills { get; } = 0;

	public int SiphoningSkills { get; } = 0;

	public object[] SkillDamage { get; } = [];

	public object[] SkillHealing { get; } = [];

	public object[] SkillSpellDamage { get; } = [];

	public int SoldierofApocryphaSkills { get; } = 0;

	public int SorcererSkills { get; } = 0;

	public int SpellDamage { get; } = defaultDamage;

	public int SpellResist { get; } = defaultArmor;

	public int Stamina { get; } = defaultMagStam;

	public int SupportSkills { get; } = 0;

	public int WeaponDamage { get; } = defaultDamage;

	public int WeaponPower { get; } = 0;

	public int WintersEmbraceSkills { get; } = 0;
	#endregion
}