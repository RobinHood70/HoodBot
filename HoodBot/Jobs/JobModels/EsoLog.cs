namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;

internal static class EsoLog
{
	#region Fields
	private static readonly Regex TrailingDigits = new(@"\s*\d+\Z", RegexOptions.None, Globals.DefaultRegexTimeout);
	private static readonly Regex UpdateFinder = new(@"\d+(pts)?\Z", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	private static readonly Dictionary<string, EsoVersion[]> LatestVersions = new(StringComparer.Ordinal);
	#endregion

	#region Public Properties
	public static Regex BonusFinder { get; } = new(@"\s*Current [Bb]onus:.*?(\.|$)", RegexOptions.Multiline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

	public static Regex ColourCode { get; } = new(@"\|c[0-9a-f]{6}\|?(?<content>[^\|]*?)\|r", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);

	public static Regex FloatFinder { get; } = new Regex(@"([\d\.]+)", RegexOptions.None, Globals.DefaultRegexTimeout);

	public static string Connection { get; } = App.GetConnectionString("EsoLog") ?? throw new InvalidOperationException();

	public static Database EsoDb => field ??= new Database(Connection);

	public static Dictionary<CoefficientTypes, string> MechanicNames { get; } = new Dictionary<CoefficientTypes, string>
	{
		[CoefficientTypes.Invalid] = "Invalid",
		[CoefficientTypes.Magicka] = "Magicka",
		[CoefficientTypes.Werewolf] = "Werewolf",
		[CoefficientTypes.Stamina] = "Stamina",
		[CoefficientTypes.Ultimate] = "Ultimate",
		[CoefficientTypes.MountStamina] = "Mount Stamina",
		[CoefficientTypes.Health] = "Health",
		[CoefficientTypes.Daedric] = "Daedric",

		[CoefficientTypes.HealthOld] = "Health",
		[CoefficientTypes.MagickaOld] = "Magicka",
		[CoefficientTypes.StaminaOld] = "Stamina",
		[CoefficientTypes.UltimateOld] = "Ultimate",
		[CoefficientTypes.SoulTether] = "Ultimate (no weapon damage)",
		[CoefficientTypes.LightArmor] = "Light Armor #",
		[CoefficientTypes.MediumArmor] = "Medium Armor #",
		[CoefficientTypes.HeavyArmor] = "Heavy Armor #",
		[CoefficientTypes.WeaponDagger] = "Dagger #",
		[CoefficientTypes.ArmorType] = "Armor Type #",
		[CoefficientTypes.Damage] = "Spell + Weapon Damage",
		[CoefficientTypes.Assassination] = "Assassination Skills Slotted",
		[CoefficientTypes.FightersGuild] = "Fighters Guild Skills Slotted",
		[CoefficientTypes.DraconicPower] = "Draconic Power Skills Slotted",
		[CoefficientTypes.Shadow] = "Shadow Skills Slotted",
		[CoefficientTypes.Siphoning] = "Siphoning Skills Slotted",
		[CoefficientTypes.Sorcerer] = "Sorcerer Skills Slotted",
		[CoefficientTypes.MagesGuild] = "Mages Guild Skills Slotted",
		[CoefficientTypes.Support] = "Support Skills Slotted",
		[CoefficientTypes.AnimalCompanion] = "Animal Companion Skills Slotted",
		[CoefficientTypes.GreenBalance] = "Green Balance Skills Slotted",
		[CoefficientTypes.WintersEmbrace] = "Winter's Embrace Slotted",
		[CoefficientTypes.MagicHealthCapped] = "Magicka with Health Cap",
		[CoefficientTypes.BoneTyrant] = "Bone Tyrant Slotted",
		[CoefficientTypes.GraveLord] = "Grave Lord Slotted",
		[CoefficientTypes.SpellDamageCapped] = "Spell Damage Capped",
		[CoefficientTypes.MagickaWeaponDamage] = "Magicka and Weapon Damage",
		[CoefficientTypes.MagickaSpellDamageCapped] = "Magicka and Spell Damage",
		[CoefficientTypes.WeaponPower] = "Weapon Power",
		[CoefficientTypes.ConstantValue] = "Constant Value",
		[CoefficientTypes.HealthOrSpellDamage] = "Health or Spell Damage",
		[CoefficientTypes.Resistance] = "Max Resistance",
		[CoefficientTypes.MagickaLightArmor] = "Magicka and Light Armor (Health Capped)",
		[CoefficientTypes.HealthOrDamage] = "Health or Weapon/Spell Damage",
		[CoefficientTypes.HealthOrMagickaCapped] = "Health or Magicka"
	};

	public static EsoVersion LatestDBUpdate(string prefix, bool includePts)
	{
		if (!LatestVersions.TryGetValue(prefix, out var prefixedLatest))
		{
			prefixedLatest = [EsoVersion.Empty, EsoVersion.Empty];
			foreach (var table in EsoDb.ShowTables(prefix))
			{
				var match = UpdateFinder.Match(table);
				if (match.Success)
				{
					var version = new EsoVersion(match.Value);
					var ptsNum = version.Pts ? 1 : 0;

					// Invalid values return Empty, which is the default/minimum, so no need to check for it explicitly.
					if (version.Version > prefixedLatest[ptsNum].Version)
					{
						prefixedLatest[ptsNum] = version;
					}
				}
			}

			LatestVersions.Add(prefix, prefixedLatest);
		}

		return includePts && prefixedLatest[1] > prefixedLatest[0]
			? prefixedLatest[1]
			: prefixedLatest[0];
	}
	#endregion

	#region Public Methods
	public static string ConvertEncoding(string text)
	{
		var fromEncoding = Encoding.GetEncoding(1252) ?? throw new InvalidOperationException();
		var toEncoding = Encoding.UTF8;
		var bgBytes = fromEncoding.GetBytes(text);
		return toEncoding.GetString(bgBytes);
	}

	public static string? ExtractItemId(string itemLinkText)
	{
		var itemLinkOffset1 = itemLinkText.IndexOf(":item:", StringComparison.Ordinal) + 6;
		return itemLinkOffset1 != 5 &&
			itemLinkText.IndexOf(':', itemLinkOffset1) is var itemLinkOffset2 &&
			itemLinkOffset2 != -1
				? itemLinkText[itemLinkOffset1..itemLinkOffset2]
				: null;
	}

	public static IEnumerable<NpcLocationData> GetNpcLocations(List<long> npcIds)
	{
		List<NpcLocationData> retval = [];
		var query = $"SELECT npcId, zone, locCount FROM npcLocations WHERE npcId IN ({string.Join(", ", npcIds)}) AND zone != 'Tamriel'";
		for (var retries = 2; retries >= 0; retries--)
		{
			try
			{
				foreach (var location in Database.RunQuery(Connection, query, NpcLocationDataFromRow))
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
		NpcCollection retval = [];
		HashSet<string> nameClash = new(StringComparer.Ordinal);
		var query = "SELECT id, name, gender, difficulty, ppDifficulty, ppClass, reaction FROM uesp_esolog.npc WHERE level != -1 AND reaction != 6";
		foreach (var npcData in Database.RunQuery(Connection, query, NpcDataFromRow))
		{
			if ((npcData.Id >= 322827 && npcData.Id <= 322924) || // Remove Polish names
				ReplacementData.NpcIdSkips.Contains(npcData.Id) ||
				ReplacementData.NpcNameSkips.Contains(npcData.DataName) ||
				ColourCode.IsMatch(npcData.DataName) ||
				TrailingDigits.IsMatch(npcData.DataName))
			{
				continue;
			}

			if (nameClash.Add(npcData.DataName))
			{
				retval.Add(npcData);
			}
			else
			{
				Debug.WriteLine($"Warning: an NPC with the name \"{npcData.DataName}\" exists more than once in the database!");
			}
		}

		return retval;
	}

	public static string GetRankDescription(long id, IDataRecord row)
	{
		if ((string)row["rawDescription"] is var description && description.Length == 0)
		{
			description = (string)row["description"];
		}

		var descHeader = ColourCode.Replace(ConvertEncoding((string)row["descHeader"]), "${content}");
		description = ConvertEncoding(description).Trim();
		if (ReplacementData.IdPartialReplacements.TryGetValue(id, out var partial))
		{
			description = description.Replace(partial.From, partial.To, StringComparison.Ordinal);
		}

		description = ColourCode.Replace(description, "'''${content}'''");
		description = BonusFinder.Replace(description, string.Empty);
		if (descHeader.Length > 0)
		{
			description = $"'''{descHeader}''' " + description;
		}

		return RegexLibrary.PruneExcessWhitespace(description).Trim();
	}

	public static IEnumerable<(string Name, int Data)> GetZones()
	{
		var query = "SELECT zoneName, subZoneName, mapName, description, mapType, mapContentType, mapFilterType, isDungeon FROM uesp_esolog.zones";
		foreach (var row in Database.RunQuery(Connection, query))
		{
			var mapName = ConvertEncoding((string)row["mapName"]);
			var subZoneName = ConvertEncoding((string)row["subZoneName"]);
			var zoneName = ConvertEncoding((string)row["zoneName"]);
			var name = subZoneName.Length > 0
					? subZoneName
					: zoneName;
			Debug.WriteLine($"{zoneName}/{subZoneName}/{mapName} - chose {name}");
			yield return (name, (int)row["mapType"]);
		}
	}
	#endregion

	#region Private Methods
	private static NpcData NpcDataFromRow(IDataRecord row)
	{
		var dataName = ConvertEncoding((string)row["name"]).Trim();
		var gender = (Gender)(sbyte)row["gender"];
		if (gender == Gender.None && dataName.Length > 2 && dataName[^2] == '^')
		{
			var genderChar = char.ToUpperInvariant(dataName[^1]);
			dataName = dataName[0..^2];
			gender = genderChar switch
			{
				'M' => Gender.Male,
				'F' => Gender.Female,
				'N' => Gender.NotApplicable,
				_ => Gender.None
			};
		}

		if (!ReplacementData.NpcNameFixes.TryGetValue(dataName, out var name))
		{
			name = dataName;
		}

		var lootType = ConvertEncoding((string)row["ppClass"]);
		var dataReaction = (sbyte)row["reaction"];
		var reaction = dataReaction == -1
			? lootType switch
			{
				"Bard" => "Friendly",
				"" => string.Empty,
				_ => "Justice Neutral"
			}
			: NpcData.Reactions[dataReaction];

		return new NpcData(
			dataName: dataName,
			difficulty: (sbyte)((sbyte)row["difficulty"] - 1),
			gender: gender,
			id: (long)row["id"],
			lootType: lootType,
			name: name,
			pickpocketDifficulty: (PickpocketDifficulty)(sbyte)row["ppDifficulty"],
			reaction: reaction);
	}

	private static NpcLocationData NpcLocationDataFromRow(IDataRecord row) => new(
		id: (long)row["npcId"],
		zone: ConvertEncoding((string)row["zone"])
			.Replace(" (Normal)", string.Empty, StringComparison.Ordinal)
			.Replace(" (Veteran)", string.Empty, StringComparison.Ordinal),
		locCount: (int)row["locCount"]);
	#endregion
}