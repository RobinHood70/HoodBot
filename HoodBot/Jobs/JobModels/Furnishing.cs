namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;

#region Internal Enumerations
internal enum FurnishingType
{
	None = -1,
	TraditionalFurnishings,
	SpecialFurnishings,
	CollectibleFurnishings,
	SpecialCollectibles
}
#endregion

internal sealed class Furnishing
{
	#region Static Fields
	private static readonly HashSet<string> AliveCats = new(StringComparer.Ordinal)
	{
		"Amory Assitants",
		"Banking Assistants",
		"Companions",
		"Creatures",
		"Deconstruction Assistants",
		"Houseguests",
		"Merchant Assistants",
		"Mounts",
		"Non-Combat Pets",
		"Statues",
	};

	private static readonly HashSet<string> AllSkills = new(StringComparer.Ordinal)
	{
		"Engraver",
		"Metalworking",
		"Potency Improvement",
		"Provisioning",
		"Recipe Improvement",
		"Solvent Proficiency",
		"Tailoring",
		"Woodworking",
	};

	private static readonly HashSet<long> DeprecatedFurnishings =
	[
		115083,
		119706,
		120853,
		152141,
		152142,
		152143,
		152144,
		152145,
		152146,
		152147,
		152148,
		152149,
		153552,
		153553,
		153554,
		153555,
		153556,
		153557,
		153558,
		153559,
		153560,
		153561,
		153562,
		183198,
		220297,
		220318,
		220288,
		220300,
		220320,
		220323,
	];

