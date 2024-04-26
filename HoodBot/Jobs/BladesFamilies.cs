namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;

	internal sealed class BladesFamilies : WikiJob
	{
		#region Fields
		private readonly List<BladesEnemy> enemies = [];
		private readonly List<BladesFamily> families = [];
		#endregion

		#region Constructors
		[JobInfo("Blades Families", "Blades")]
		public BladesFamilies(JobManager jobManager)
			: base(jobManager, JobType.ReadOnly)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			foreach (var file in Directory.EnumerateFiles(LocalConfig.BotDataSubPath("Enemies")))
			{
				// var fi = new FileInfo(file);
				var text = File.ReadAllText(file);
				var token = JToken.Parse(text);
				if (token["_stats"] != null)
				{
					this.enemies.Add(new BladesEnemy(token));
				}
				else
				{
					this.families.Add(new BladesFamily(token));
				}
			}

			this.families.Sort((x, y) => string.CompareOrdinal(x.Sort, y.Sort));
			this.enemies.Sort((x, y) => string.CompareOrdinal(x.Sort, y.Sort));

			using var output = File.CreateText(LocalConfig.BotDataSubPath("Enemies.txt"));
			foreach (var family in this.families)
			{
				output.Write(family.Build());
			}

			output.WriteLine();
			foreach (var enemy in this.enemies)
			{
				output.Write(enemy.Build(this.Site.Culture));
			}
		}
		#endregion

		#region Private Classes
		private sealed class BladesEnemy
		{
			#region Static Fields
			private static readonly Dictionary<string, string> AbilityLookup = new(StringComparer.Ordinal)
			{
				["4e760726-b012-4b25-bc92-0cd6312d6601"] = "absorb",
				["be56c560-a4ba-47ad-8513-f24c342ca594"] = "adrenaline_dodge",
				["3f575c09-ca6d-40a8-977b-b2c5d44d1b5c"] = "armsman",
				["ed235f8d-0648-4aee-b955-a951562f549d"] = "augmented_flames",
				["270ae0e8-3758-4840-bdc2-e882008f0f0a"] = "augmented_frost",
				["66faf7c6-426c-4689-99e6-ccdd25b9bc11"] = "augmented_poison",
				["3c0510d4-84ef-40b6-b0f4-2b096ae89860"] = "augmented_shock",
				["64a6a981-0dc8-4fc1-b043-a75d052b00f5"] = "barbarian",
				["85596d85-5f2a-4f3a-9059-960eaff79a87"] = "blind",
				["c4b48518-e847-4f3d-81a2-2856bdb4ed98"] = "blizzard_armor",
				["b405918e-5988-49e6-91f2-35761add9a5e"] = "atronach_power",
				["e0b549c8-a686-49d4-a800-4661fff73e1d"] = "combat_focus",
				["e07f9b1a-64db-44ef-ba25-0e4378789ddc"] = "consuming_inferno",
				["dfb8d247-1333-42eb-9730-a1c16d10584f"] = "delayed_lightning_bolt",
				["1e7f0dd6-6015-4f65-b811-3246e407e330"] = "dodging_strike",
				["f60f69d4-24bc-46fb-a4fa-d4abdac0f06f"] = "echo_weapon",
				["788aa75e-4796-4d57-bbab-b1b901623f16"] = "elemental_protection",
				["780b82d1-a371-4454-baea-e18389f315e5"] = "enchantment_synergy",
				["50b19efb-c7d8-41fe-8791-4782cff99e70"] = "fire_breath",
				["d07a8d30-9a1c-49b0-866d-97a8aa1534cf"] = "fireball",
				["95eeca9c-7fbd-40a8-9d5b-9c061f08403c"] = "firestorm_armor",
				["9bc43c3d-4eb7-4d9c-b507-b3288f3b9ea1"] = "atronach_power",
				["e685e88f-34e7-4fdc-bacd-618763078d65"] = "focusing_dodge",
				["ef3e58a3-9bf0-49ca-8e1e-9dd34c61557d"] = "frost_breath",
				["4be1d681-c35d-4540-b255-c2910ac80664"] = "frostbite",
				["cc768bae-a063-4885-8207-f39c6542fb36"] = "guardbreaker",
				["69ffa3fd-deb7-4824-bab6-ac6450f19676"] = "harrying_bash",
				["09aa3390-8f42-4cd5-a88c-5c94d5e1dd29"] = "healing_surge",
				["cfee0b02-6d91-4d34-869c-a7e54329060d"] = "ice_spike",
				["66610227-07bf-4e3b-a75b-c591271f0817"] = "indomitable_smash",
				["7fc15804-1637-40a9-8dcc-3ea1eb0f778d"] = "lightning_bolt",
				["1c836287-44d8-40a6-bf02-d457f57d171d"] = "magicka_surge",
				["3dcb91c5-2279-4003-b6a6-53eac6fb86c8"] = "matching_set",
				["83784ade-533e-4965-a540-05bfd4f056d8"] = "maximum_power",
				["d6d7ad89-0c41-410f-8a19-c4850ab9fe4f"] = "mettle",
				["9fdc4d52-ce90-44f8-9b5d-21f31e27dbda"] = "paralyze",
				["cdab44fb-6ff6-4701-a4ec-d19cce79e49f"] = "piercing_strikes",
				["66bdc017-30c5-4b5e-9753-215c45056f6a"] = "poison_cloud",
				["ade28628-e213-4eaf-9eeb-5fce53870b92"] = "polar_slam",
				["ce6b63e9-9f18-49c4-aee0-51f7985f9892"] = "power_attack",
				["eb0cb7e6-47cf-48e7-8cc9-dbf80fc77f13"] = "quick_strikes",
				["0cfe29cd-89d9-42ad-9227-8308e2f87c7f"] = "reckless_fury",
				["e08f95de-85bb-4829-ba7e-cf45bc6fb422"] = "recovery_strikes",
				["ba61ce46-163f-4a61-8ede-f5b7ae365e40"] = "reflecting_bash",
				["7f78d342-f346-4210-9f62-01a540687bb3"] = "renewing_dodge",
				["91078132-ef5c-492a-97f2-ac69be5140a8"] = "resist_elements",
				["11ebd583-fc0c-44f0-8dbf-5c7207526064"] = "scout",
				["f9a2373b-a84f-4716-90ce-165baa2dd6ed"] = "shield_bash",
				["a9d330ac-3d9d-4aa0-92ea-65a552604acf"] = "shield_of_mania",
				["c1494bda-4219-4bec-8017-fb376a058ea7"] = "siphon_life",
				["c112c956-eaac-4d7d-878e-32cd7d1e5209"] = "skullcrusher",
				["521b9643-52e9-449a-b8ea-9221c0a73f20"] = "snakebite",
				["9b915ec3-c63b-4b62-b417-4c5436d45fc1"] = "staggering_bash",
				["3b610e1d-b4d0-48ae-bc6a-7b3fc6fe5f12"] = "tempest_armor",
				["a6bf8b2e-ee6e-4d61-a806-e46386451a42"] = "atronach_power",
				["2ab06506-2114-4738-bd87-f6f402d3ce2e"] = "thunderstorm",
				["e14eedd5-cd50-404e-9697-a37fd1d2ce0"] = "venom_strikes",
				["256b722a-c4bd-45ad-ae25-15ee805fbc70"] = "wall_of_fire",
				["65ede044-d68a-4b2b-8f0c-02075ad133cc"] = "ward",
			};
			#endregion

			#region Constructors
			public BladesEnemy(JToken token)
			{
				var stats = token["_stats"] ?? throw new InvalidOperationException();
				this.Name = (string?)token["m_Name"] ?? string.Empty;
				this.HealthBase = (double?)stats["_healthBase"] ?? 0;
				this.HealthIncrement = (double?)stats["_healthIncrement"] ?? 0;
				this.MagickaBase = (double?)stats["_magickaBase"] ?? 0;
				this.MagickaIncrement = (double?)stats["_magickaIncrement"] ?? 0;
				this.StaminaBase = (double?)stats["_staminaBase"] ?? 0;
				this.StaminaIncrement = (double?)stats["_staminaIncrement"] ?? 0;
				this.HealthRegenRate = Math.Round(((double?)stats["_healthRegenRate"] ?? 0) * 100, 5);
				this.HealthRegenRateOutsideCombat = (double?)stats["_healthRegenRateOutsideCombat"] ?? 0;
				this.StaminaRegenRate = Math.Round(((double?)stats["_staminaRegenRate"] ?? 0) * 100, 5);
				this.StaminaRegenRateOutsideCombat = (double?)stats["_staminaRegenRateOutsideCombat"] ?? 0;
				this.MagickaRegenRate = Math.Round(((double?)stats["_magickaRegenRate"] ?? 0) * 100, 5);
				this.MagickaRegenRateOutsideCombat = (double?)stats["_magickaRegenRateOutsideCombat"] ?? 0;
				this.InnateResistanceNone = (double?)stats["_innateResistanceNone"] ?? 0;
				this.InnateResistanceSlashing = (double?)stats["_innateResistanceSlashing"] ?? 0;
				this.InnateResistanceCleaving = (double?)stats["_innateResistanceCleaving"] ?? 0;
				this.InnateResistanceBashing = (double?)stats["_innateResistanceBashing"] ?? 0;
				this.InnateResistanceFire = (double?)stats["_innateResistanceFire"] ?? 0;
				this.InnateResistanceFrost = (double?)stats["_innateResistanceFrost"] ?? 0;
				this.InnateResistanceShock = (double?)stats["_innateResistanceShock"] ?? 0;
				this.InnateResistancePoison = (double?)stats["_innateResistancePoison"] ?? 0;
				this.InnateWeaknessNone = (double?)stats["_innateWeaknessNone"] ?? 0;
				this.InnateWeaknessSlashing = (double?)stats["_innateWeaknessSlashing"] ?? 0;
				this.InnateWeaknessCleaving = (double?)stats["_innateWeaknessCleaving"] ?? 0;
				this.InnateWeaknessBashing = (double?)stats["_innateWeaknessBashing"] ?? 0;
				this.InnateWeaknessFire = (double?)stats["_innateWeaknessFire"] ?? 0;
				this.InnateWeaknessFrost = (double?)stats["_innateWeaknessFrost"] ?? 0;
				this.InnateWeaknessShock = (double?)stats["_innateWeaknessShock"] ?? 0;
				this.InnateWeaknessPoison = (double?)stats["_innateWeaknessPoison"] ?? 0;
				this.InnateImmunityNone = (int?)stats["_innateImmunityNone"] == 1;
				this.InnateImmunitySlashing = (int?)stats["_innateImmunitySlashing"] == 1;
				this.InnateImmunityCleaving = (int?)stats["_innateImmunityCleaving"] == 1;
				this.InnateImmunityBashing = (int?)stats["_innateImmunityBashing"] == 1;
				this.InnateImmunityFire = (int?)stats["_innateImmunityFire"] == 1;
				this.InnateImmunityFrost = (int?)stats["_innateImmunityFrost"] == 1;
				this.InnateImmunityShock = (int?)stats["_innateImmunityShock"] == 1;
				this.InnateImmunityPoison = (int?)stats["_innateImmunityPoison"] == 1;
				this.InnateBlockRating = (double?)stats["_innateBlockRating"] ?? 0;
				this.InnateArmorRating = (double?)stats["_innateArmorRating"] ?? 0;
				this.AttackDamageBase = (double?)stats["_attackDamageBase"] ?? 0;
				this.AttackDamageIncrement = (double?)stats["_attackDamageIncrement"] ?? 0;
				this.ArmorRatingIncrement = (double?)stats["_armorRatingIncrement"] ?? 0;
				this.BlockRatingIncrement = (double?)stats["_blockRatingIncrement"] ?? 0;
				this.BackswingTime = (double?)stats["_backswingTime"] ?? 0;
				this.MaxDamageTime = (double?)stats["_maxDamageTime"] ?? 0;
				this.DamageDecayTime = (double?)stats["_damageDecayTime"] ?? 0;
				this.ComboDamageFactor = (double?)stats["_comboDamageFactor"] ?? 0;
				this.RecoveryTime = (double?)stats["_recoveryTime"] ?? 0;
				this.RecoveryToComboTime = (double?)stats["_recoveryToComboTime"] ?? 0;
				this.RecoveryToBlockTime = (double?)stats["_recoveryToBlockTime"] ?? 0;
				this.RecoveryToNeutralTime = (double?)stats["_recoveryToNeutralTime"] ?? 0;
				this.TimeToBlock = (double?)stats["_timeToBlock"] ?? 0;
				this.MinimumDamageFactor = (double?)stats["_minimumDamageFactor"] ?? 0;
				this.MaximumDamageFactor = (double?)stats["_maximumDamageFactor"] ?? 0;
				this.KillScoreMultiplier = (double?)stats["_killScoreMultiplier"] ?? 0;
				this.AttackingDamageReaction = (double?)stats["_attackingDamageReaction"] ?? 0;
				this.NotAttackingDamageReaction = (double?)stats["_notAttackingDamageReaction"] ?? 0;
				this.InnateImmunityStagger = (int?)stats["_innateImmunityStagger"] == 1;
				this.InnateImmunityParalyze = (int?)stats["_innateImmunityParalyze"] == 1;
				this.SpellCooldownFactor = (double?)token["_spellCooldownFactor"] ?? 0;
				this.ManeuverCooldownFactor = (double?)token["_maneuverCooldownFactor"] ?? 0;
				this.IsSpellCaster = (int?)token["_isSpellCaster"] == 1;
				this.EnemyName = (string?)token["_enemyName"]?["_key"];
				this.Sort = this.Name;

				if (stats["_baseDamageTypes"] is JToken damageTypes)
				{
					foreach (var damageType in damageTypes)
					{
						this.BaseDamageTypes.Add(new DamageTypeInfo(damageType));
					}
				}

				if (token["_knownAbilities"] is JToken abilities)
				{
					foreach (var ability in abilities)
					{
						if ((string?)ability["_abilityUid"]?["_uid"]?["_id"] is string guid)
						{
							var abilityName = AbilityLookup[guid];
							var rank = (int?)ability["_rank"] ?? 0;
							this.Abilities.Add(abilityName, rank);
						}
					}
				}
			}
			#endregion

			#region Public Properties
			public SortedDictionary<string, int> Abilities { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

			public double ArmorRatingIncrement { get; }

			public double AttackDamageBase { get; }

			public double AttackDamageIncrement { get; }

			public double AttackingDamageReaction { get; }

			public double BackswingTime { get; }

			public List<DamageTypeInfo> BaseDamageTypes { get; } = [];

			public double BlockRatingIncrement { get; }

			public double ComboDamageFactor { get; }

			public double DamageDecayTime { get; }

			public string? EnemyName { get; }

			public double HealthBase { get; internal set; }

			public double HealthIncrement { get; }

			public double HealthRegenRate { get; }

			public double HealthRegenRateOutsideCombat { get; }

			public double InnateArmorRating { get; }

			public double InnateBlockRating { get; }

			public bool InnateImmunityBashing { get; }

			public bool InnateImmunityCleaving { get; }

			public bool InnateImmunityFire { get; }

			public bool InnateImmunityFrost { get; }

			public bool InnateImmunityNone { get; }

			public bool InnateImmunityParalyze { get; }

			public bool InnateImmunityPoison { get; }

			public bool InnateImmunityShock { get; }

			public bool InnateImmunitySlashing { get; }

			public bool InnateImmunityStagger { get; }

			public double InnateResistanceBashing { get; }

			public double InnateResistanceCleaving { get; }

			public double InnateResistanceFire { get; }

			public double InnateResistanceFrost { get; }

			public double InnateResistanceNone { get; }

			public double InnateResistancePoison { get; }

			public double InnateResistanceShock { get; }

			public double InnateResistanceSlashing { get; }

			public double InnateWeaknessBashing { get; }

			public double InnateWeaknessCleaving { get; }

			public double InnateWeaknessFire { get; }

			public double InnateWeaknessFrost { get; }

			public double InnateWeaknessNone { get; }

			public double InnateWeaknessPoison { get; }

			public double InnateWeaknessShock { get; }

			public double InnateWeaknessSlashing { get; }

			public bool IsSpellCaster { get; }

			public double KillScoreMultiplier { get; }

			public double MagickaBase { get; }

			public double MagickaIncrement { get; }

			public double MagickaRegenRate { get; }

			public double MagickaRegenRateOutsideCombat { get; }

			public double ManeuverCooldownFactor { get; }

			public double MaxDamageTime { get; }

			public double MaximumDamageFactor { get; }

			public double MinimumDamageFactor { get; }

			public string Name { get; }

			public double NotAttackingDamageReaction { get; }

			public double RecoveryTime { get; }

			public double RecoveryToBlockTime { get; }

			public double RecoveryToComboTime { get; }

			public double RecoveryToNeutralTime { get; }

			public string Sort { get; }

			public double SpellCooldownFactor { get; }

			public double StaminaBase { get; }

			public double StaminaIncrement { get; }

			public double StaminaRegenRate { get; }

			public double StaminaRegenRateOutsideCombat { get; }

			public double TimeToBlock { get; }
			#endregion

			#region Public Methods
			public string Build(CultureInfo siteCulture)
			{
				StringBuilder sb = new();
				sb
					.Append("== ")
					.Append(this.EnemyName)
					.AppendLine(" ==")
					.AppendLine("{{Blades Creature Summary")
					.Append("|id=").AppendLine(this.Name)
					.AppendLine("|lvl=")
					.AppendLine("|archetype=")
					;
				BuildLvlStat(sb, "health", this.HealthBase, this.HealthIncrement);
				BuildNonZero(sb, "healthregen", this.HealthRegenRate);
				BuildLvlStat(sb, "stamina", this.StaminaBase, this.StaminaIncrement);
				BuildNonZero(sb, "staminaregen", this.StaminaRegenRate);
				BuildLvlStat(sb, "magicka", this.MagickaBase, this.MagickaIncrement);
				BuildNonZero(sb, "magickaregen", this.MagickaRegenRate);
				BuildLvlStat(sb, "damage", this.AttackDamageBase, this.AttackDamageIncrement);
				BuildLvlStat(sb, "block", this.InnateBlockRating, this.BlockRatingIncrement);
				BuildLvlStat(sb, "armor", this.InnateArmorRating, this.ArmorRatingIncrement);
				//// BuildResistance(sb, "none", this.InnateImmunityNone, this.InnateResistanceNone, this.InnateWeaknessNone);
				BuildResistance(sb, "fire", this.InnateImmunityFire, this.InnateResistanceFire, this.InnateWeaknessFire);
				BuildResistance(sb, "frost", this.InnateImmunityFrost, this.InnateResistanceFrost, this.InnateWeaknessFrost);
				BuildResistance(sb, "shock", this.InnateImmunityShock, this.InnateResistanceShock, this.InnateWeaknessShock);
				BuildResistance(sb, "poison", this.InnateImmunityPoison, this.InnateResistancePoison, this.InnateWeaknessPoison);
				BuildResistance(sb, "slashing", this.InnateImmunitySlashing, this.InnateResistanceSlashing, this.InnateWeaknessSlashing);
				BuildResistance(sb, "cleaving", this.InnateImmunityCleaving, this.InnateResistanceCleaving, this.InnateWeaknessCleaving);
				BuildResistance(sb, "bashing", this.InnateImmunityBashing, this.InnateResistanceBashing, this.InnateWeaknessBashing);
				BuildNonZero(sb, "attackCooldown", this.BackswingTime);
				BuildNonZero(sb, "blockCooldown", this.RecoveryToBlockTime);
				BuildNonZero(sb, "spellCooldown", this.SpellCooldownFactor);
				BuildNonZero(sb, "abilityCooldown", this.ManeuverCooldownFactor);
				switch (this.BaseDamageTypes.Count)
				{
					case 0:
						sb
							.Append("|damagetype=")
							.AppendLine(DamageTypeInfo.DamageTypes[0]);
						break;
					case 1:
					case 2:
						for (var dmgIndex = 0; dmgIndex < this.BaseDamageTypes.Count; dmgIndex++)
						{
							var dmgType = this.BaseDamageTypes[dmgIndex];
							var dmgNum = dmgIndex == 0 ? string.Empty : (dmgIndex + 1).ToStringInvariant();

							sb
								.Append("|damagetype")
								.Append(dmgNum)
								.Append('=')
								.AppendLine(dmgType.DamageType);
							if (dmgIndex == 0 && dmgType.PercentOfTotal is not 0 and not 100)
							{
								var val = dmgType.PercentOfTotal.ToString(siteCulture);
								sb
									.Append("|damagetype")
									.Append(dmgNum)
									.Append("%=")
									.AppendLine(val);
							}
						}

						break;
					default:
						throw new InvalidOperationException();
				}

				foreach (var ability in this.Abilities)
				{
					sb
						.Append('|')
						.Append(ability.Key)
						.Append('=')
						.Append(ability.Value)
						.AppendLine();
				}

				sb
					.AppendLine("}}")
					.AppendLine();

				return sb.ToString();
			}
			#endregion

			#region Public Override Methods
			public override string? ToString() => this.Name;
			#endregion

			#region Private Static Methods
			private static void BuildResistance(StringBuilder sb, string name, bool immune, double resist, double weakness)
			{
				if (resist < 0 || weakness < 0 || (resist != 0 && weakness != 0))
				{
					throw new InvalidOperationException();
				}

				if (weakness > 0)
				{
					resist = -weakness;
				}

				if (immune || resist != 0)
				{
					sb
						.Append('|')
						.Append(name)
						.Append('=');
					if (immune)
					{
						sb.Append("Immune");
					}
					else
					{
						sb.Append(resist);
					}

					sb.AppendLine();
				}
			}

			private static void BuildLvlStat(StringBuilder sb, string name, double baseValue, double mult)
			{
				sb
					.Append('|')
					.Append(name)
					.Append('=');
				if (baseValue == 0)
				{
					if (mult == 0)
					{
						sb.Append('0');
					}
				}
				else
				{
					sb.Append(baseValue);
					if (mult > 0)
					{
						sb.Append('+');
					}
					else if (mult < 0)
					{
						sb.Append('-');
					}
				}

				mult = Math.Abs(mult);
				switch (mult)
				{
					case 0:
						break;
					case 1:
						sb.Append("lvl");
						break;
					default:
						if (baseValue == 0)
						{
							sb
								.Append(mult)
								.Append("*lvl");
						}
						else
						{
							sb
								.Append('(')
								.Append(mult)
								.Append("*lvl)");
						}

						break;
				}

				sb.AppendLine();
			}

			private static void BuildNonZero(StringBuilder sb, string name, double value)
			{
				if (value != 0)
				{
					sb
						.Append('|')
						.Append(name)
						.Append('=')
						.Append(value)
						.AppendLine();
				}
			}
			#endregion
		}

		private sealed class BladesFamily
		{
			#region Constructors
			public BladesFamily(JToken token)
			{
				this.Name = (string?)token["m_Name"] ?? string.Empty;
				this.EnemyName = (string?)token["_enemyName"]?["_key"];
				if (token["_variants"] is JArray variants)
				{
					foreach (var variant in variants)
					{
						this.Variants.Add(new ValueRange<int>((int?)variant["_minLevel"] ?? 0, (int?)variant["_maxLevel"] ?? 0));
					}
				}

				this.Sort = this.Name;
			}
			#endregion

			#region Public Properties
			public string? EnemyName { get; }

			public string Name { get; }

			public string Sort { get; }

			public List<ValueRange<int>> Variants { get; } = [];
			#endregion

			#region Public Methods
			public string Build() => this.Variants.Count == 0 ? string.Empty : new StringBuilder()
				.Append(this.Name)
				.Append("  |lvl=")
				.AppendJoin(", ", this.Variants)
				.AppendLine()
				.ToString();
			#endregion

			#region Public Override Methods
			public override string? ToString() => this.Name;
			#endregion
		}

		private sealed class DamageTypeInfo
		{
			public static readonly string[] DamageTypes =
			[
				"weapon",
				"slashing",
				"cleaving",
				"bashing",
				"fire",
				"frost",
				"shock",
				"poison"
			];

			public DamageTypeInfo(JToken token)
			{
				this.DamageType = DamageTypes[(int?)token["_type"] ?? 0];
				this.PercentOfTotal = (float?)token["_percentOfTotal"] is float percent ? (int)(percent * 100) : 100;
			}

			public string DamageType { get; set; }

			public int PercentOfTotal { get; set; }

			public override string ToString() => $"{this.DamageType}: {this.PercentOfTotal.ToString(CultureInfo.CurrentCulture)}";
		}
		#endregion
	}
}