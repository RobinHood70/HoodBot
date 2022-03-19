namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using MySql.Data.MySqlClient;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;

	internal static class EsoLog
	{
		#region Static Fields
		private static readonly Regex ColourCode = new(@"\A\|c[0-9A-F]{6}(.*?)\|r\Z", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Regex TrailingDigits = new(@"\s*\d+\Z", RegexOptions.None, Globals.DefaultRegexTimeout);
		#endregion

		#region Public Properties
		public static string Connection { get; } = App.GetConnectionString("EsoLog");

		public static Dictionary<int, string> MechanicNames { get; } = new Dictionary<int, string>
		{
			[-2] = "Health",
			[0] = "Magicka",
			[6] = "Stamina",
			[10] = "Ultimate",
			[-50] = "Ultimate (no weapon damage)",
			[-51] = "Light Armor #",
			[-52] = "Medium Armor #",
			[-53] = "Heavy Armor #",
			[-54] = "Dagger #",
			[-55] = "Armor Type #",
			[-56] = "Spell + Weapon Damage",
			[-57] = "Assassination Skills Slotted",
			[-58] = "Fighters Guild Skills Slotted",
			[-59] = "Draconic Power Skills Slotted",
			[-60] = "Shadow Skills Slotted",
			[-61] = "Siphoning Skills Slotted",
			[-62] = "Sorcerer Skills Slotted",
			[-63] = "Mages Guild Skills Slotted",
			[-64] = "Support Skills Slotted",
			[-65] = "Animal Companion Skills Slotted",
			[-66] = "Green Balance Skills Slotted",
			[-67] = "Winter's Embrace Slotted",
			[-68] = "Magicka with Health Cap",
			[-69] = "Bone Tyrant Slotted",
			[-70] = "Grave Lord Slotted",
			[-71] = "Spell Damage Capped",
			[-72] = "Magicka and Weapon Damage",
			[-73] = "Magicka and Spell Damage",
			[-74] = "Weapon Power",
			[-75] = "Constant Value",
			[-76] = "Health or Spell Damage",
			[-77] = "Max Resistance",
			[-78] = "Magicka and Light Armor (Health Capped)",
			[-79] = "Health or Weapon/Spell Damage",
		};
		#endregion

		#region Public Methods
		public static IEnumerable<NpcLocationData> GetNpcLocations(List<long> npcIds)
		{
			List<NpcLocationData> retval = new();
			var query = $"SELECT npcId, zone, locCount FROM npcLocations WHERE npcId IN ({string.Join(", ", npcIds)}) AND zone != 'Tamriel'";
			for (var retries = 2; retries >= 0; retries--)
			{
				try
				{
					foreach (var location in Database.RunQuery(Connection, query, row => new NpcLocationData(row)))
					{
						retval.Add(location);
					}

					retries = 0;
				}
				catch (TimeoutException) when (retries > 0)
				{
					// Do nothing
				}
				catch (MySqlException) when (retries > 0)
				{
					// Do nothing
				}
			}

			return retval;
		}

		public static NpcCollection GetNpcs()
		{
			// Note: for now, it's assumed that the collection should be the same across all jobs, so all filtering is done here in the query (e.g., Reaction != 6 for companions). If this becomes untrue at some point, filtering will have to be shifted to the individual jobs or we could add a query string to the call.
			NpcCollection retval = new();
			HashSet<string> nameClash = new(StringComparer.Ordinal);
			var throwNameClash = false;
			var query = "SELECT id, name, gender, difficulty, ppDifficulty, ppClass, reaction FROM uesp_esolog.npc WHERE level != -1 AND reaction != 6";
			foreach (var npcData in Database.RunQuery(Connection, query, row => new NpcData(row)))
			{
				if (!ColourCode.IsMatch(npcData.Name) &&
					!TrailingDigits.IsMatch(npcData.Name) &&
					!ReplacementData.NpcNameSkips.Contains(npcData.Name))
				{
					if (nameClash.Add(npcData.Name))
					{
						retval.Add(npcData);
					}
					else
					{
						Debug.WriteLine($"Warning: an NPC with the name \"{npcData.Name}\" exists more than once in the database!");
						throwNameClash = true;
					}
				}
			}

			return throwNameClash
				? throw new InvalidOperationException("Duplicate NPCs found. Operation aborted! See debug output for specifics.")
				: retval;
		}

		public static IEnumerable<(string Name, int Data)> GetZones()
		{
			var query = "SELECT zoneName, subZoneName, mapName, description, mapType, mapContentType, mapFilterType, isDungeon FROM uesp_esolog.zones";
			foreach (var row in Database.RunQuery(Connection, query))
			{
				var mapName = (string)row["mapName"];
				var subZoneName = (string)row["subZoneName"];
				var zoneName = (string)row["zoneName"];
				var name = subZoneName.Length > 0
						? subZoneName
						: zoneName;
				Debug.WriteLine($"{zoneName}/{subZoneName}/{mapName} - chose {name}");
				yield return (name, (int)row["mapType"]);
			}
		}
		#endregion
	}
}
