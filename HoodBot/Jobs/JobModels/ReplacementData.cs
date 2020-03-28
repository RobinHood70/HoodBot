namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;

	public static class ReplacementData
	{
		// This is a fugly way to fix up the descriptions, but it simplifies things enormously.
		public static Dictionary<int, string> IdReplacements { get; } = new Dictionary<int, string>
		{
			[23234] = "Transform yourself into pure energy and flash forward, stunning enemies near your final location for |cffffff1|r seconds. This effect cannot be blocked. Casting again within |cffffff4|r seconds costs |cffffff33|r% more {{ESO Magicka Link}}.",
			[24574] = "Place a rune of protection on yourself for |cffffff1|r minutes. While active, the next enemy to attack you is imprisoned in a constricting sphere of dark magic, stunning them after a short delay for |cffffff3|r seconds. This stun cannot be blocked.",
			[38096] = "Conceal two sinister traps, one at the targeted location and another next to you, which take |cffffff2|r seconds to arm and last for |cffffff60|r seconds. When each trap is triggered, a dark spirit is summoned to terrify up to |cffffff6|r enemies, causing them to cower in fear for |cffffff4|r seconds.",
			[39104] = "Pounce on an enemy with primal fury, dealing |cffffff$1|r Physical Damage. Pouncing from at least |cffffff10|r meters away adds |cffffff1|r seconds to the duration of your Werewolf Transformation.",
			[40242] = "Hurl a ball of caltrops that scatter over the target area, dealing |cffffff$1|r Physical Damage every |cffffff1|r second to enemies inside, and reducing their Movement Speed by |cffffff50|r%. Enemies who take damage from the caltrops have Major Fracture applied to them, reducing their {{ESO Resistance Link|Physical}} by |cffffff5280|r for |cffffff1|r seconds.",
			[62107] = "Focus your senses for |cffffff60|r seconds, reducing your damage taken by |cffffff2|r% with every Light or Heavy Attack, up to 5 times. While active, hitting an enemy with |cffffff5|r Light or Heavy Attacks converts this ability into Assassin's Scourge, allowing you to fire a spectral arrow for half cost that deals |cffffff$1|r Disease Damage, and heals for |cffffff33|r% of the damage dealt if you are within |cffffff7|r meters of the enemy.",
			[103710] = "Bend time and space around you to gain Major Expedition for |cffffff4|r seconds and Minor Force for |cffffff12|r seconds, increasing your Movement Speed by |cffffff30|r% and Critical Damage by |cffffff10|r%. Activating this ability removes all snares and immobilizations from you and grants immunity to them for |cffffff1|r seconds.",
		};

		public static Dictionary<string, string> NpcNameFixes { get; } = new Dictionary<string, string>
		{
			["Ahmuna-La"] = "Ahmuna-la",
			["Haina-Daro"] = "Haina-daro",
			["Samati-Ko"] = "Samati-ko",
			["Sayya-Daro"] = "Sayya-daro",
		};

		public static HashSet<string> NpcNameSkips { get; } = new HashSet<string>
		{
			"Adventurer", "Alarm", "Alchemy", "Argonian", "Ashlander", "Auroran", "Barracks Door", "Blastbones", "Blighted Blastbones", "Bloodspawn", "Bone Armor", "Bosmer", "Brackenleaf", "Brawler", "Burn Buildings", "Butcher", "Capacitor", "Central Welkynd Stone", "Charged Atronach", "Crystal Prism", "Crystal Receiver", "Crystal Reciever", "Dark Anchor", "Dark Elf", "Dark Seducer", "Dremora Kynlurker\t", "Dremora", "Dro-m'Athra", "Eluza", "Empower Totem", "Enkindling Appendage", "Falinesti Faithful", "Feral Guardian", "Forgotten Tome", "Fractured Energy", "Goblin", "Golden Saint", "Guild Member", "Guise of the Cadaverous Assassin", "Healer", "Hei-Halai", "Herne", "Imperial", "Intensive Mender", "Invis Theatre", "InvisiDirector", "King's Guard", "Knight", "Lion Guard", "Mages Guild", "Malachite", "Manifestation of Terror", "Marona Girith", "Meteor", "Monastic Earrel", "Moonstone", "Mummy", "Nascent Indrik", "Necromancer", "Netch", "New Life Celebrant", "Nix-Ox Fabricant Steed", "Noordigloop the Clog", "Oleena", "Orc", "Poison Gas", "Portal", "Projection", "Pull Totem", "Q3381 Lizard02 PC Child", "Q5872 - Music Control", "Questionable Meat", "Ranger", "Razak's Opus", "Restoring Twilight", "Reveler", "Roneril", "Ruby", "Sand Storm", "Savage Book", "Senche-raht", "Sentry", "Shadow", "Silver", "Siphoning Totem", "Sithis", "Skeletal Mage", "Skeleton", "Slaughterer", "Sorcerer", "Spirit Guardian", "Spirit Mender", "Stalking Blastbones", "Storm", "Strange Cloud", "Strangler\t", "Summoned Flames", "Summoned Storm Atronach", "Sunna'rah", "The Insatiable", "The Ritual", "The Scarlet Judge", "Thief Statue", "Thorn Geko", "Treasure Hunter", "Twilight Matriarch", "Vampiric Totem", "Vulkhel Guard", "Warden", "Welkynd Stone", "Werewolf Berserker", "Werewolf", "Wrath of Sithis", "Wrest Totem", "Yargob gro-Shelob"
		};

		public static Dictionary<string, string> SkillNameFixes { get; } = new Dictionary<string, string>
		{
			["Agility"] = "Agility (skill)",
			["Betty Netch"] = "Betty Netch (skill)",
			["Consuming Darkness"] = "Consuming Darkness (skill)",
			["Executioner"] = "Executioner (skill)",
			["Guard"] = "Guard (skill)",
			["Hireling - Enchanting"] = "Hireling (Enchanting)",
			["Hireling - Provisioning"] = "Hireling (Provisioning)",
			["Ruination"] = "Ruination (skill)",
			["Skeletal Mage"] = "Skeletal Mage (skill)",
			["Spectral Assassin"] = "Spectral Assassin (skill)",
			["Unstoppable"] = "Unstoppable (skill)",
			["Woodworking"] = "Woodworking (skill)",
		};

		public static IReadOnlyList<Synergy> Synergies { get; } = new List<Synergy>
		{
			/* Do not re-sort this into alphabetical order or it will screw things up (e.g., "Blood Funnel" getting replaced multiple times) */
			new Synergy("Blood Altar", "Blood Funnel", "{{ESO Synergy Link|Blood Funnel}}"),
			new Synergy("Blood Altar", "Blood Feast", "{{ESO Synergy Link|Blood Feast|Blood Funnel}}"),
			new Synergy("Bone Shield", "Bone Wall", "{{ESO Synergy Link|Bone Wall}}"),
			new Synergy("Bone Shield", "Spinal Surge", "{{ESO Synergy Link|Spinal Surge}}"),
			new Synergy("Cleansing Ritual", "Purify", "{{ESO Synergy Link|Purify}}"),
			new Synergy("Consuming Darkness", "Hidden Refresh", "{{ESO Synergy Link|Hidden Refresh}}"),
			new Synergy("Dark Talons", "Impale", "{{ESO Synergy Link|Impale}}"),
			new Synergy("Dragonknight Standard", "Shackle", "{{ESO Synergy Link|Shackle}}"),
			new Synergy("Inner Fire", "Radiate", "{{ESO Synergy Link|Radiate}}"),
			new Synergy("Lightning Splash", "Conduit", "{{ESO Synergy Link|Conduit}}"),
			new Synergy("Necrotic Orb", "Healing Combustion", "{{ESO Synergy Link|Healing Combustion|Magicka Combustion}}"),
			new Synergy("Necrotic Orb", "Combustion", "{{ESO Synergy Link|Combustion}}"),
			new Synergy("Necrotic Orb", "Magicka Combustion", "{{ESO Synergy Link|Magicka Combustion}}"),
			new Synergy("Nova", "Supernova", "{{ESO Synergy Link|Supernova}}"),
			new Synergy("Nova", "Gravity Crush", "{{ESO Synergy Link|Gravity Crush|Supernova}}"),
			new Synergy("Piercing Howl", "Feeding Frenzy", "{{ESO Synergy Link|Feeding Frenzy}}"),
			new Synergy("Soul Shred", "Soul Leech", "{{ESO Synergy Link|Soul Leech}}"),
			new Synergy("Spear Shards", "Blessed Shards", "{{ESO Synergy Link|Blessed Shards}}"),
			new Synergy("Spear Shards", "Holy Shards", "{{ESO Synergy Link|Holy Shards|Blessed Shards}}"),
			new Synergy("Summon Storm Atronach", "Charged Lightning", "{{ESO Synergy Link|Charged Lightning}}"),
			new Synergy("Trapping Webs", "Spawn Broodlings", "{{ESO Synergy Link|Spawn Broodlings}}"),
			new Synergy("Trapping Webs", "Arachnophobia", "{{ESO Synergy Link|Arachnophobia|Spawn Broodlings}}"),
			new Synergy("Trapping Webs", "Black Widows", "{{ESO Synergy Link|Black Widows|Spawn Broodlings}}"),
		};
	}
}
