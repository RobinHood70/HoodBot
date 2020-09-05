namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;

	internal static class ReplacementData
	{
		// Replacements that are specific to certain IDs but do not apply to the general case.
		public static Dictionary<int, (string From, string To)> IdPartialReplacements { get; } = new Dictionary<int, (string, string)>
		{
			[23234] = ("|cffffff1|r second", "|cffffff1|r seconds"),
			[24574] = ("|cffffff1|r minute", "|cffffff1|r minutes"),
			[38096] = ("|cffffff1|r minute", "|cffffff60|r seconds"),
			[40242] = ("|cffffff1|r second.", "|cffffff1|r seconds."),
			[62107] = ("|cffffff1|r minute", "|cffffff60|r seconds"),
			[85982] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Feral Guardian|grizzly]]"),
			[85983] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Feral Guardian|grizzly]]"),
			[85984] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Feral Guardian|grizzly]]"),
			[85985] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Feral Guardian|grizzly]]"),
			[85986] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Eternal Guardian|grizzly]]"),
			[85987] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Eternal Guardian|grizzly]]"),
			[85988] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Eternal Guardian|grizzly]]"),
			[85989] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Eternal Guardian|grizzly]]"),
			[85990] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Wild Guardian|grizzly]]"),
			[85991] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Wild Guardian|grizzly]]"),
			[85992] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Wild Guardian|grizzly]]"),
			[85993] = ("Rouse a grizzly", "Rouse a [[Online: Familiars#Wild Guardian|grizzly]]"),
			[103710] = ("|cffffff1|r second", "|cffffff1|r seconds"),
		};

		public static Dictionary<string, string> NpcNameFixes { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["Ahmuna-La"] = "Ahmuna-la",
			["Dar-M'Athra"] = "Dar-m'Athra",
			["Haina-Daro"] = "Haina-daro",
			["Samati-Ko"] = "Samati-ko",
			["Sayya-Daro"] = "Sayya-daro",
		};

		public static HashSet<string> NpcNameSkips { get; } = new HashSet<string>(StringComparer.Ordinal)
		{
			"Adventurer", "Alarm", "Alchemy", "Argonian", "Ashlander", "Auroran", "Banished Guard", "Banished Royal Guard", "Barracks Door", "Blastbones", "Blighted Blastbones", "Bloodspawn", "Bone Armor", "Book", "Bosmer", "Brackenleaf", "Brawler", "Burn Buildings", "Butcher", "Capacitor", "Central Welkynd Stone", "Charged Atronach", "Crystal Prism", "Crystal Receiver", "Crystal Reciever", "Dark Anchor", "Dark Elf", "Dark Seducer", "Dremora Kynlurker\t", "Dremora", "Dro-m'Athra", "Eluza", "Empower Totem", "Enkindling Appendage", "Falinesti Faithful", "Feral Guardian", "Forgotten Tome", "Fractured Energy", "Gate Guard", "Goblin", "Golden Saint", "Guard Azad", "Guard Iralundore", "Guild Member", "Guise of the Cadaverous Assassin", "Healer", "Hei-Halai", "Herne", "Imperial", "Intensive Mender", "Invis Theatre", "InvisiDirector", "King's Guard", "Knight", "Lion Guard", "Mages Guild", "Malachite", "Manifestation of Terror", "Marona Girith", "Meteor", "Monastic Earrel", "Moonstone", "Mummy", "Nascent Indrik", "Necromancer", "Netch", "New Life Celebrant", "Nix-Ox Fabricant", "Nix-Ox Fabricant Steed", "Noordigloop the Clog", "Oleena", "Orc", "Poison Gas", "Portal", "Projection", "Pull Totem", "Q3381 Lizard02 PC Child", "Q5872 - Music Control", "Questionable Meat", "Ranger", "Razak's Opus", "Restoring Twilight", "Reveler", "Roneril", "Ruby", "Saber Cat", "Sand Storm", "Savage Book", "Senche-raht", "Sentry", "Shadow", "Silver", "Siphoning Totem", "Sithis", "Skeletal Mage", "Skeleton", "Slaughterer", "Sorcerer", "Spirit Guardian", "Spirit Mender", "Stalking Blastbones", "Storm", "Strange Cloud", "Strangler\t", "Summoned Flames", "Summoned Storm Atronach", "Sunna'rah", "The Insatiable", "The Ritual", "The Scarlet Judge", "The Voice of Ouze", "Thief Statue", "Thorn Geko", "Treasure Hunter", "Twilight Matriarch", "Vampiric Totem", "Vulkhel Guard", "Warden", "Welkynd Stone", "Werewolf Berserker", "Werewolf", "Wrath of Sithis", "Wrest Totem", "Yargob gro-Shelob",
		};

		public static Dictionary<string, string> SkillNameFixes { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["Agility"] = "Agility (skill)",
			["Betty Netch"] = "Betty Netch (skill)",
			["Consuming Darkness"] = "Consuming Darkness (skill)",
			["Executioner"] = "Executioner (skill)",
			["Farsight"] = "Farsight (skill)",
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