	private static readonly Regex IngredientsFinder = new(@"\|cffffffINGREDIENTS\|r\n(?<ingredients>.+)$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
	private static readonly Regex SizeFinder = new(@"This is a (?<size>\w+) house item.", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	private static readonly Dictionary<long, string> NameExceptions = new()
	{
		// Capitalization is still mostly algorithmic. This list contains exceptions to the rules Dave implemented in ESOLog.
		[208358111] = "Handbook for New Homeowners",
	};

	private static readonly Dictionary<long, string> PageNameExceptions = new()
	{
		[203278] = "Apocrypha Tree, Spore (Legendary)",
		[118162] = "Carpet of the Desert Flame, Faded (design 1)",
		[118167] = "Carpet of the Desert Flame, Faded (design 2)",
		[125530] = "Dwarven Pipeline Cap, Sealed (small)",
		[126658] = "Dwarven Pipeline Cap, Sealed (standard)",
		[198055] = "Necrom Funerary Offering, Mushrooms (Bundle)",
		[218012] = "Necrom Funerary Offering, Mushrooms (Planter)",
		[116427] = "Orcish Bookshelf, Peaked",
		[116419] = "Orcish Chair, Peaked (epic)",
		[116392] = "Orcish Chair, Peaked (superior)",
		[197713] = "Tribunal Rug (Necrom)",
	};
	#endregion

	#region Constructors
	public Furnishing(IDataRecord row, HashSet<string>? names)
	{
		var furnField = row["furnLimitType"];

		// Clunky workaround for furnishing having different type than collectible.
		this.FurnishingLimitType = furnField is sbyte tinyVal
			? (FurnishingType)(int)tinyVal
			: (FurnishingType)furnField;
		var furnSplit = EsoLog.ConvertEncoding((string)row["furnCategory"])
			.Split(TextArrays.Colon, 2);
		this.FurnishingCategory = furnSplit[0];
		if (furnSplit.Length == 2)
		{
			this.FurnishingSubcategory = furnSplit[1].Split(TextArrays.Parentheses, StringSplitOptions.TrimEntries)[0];
			if (this.FurnishingLimitType == FurnishingType.None)
			{
				this.FurnishingLimitType = (
					AliveCats.Contains(this.FurnishingCategory) ||
					AliveCats.Contains(this.FurnishingSubcategory))
						? FurnishingType.SpecialFurnishings
						: FurnishingType.TraditionalFurnishings;
			}
		}
		else
		{
			// If you get an error here, it may be because the furnishing furnCategory isn't in the expected format, so it's processing it as a collection item.
			this.FurnishingSubcategory = EsoLog.ConvertEncoding((string)row["furnSubCategory"]);
		}

		this.Id = row["itemId"] is long longVal ? (int)longVal : (int)row["itemId"];
		var desc = EsoLog.ConvertEncoding((string)row["description"])
			.Replace(" |cFFFFFF", "\n:", StringComparison.Ordinal)
			.Replace("|r", string.Empty, StringComparison.Ordinal);
		var sizeMatch = SizeFinder.Match(desc);
		this.Size = sizeMatch.Success ? sizeMatch.Groups["size"].Value.UpperFirst(CultureInfo.CurrentCulture) : null;
		this.Description = sizeMatch.Success && sizeMatch.Index == 0 && sizeMatch.Length == desc.Length
			? null
			: desc;
		if ((string?)row["tags"] is string tags)
		{
			this.Behavior = EsoSpace.TrimBehavior(EsoLog.ConvertEncoding(tags));
		}

		if (this.Collectible)
		{
			this.NickName = EsoLog.ConvertEncoding((string)row["nickname"]);
		}
		else
		{
			this.BindType = GetBindTypeName((int)row["bindType"]);
			var quality = EsoLog.ConvertEncoding((string)row["quality"]);
			this.Quality = GetQualityName(quality);

			var abilityDesc = EsoLog.ConvertEncoding((string)row["abilityDesc"]);
			this.AddSkillsAndMats(abilityDesc);
		}

		var itemLink = EsoLog.ConvertEncoding((string)row["resultitemLink"]);
		this.ResultItemLink = EsoLog.ExtractItemId(itemLink);
		this.Name = NameExceptions.TryGetValue(this.Id, out var correctedName)
			? correctedName
			: RegexLibrary.PruneExcessSpaces(EsoLog.ConvertEncoding((string)row["name"])).Trim();
		if (names?.TryGetValue(this.Name, out var correctedCase) == true)
		{
			if (!this.Name.OrdinalEquals(correctedCase))
			{
				Debug.WriteLine($"Corrected casing for furnishing ID {this.Id} from '{this.Name}' to '{correctedCase}'");
			}

			this.Name = correctedCase;
		}

		this.PageName = !this.Collectible && PageNameExceptions.TryGetValue(this.Id, out var correctedPageName)
			? correctedPageName
			: TitleFactory.SanitizePageName(this.Name, true);
		this.Disambiguator =
			this.FurnishingCategory.OrdinalICEquals("Mounts") ? "mount" :
			this.FurnishingCategory.OrdinalICEquals("Vanity Pets") ? "pet" :
			this.Collectible ? "collectible" :
			"furnishing";
	}
	#endregion

	#region Public Properties
	public string? Behavior { get; }

	public string? BindType { get; }

	public bool Collectible => this.FurnishingLimitType is
		FurnishingType.CollectibleFurnishings or
		FurnishingType.SpecialCollectibles;

	public string? Description { get; }

	public string Disambiguator { get; }

	public string? FurnishingCategory { get; }

	public FurnishingType FurnishingLimitType { get; }

	public string? FurnishingSubcategory { get; }

	public long Id { get; }

	public SortedSet<string> Materials { get; } = new(StringComparer.Ordinal);

	public string Name { get; }

	public string? NickName { get; }

	public string PageName { get; set; } // Settable to deal with conflicts.

	public string? Quality { get; }

	public string? ResultItemLink { get; }

	public string? Size { get; }

	public SortedSet<string> Skills { get; } = new(StringComparer.Ordinal);
	#endregion

	#region Public Static Methods
	public static string? GetFurnishingLimitType(FurnishingType furnishingLimitType) => furnishingLimitType switch
	{
		FurnishingType.None => string.Empty,
		FurnishingType.TraditionalFurnishings => "Traditional Furnishings",
		FurnishingType.SpecialFurnishings => "Special Furnishings",
		FurnishingType.CollectibleFurnishings => "Collectible Furnishings",
		FurnishingType.SpecialCollectibles => "Special Collectibles",
		_ => null
	};

	public static long GetKey(long keyIn, bool collectible) => collectible ? keyIn << 32 : keyIn;

	public static string IconName(string itemName) => $"ON-icon-furnishing-{itemName.Replace(':', ',')}.png";

	public static string ImageName(string itemName) => $"ON-furnishing-{itemName.Replace(':', ',')}.jpg";
	#endregion

	#region Public Methods
	public bool IsValid() =>
		!DeprecatedFurnishings.Contains(this.Id) &&
		!this.Name.Contains(" Station (", StringComparison.Ordinal);
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name.OrdinalEquals(this.PageName)
		? $"({this.Id}) {this.Name}"
		: $"({this.Id}) {this.Name} => Online:{this.PageName}";
	#endregion

	#region Private Static Methods
	private static string? GetBindTypeName(int bindType) => bindType switch
	{
		0 => string.Empty,
		1 => "Bind on Pickup",
		2 => "Bind on Equip",
		3 => "Backpack Bind on Pickup",
		_ => null,
	};

	private static string? GetQualityName(string quality) => quality switch
	{
		"1-5" => "Any",
		"1" => "Normal",
		"2" => "Fine",
		"3" => "Superior",
		"4" => "Epic",
		"5" => "Legendary",
		"6" => "Mythic",
		_ => null,
	};
	#endregion

	#region Private Methods
	private void AddSkillsAndMats(string abilityDesc)
	{
		var ingrMatch = IngredientsFinder.Match(abilityDesc);
		if (ingrMatch.Success)
		{
			var ingredientList = ingrMatch.Groups["ingredients"].Value;
			var entries = ingredientList.Split(", ", StringSplitOptions.None);
			foreach (var entry in entries)
			{
				var ingSplit = entry.Split(" (", 2, StringSplitOptions.None);
				var count = ingSplit.Length == 2
					? ingSplit[1]
					: "1";
				var ingredient = ingSplit[0];
				var addAs = $"{ingredient} ({count})";
				if (AllSkills.Contains(ingredient))
				{
					this.Skills.Add(addAs);
				}
				else
				{
					this.Materials.Add(addAs);
				}
			}
		}
	}
	#endregion
}