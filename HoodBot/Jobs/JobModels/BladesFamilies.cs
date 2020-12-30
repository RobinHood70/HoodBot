namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;

	public class BladesFamilies : WikiJob
	{
		#region Static Fields
		private static readonly Dictionary<string, string> AbilityLookup = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["4e760726-b012-4b25-bc92-0cd6312d6601"] = "absorb",
			["be56c560-a4ba-47ad-8513-f24c342ca594"] = "adrenalinedodge",
			["3f575c09-ca6d-40a8-977b-b2c5d44d1b5c"] = "armsman",
			["ed235f8d-0648-4aee-b955-a951562f549d"] = "augmentedflames",
			["270ae0e8-3758-4840-bdc2-e882008f0f0a"] = "augmentedfrost",
			["66faf7c6-426c-4689-99e6-ccdd25b9bc11"] = "augmentedpoison",
			["3c0510d4-84ef-40b6-b0f4-2b096ae89860"] = "augmentedshock",
			["64a6a981-0dc8-4fc1-b043-a75d052b00f5"] = "barbarian",
			["85596d85-5f2a-4f3a-9059-960eaff79a87"] = "blind",
			["c4b48518-e847-4f3d-81a2-2856bdb4ed98"] = "blizzardarmor",
			["e0b549c8-a686-49d4-a800-4661fff73e1d"] = "combatfocus",
			["2eff05aa-a6a8-4060-9bac-8be1bb60c040"] = "conservationist",
			["e07f9b1a-64db-44ef-ba25-0e4378789ddc"] = "consuminginferno",
			["dfb8d247-1333-42eb-9730-a1c16d10584f"] = "delayedlightningbolt",
			["1e7f0dd6-6015-4f65-b811-3246e407e330"] = "dodgingstrike",
			["f60f69d4-24bc-46fb-a4fa-d4abdac0f06f"] = "echoweapon",
			["788aa75e-4796-4d57-bbab-b1b901623f16"] = "elementalprotection",
			["780b82d1-a371-4454-baea-e18389f315e5"] = "enchantmentsynergy",
			["d07a8d30-9a1c-49b0-866d-97a8aa1534cf"] = "fireball",
			["256b722a-c4bd-45ad-ae25-15ee805fbc70"] = "firewall",
			["e685e88f-34e7-4fdc-bacd-618763078d65"] = "focusingdodge",
			["4be1d681-c35d-4540-b255-c2910ac80664"] = "frostbite",
			["cc768bae-a063-4885-8207-f39c6542fb36"] = "guardbreaker",
			["69ffa3fd-deb7-4824-bab6-ac6450f19676"] = "harryingbash",
			["09aa3390-8f42-4cd5-a88c-5c94d5e1dd29"] = "healingsurge",
			["cfee0b02-6d91-4d34-869c-a7e54329060d"] = "icespike",
			["66610227-07bf-4e3b-a75b-c591271f0817"] = "indomitablesmash",
			["7fc15804-1637-40a9-8dcc-3ea1eb0f778d"] = "lightningbolt",
			["1c836287-44d8-40a6-bf02-d457f57d171d"] = "magickasurge",
			["3dcb91c5-2279-4003-b6a6-53eac6fb86c8"] = "matchingset",
			["83784ade-533e-4965-a540-05bfd4f056d8"] = "maximumpower",
			["d6d7ad89-0c41-410f-8a19-c4850ab9fe4f"] = "mettle",
			["9fdc4d52-ce90-44f8-9b5d-21f31e27dbda"] = "paralyze",
			["cdab44fb-6ff6-4701-a4ec-d19cce79e49f"] = "piercingstrikes",
			["66bdc017-30c5-4b5e-9753-215c45056f6a"] = "poisoncloud",
			["ce6b63e9-9f18-49c4-aee0-51f7985f9892"] = "powerattack",
			["eb0cb7e6-47cf-48e7-8cc9-dbf80fc77f13"] = "quickstrikes",
			["0cfe29cd-89d9-42ad-9227-8308e2f87c7f"] = "recklessfury",
			["e08f95de-85bb-4829-ba7e-cf45bc6fb422"] = "recoverystrikes",
			["ba61ce46-163f-4a61-8ede-f5b7ae365e40"] = "reflectingbash",
			["7f78d342-f346-4210-9f62-01a540687bb3"] = "renewingdodge",
			["91078132-ef5c-492a-97f2-ac69be5140a8"] = "resistelements",
			["11ebd583-fc0c-44f0-8dbf-5c7207526064"] = "scout",
			["f9a2373b-a84f-4716-90ce-165baa2dd6ed"] = "shieldbash",
			["c112c956-eaac-4d7d-878e-32cd7d1e5209"] = "skullcrusher",
			["9b915ec3-c63b-4b62-b417-4c5436d45fc1"] = "staggeringbash",
			["2ab06506-2114-4738-bd87-f6f402d3ce2e"] = "thunderstorm",
			["e14eedd5-cd50-404e-9697-a37fd1d2ce00"] = "venomstrikes",
			["65ede044-d68a-4b2b-8f0c-02075ad133cc"] = "ward",
			["50b19efb-c7d8-41fe-8791-4782cff99e70"] = "fire_breath",
			["95eeca9c-7fbd-40a8-9d5b-9c061f08403c"] = "firestorm_armor",
			["ef3e58a3-9bf0-49ca-8e1e-9dd34c61557d"] = "frost_breath",
			["ade28628-e213-4eaf-9eeb-5fce53870b92"] = "polar_slam",
			["c1494bda-4219-4bec-8017-fb376a058ea7"] = "siphon_life",
			["3b610e1d-b4d0-48ae-bc6a-7b3fc6fe5f12"] = "tempest_armor",
			["9bc43c3d-4eb7-4d9c-b507-b3288f3b9ea1"] = "flameatronachpower",
			["b405918e-5988-49e6-91f2-35761add9a5e"] = "frostatronachpower",
			["a6bf8b2e-ee6e-4d61-a806-e46386451a42"] = "stormatronachpower",
		};

		private static readonly string[] DamageTypes = new[]
		{
			"None",
			"Slashing",
			"Cleaving",
			"Bashing",
			"Fire",
			"Frost",
			"Shock",
			"Poison"
		};
		#endregion

		#region Constructors
		[JobInfo("Blades Families")]
		public BladesFamilies(JobManager jobManager)
		: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var family = ReadFamily("EQ07_Caster_Family");
			var enemy = ReadEnemy("EQ07_01_NecromancerLowLevel");
			Debug.WriteLine(ToWikiText(family, enemy));
		}
		#endregion

		#region Private Static Methods
		private static BladesEnemy ReadEnemy(string fileName)
		{
			var text = File.ReadAllText(UespSite.GetBotDataFolder(fileName + ".json"));
			var token = JToken.Parse(text);
			if (token["_stats"] is not JToken stats)
			{
				throw new InvalidOperationException();
			}

			var retval = new BladesEnemy
			{
				Name = (string?)token["m_Name"],
				HealthBase = (double?)stats["_healthBase"] ?? 0,
				HealthIncrement = (double?)stats["_healthIncrement"] ?? 0,
				MagickaBase = (double?)stats["_magickaBase"] ?? 0,
				MagickaIncrement = (double?)stats["_magickaIncrement"] ?? 0,
				StaminaBase = (double?)stats["_staminaBase"] ?? 0,
				StaminaIncrement = (double?)stats["_staminaIncrement"] ?? 0,
				HealthRegenRate = (double?)stats["_healthRegenRate"] ?? 0,
				HealthRegenRateOutsideCombat = (double?)stats["_healthRegenRateOutsideCombat"] ?? 0,
				StaminaRegenRate = (double?)stats["_staminaRegenRate"] ?? 0,
				StaminaRegenRateOutsideCombat = (double?)stats["_staminaRegenRateOutsideCombat"] ?? 0,
				MagickaRegenRate = (double?)stats["_magickaRegenRate"] ?? 0,
				MagickaRegenRateOutsideCombat = (double?)stats["_magickaRegenRateOutsideCombat"] ?? 0,
				InnateResistanceNone = (double?)stats["_innateResistanceNone"] ?? 0,
				InnateResistanceSlashing = (double?)stats["_innateResistanceSlashing"] ?? 0,
				InnateResistanceCleaving = (double?)stats["_innateResistanceCleaving"] ?? 0,
				InnateResistanceBashing = (double?)stats["_innateResistanceBashing"] ?? 0,
				InnateResistanceFire = (double?)stats["_innateResistanceFire"] ?? 0,
				InnateResistanceFrost = (double?)stats["_innateResistanceFrost"] ?? 0,
				InnateResistanceShock = (double?)stats["_innateResistanceShock"] ?? 0,
				InnateResistancePoison = (double?)stats["_innateResistancePoison"] ?? 0,
				InnateWeaknessNone = (double?)stats["_innateWeaknessNone"] ?? 0,
				InnateWeaknessSlashing = (double?)stats["_innateWeaknessSlashing"] ?? 0,
				InnateWeaknessCleaving = (double?)stats["_innateWeaknessCleaving"] ?? 0,
				InnateWeaknessBashing = (double?)stats["_innateWeaknessBashing"] ?? 0,
				InnateWeaknessFire = (double?)stats["_innateWeaknessFire"] ?? 0,
				InnateWeaknessFrost = (double?)stats["_innateWeaknessFrost"] ?? 0,
				InnateWeaknessShock = (double?)stats["_innateWeaknessShock"] ?? 0,
				InnateWeaknessPoison = (double?)stats["_innateWeaknessPoison"] ?? 0,
				InnateImmunityNone = (int?)stats["_innateImmunityNone"] == 1,
				InnateImmunitySlashing = (int?)stats["_innateImmunitySlashing"] == 1,
				InnateImmunityCleaving = (int?)stats["_innateImmunityCleaving"] == 1,
				InnateImmunityBashing = (int?)stats["_innateImmunityBashing"] == 1,
				InnateImmunityFire = (int?)stats["_innateImmunityFire"] == 1,
				InnateImmunityFrost = (int?)stats["_innateImmunityFrost"] == 1,
				InnateImmunityShock = (int?)stats["_innateImmunityShock"] == 1,
				InnateImmunityPoison = (int?)stats["_innateImmunityPoison"] == 1,
				InnateBlockRating = (double?)stats["_innateBlockRating"] ?? 0,
				InnateArmorRating = (double?)stats["_innateArmorRating"] ?? 0,
				AttackDamageBase = (double?)stats["_attackDamageBase"] ?? 0,
				AttackDamageIncrement = (double?)stats["_attackDamageIncrement"] ?? 0,
				ArmorRatingIncrement = (double?)stats["_armorRatingIncrement"] ?? 0,
				BlockRatingIncrement = (double?)stats["_blockRatingIncrement"] ?? 0,
				BackswingTime = (double?)stats["_backswingTime"] ?? 0,
				MaxDamageTime = (double?)stats["_maxDamageTime"] ?? 0,
				DamageDecayTime = (double?)stats["_damageDecayTime"] ?? 0,
				ComboDamageFactor = (double?)stats["_comboDamageFactor"] ?? 0,
				RecoveryTime = (double?)stats["_recoveryTime"] ?? 0,
				RecoveryToComboTime = (double?)stats["_recoveryToComboTime"] ?? 0,
				RecoveryToBlockTime = (double?)stats["_recoveryToBlockTime"] ?? 0,
				RecoveryToNeutralTime = (double?)stats["_recoveryToNeutralTime"] ?? 0,
				TimeToBlock = (double?)stats["_timeToBlock"] ?? 0,
				MinimumDamageFactor = (double?)stats["_minimumDamageFactor"] ?? 0,
				MaximumDamageFactor = (double?)stats["_maximumDamageFactor"] ?? 0,
				KillScoreMultiplier = (double?)stats["_killScoreMultiplier"] ?? 0,
				AttackingDamageReaction = (double?)stats["_attackingDamageReaction"] ?? 0,
				NotAttackingDamageReaction = (double?)stats["_notAttackingDamageReaction"] ?? 0,
				InnateImmunityStagger = (int?)stats["_innateImmunityStagger"] == 1,
				InnateImmunityParalyze = (int?)stats["_innateImmunityParalyze"] == 1,
				SpellCooldownFactor = (double?)token["_spellCooldownFactor"] ?? 0,
				ManeuverCooldownFactor = (double?)token["_maneuverCooldownFactor"] ?? 0,
				IsSpellCaster = (int?)token["_isSpellCaster"] == 1,
				EnemyName = (string?)token["_enemyName"]?["_key"],
			};

			if (stats["_baseDamageTypes"] is JToken damageTypes)
			{
				foreach (var damageType in damageTypes)
				{
					var dmgText = DamageTypes[(int)damageType];
					retval.BaseDamageTypes.Add(dmgText);
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
						retval.Abilities.Add(abilityName, rank);
					}
				}
			}

			return retval;
		}

		private static BladesFamily ReadFamily(string fileName)
		{
			var text = File.ReadAllText(UespSite.GetBotDataFolder(fileName + ".json"));
			var token = JToken.Parse(text);
			var retval = new BladesFamily
			{
				Name = (string?)token["m_Name"],
				EnemyName = (string?)token["_enemyName"]?["_key"]
			};
			if (token["_variants"] is JArray variants)
			{
				foreach (var variant in variants)
				{
					retval.Variants.Add(new ValueRange<int>((int?)variant["_minLevel"] ?? 0, (int?)variant["_maxLevel"] ?? 0));
				}
			}

			return retval;
		}

		private static string ToWikiText(BladesFamily family, BladesEnemy enemy)
		{
			var sb = new StringBuilder();
			sb
				.Append("Family Name=").AppendLine(family.Name)
				.Append("Enemy Name=").AppendLine(family.EnemyName)
				.AppendLine();
			foreach (var variant in family.Variants)
			{
				BuildEnemy(enemy, sb, variant);
			}

			return sb.ToString();
		}

		private static void BuildEnemy(BladesEnemy enemy, StringBuilder sb, ValueRange<int> variant)
		{
			sb
				.AppendLine("{{Blades Creature Summary")
				.Append("|id=").AppendLine(enemy.Name)
				.Append("|race=").Append(enemy.Race).AppendLine()
				.Append("|gender=").Append(enemy.Gender).AppendLine()
				.Append("|lvl=").Append(variant).AppendLine();
			BuildLvlStat(sb, "health", enemy.HealthBase, enemy.HealthIncrement);
			BuildNonZero(sb, "healthregen", enemy.HealthRegenRate * 100);
			BuildLvlStat(sb, "stamina", enemy.StaminaBase, enemy.StaminaIncrement);
			BuildNonZero(sb, "staminaregen", enemy.StaminaRegenRate * 100);
			BuildLvlStat(sb, "magicka", enemy.MagickaBase, enemy.MagickaIncrement);
			BuildNonZero(sb, "magickaregen", enemy.MagickaRegenRate * 100);
			BuildLvlStat(sb, "damage", enemy.AttackDamageBase, enemy.AttackDamageIncrement);
			BuildLvlStat(sb, "block", enemy.InnateBlockRating, enemy.BlockRatingIncrement);
			BuildLvlStat(sb, "armor", enemy.InnateArmorRating, enemy.ArmorRatingIncrement);
			//// BuildResistance(sb, "none", enemy.InnateImmunityNone, enemy.InnateResistanceNone, enemy.InnateWeaknessNone);
			BuildResistance(sb, "fire", enemy.InnateImmunityFire, enemy.InnateResistanceFire, enemy.InnateWeaknessFire);
			BuildResistance(sb, "frost", enemy.InnateImmunityFrost, enemy.InnateResistanceFrost, enemy.InnateWeaknessFrost);
			BuildResistance(sb, "shock", enemy.InnateImmunityShock, enemy.InnateResistanceShock, enemy.InnateWeaknessShock);
			BuildResistance(sb, "poison", enemy.InnateImmunityPoison, enemy.InnateResistancePoison, enemy.InnateWeaknessPoison);
			BuildResistance(sb, "slashing", enemy.InnateImmunitySlashing, enemy.InnateResistanceSlashing, enemy.InnateWeaknessSlashing);
			BuildResistance(sb, "cleaving", enemy.InnateImmunityCleaving, enemy.InnateResistanceCleaving, enemy.InnateWeaknessCleaving);
			BuildResistance(sb, "bashing", enemy.InnateImmunityBashing, enemy.InnateResistanceBashing, enemy.InnateWeaknessBashing);
			BuildNonZero(sb, "attackCooldown", enemy.BackswingTime);
			BuildNonZero(sb, "blockCooldown", enemy.RecoveryToBlockTime);
			BuildNonZero(sb, "spellCooldown", enemy.SpellCooldownFactor);
			BuildNonZero(sb, "abilityCooldown", enemy.ManeuverCooldownFactor);
			sb
				.Append("|damagetype=")
				.AppendLine(enemy.BaseDamageTypes.Count == 0 ? "weapon" : string.Join(", ", enemy.BaseDamageTypes));

			foreach (var ability in enemy.Abilities)
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
		}

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
			}

			switch (mult)
			{
				case 0:
					break;
				case 1:
					sb.Append("lvl");
					break;
				default:
					sb
						.Append('(')
						.Append(mult)
						.Append("*lvl)");
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

		#region Private Classes
		private sealed class BladesEnemy
		{
			public IDictionary<string, int> Abilities { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

			public double ArmorRatingIncrement { get; set; }

			public double AttackDamageBase { get; set; }

			public double AttackDamageIncrement { get; set; }

			public double AttackingDamageReaction { get; set; }

			public double BackswingTime { get; set; }

			public List<string> BaseDamageTypes { get; } = new();

			public double BlockRatingIncrement { get; set; }

			public double ComboDamageFactor { get; set; }

			public double DamageDecayTime { get; set; }

			public string? EnemyName { get; set; }

			public string Gender { get; set; } = "Varies";

			public double HealthBase { get; internal set; }

			public double HealthIncrement { get; set; }

			public double HealthRegenRate { get; set; }

			public double HealthRegenRateOutsideCombat { get; set; }

			public double InnateArmorRating { get; set; }

			public double InnateBlockRating { get; set; }

			public bool InnateImmunityBashing { get; set; }

			public bool InnateImmunityCleaving { get; set; }

			public bool InnateImmunityFire { get; set; }

			public bool InnateImmunityFrost { get; set; }

			public bool InnateImmunityNone { get; set; }

			public bool InnateImmunityParalyze { get; set; }

			public bool InnateImmunityPoison { get; set; }

			public bool InnateImmunityShock { get; set; }

			public bool InnateImmunitySlashing { get; set; }

			public bool InnateImmunityStagger { get; set; }

			public double InnateResistanceBashing { get; set; }

			public double InnateResistanceCleaving { get; set; }

			public double InnateResistanceFire { get; set; }

			public double InnateResistanceFrost { get; set; }

			public double InnateResistanceNone { get; set; }

			public double InnateResistancePoison { get; set; }

			public double InnateResistanceShock { get; set; }

			public double InnateResistanceSlashing { get; set; }

			public double InnateWeaknessBashing { get; set; }

			public double InnateWeaknessCleaving { get; set; }

			public double InnateWeaknessFire { get; set; }

			public double InnateWeaknessFrost { get; set; }

			public double InnateWeaknessNone { get; set; }

			public double InnateWeaknessPoison { get; set; }

			public double InnateWeaknessShock { get; set; }

			public double InnateWeaknessSlashing { get; set; }

			public bool IsSpellCaster { get; set; }

			public double KillScoreMultiplier { get; set; }

			public double MagickaBase { get; set; }

			public double MagickaIncrement { get; set; }

			public double MagickaRegenRate { get; set; }

			public double MagickaRegenRateOutsideCombat { get; set; }

			public double ManeuverCooldownFactor { get; set; }

			public double MaxDamageTime { get; set; }

			public double MaximumDamageFactor { get; set; }

			public double MinimumDamageFactor { get; set; }

			public string? Name { get; set; }

			public double NotAttackingDamageReaction { get; set; }

			public string Race { get; set; } = "Varies";

			public double RecoveryTime { get; set; }

			public double RecoveryToBlockTime { get; set; }

			public double RecoveryToComboTime { get; set; }

			public double RecoveryToNeutralTime { get; set; }

			public double SpellCooldownFactor { get; set; }

			public double StaminaBase { get; set; }

			public double StaminaIncrement { get; set; }

			public double StaminaRegenRate { get; set; }

			public double StaminaRegenRateOutsideCombat { get; set; }

			public double TimeToBlock { get; set; }
		}

		private sealed class BladesFamily
		{
			public string? EnemyName { get; set; }

			public string? Name { get; set; }

			public List<ValueRange<int>> Variants { get; } = new List<ValueRange<int>>();
		}
		#endregion
	}
}