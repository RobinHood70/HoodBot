namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;

	public static class ReplacementData
	{
		// This is a fugly way to fix up the descriptions, but it simplifies things enormously.
		public static Dictionary<int, string> IdReplacements { get; } = new Dictionary<int, string>
		{
			[24574] = "Place a rune of protection on yourself for |cffffff1|r minutes. While active, the next enemy to attack you is imprisoned in a constricting sphere of dark magic, stunning them after a short delay for |cffffff3.5|r seconds. This stun cannot be blocked.",
			[28304] = "Surprise an enemy with a deep lunge, dealing |cffffff$1|r Physical Damage and reducing their Movement Speed by |cffffff60|r% for |cffffff1|r seconds. Also afflicts the enemy with Minor Maim, reducing their damage done by |cffffff15|r% for |cffffff9|r seconds.",
			[38096] = "Conceal two sinister traps, one at the targeted location and another next to you, which take |cffffff2|r seconds to arm and last for |cffffff60|r seconds. When each trap is triggered, a dark spirit is summoned to terrify up to |cffffff6|r enemies, causing them to flee in fear for |cffffff4|r seconds. After the fear ends, their Movement Speed is reduced by |cffffff50|r% for |cffffff4|r seconds.",
			[39104] = "Pounce on an enemy with primal fury, dealing |cffffff$1|r Physical Damage. Pouncing from at least |cffffff10|r meters away adds |cffffff1|r seconds to the duration of your Werewolf Transformation.",
		};

		// Although these could be private to specific classes, they're put here in order to have a centralized location where they can easily be found.
		public static Dictionary<string, string> SetNameFixes { get; } = new Dictionary<string, string>
		{
			["Agility"] = "Agility (set)",
			["Alessian Order"] = "Alessian Order (set)",
			["Balorgh"] = "Balorgh (set)",
			["Blood Spawn"] = "Blood Spawn (set)",
			["Chokethorn"] = "Chokethorn (set)",
			["Giant Spider"] = "Giant Spider (set)",
			["Grothdarr"] = "Grothdarr (set)",
			["Iceheart"] = "Iceheart (set)",
			["Infernal Guardian"] = "Infernal Guardian (set)",
			["Maw of the Infernal"] = "Maw of the Infernal (set)",
			["Mighty Chudan"] = "Mighty Chudan (set)",
			["Molag Kena"] = "Molag Kena (set)",
			["Nerien'eth"] = "Nerien'eth (set)",
			["Night Terror"] = "Night Terror (set)",
			["Selene"] = "Selene (set)",
			["Sentinel of Rkugamz"] = "Sentinel of Rkugamz (set)",
			["Sentry"] = "Sentry (set)",
			["Shadow Walker"] = "Shadow Walker (set)",
			["Shadowrend"] = "Shadowrend (set)",
			["Slimecraw"] = "Slimecraw (set)",
			["Spawn of Mephala"] = "Spawn of Mephala (set)",
			["Stormfist"] = "Stormfist (set)",
			["Symphony of Blades"] = "Symphony of Blades (set)",
			["Swarm Mother"] = "Swarm Mother (set)",
			["The Troll King"] = "The Troll King (set)",
			["Thurvokun"] = "Thurvokun (set)",
			["Tremorscale"] = "Tremorscale (set)",
			["Valkyn Skoria"] = "Valkyn Skoria (set)",
			["Velidreth"] = "Velidreth (set)",
			["Vykosa"] = "Vykosa (set)",
			["Winterborn"] = "Winterborn (set)",
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
			["Spectral Assassin"] = "Spectral Assassin (skill)",
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

		public static Dictionary<string, string> TextReplacements { get; } = new Dictionary<string, string>
		{
			["% ."] = "%.",
			["minute When triggered"] = "minute. When triggered",
		};

		public static IList<string> TextReplacementsUsed { get; } = new List<string>();
	}
}
