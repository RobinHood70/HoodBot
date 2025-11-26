namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
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
		116426,
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
	private static readonly Dictionary<long, string> TitleExceptions = new()
	{
		[203278] = "Apocrypha Tree, Spore (Legendary)",
		[118162] = "Carpet of the Desert Flame, Faded (design 1)",
		[118167] = "Carpet of the Desert Flame, Faded (design 2)",
		[125530] = "Dwarven Pipeline Cap, Sealed (small)",
		[126658] = "Dwarven Pipeline Cap, Sealed (standard)",
		[198055] = "Necrom Funerary Offering, Mushrooms (Bundle)",
		[218012] = "Necrom Funerary Offering, Mushrooms (Planter)",
		[116419] = "Orcish Chair, Peaked (epic)",
		[116392] = "Orcish Chair, Peaked (superior)",
		[197713] = "Tribunal Rug (Necrom)",
	};
	#endregion

	#region Constructors
	public Furnishing(IDataRecord row, Site site, bool collectible)
	{
		this.Collectible = collectible;
		this.Id = collectible ? (long)row["itemId"] : (int)row["itemId"];
		var desc = EsoLog.ConvertEncoding((string)row["description"])
			.Replace(" |cFFFFFF", "\n:", StringComparison.Ordinal)
			.Replace("|r", string.Empty, StringComparison.Ordinal);
		desc = desc.Replace("and and", "{{sic|and and|and}}", StringComparison.Ordinal); // Doing this one separately since it should probably be separated out at some point.
		var sizeMatch = SizeFinder.Match(desc);
		this.Size = sizeMatch.Success ? sizeMatch.Groups["size"].Value.UpperFirst(CultureInfo.CurrentCulture) : null;
		this.Description = sizeMatch.Success && sizeMatch.Index == 0 && sizeMatch.Length == desc.Length
			? null
			: desc;
		var furnCategory = EsoLog.ConvertEncoding((string)row["furnCategory"]);
		if ((string?)row["tags"] is string tags)
		{
			this.Behavior = EsoSpace.TrimBehavior(EsoLog.ConvertEncoding(tags));
		}

		if (collectible)
		{
			this.FurnishingCategory = furnCategory;
			this.FurnishingSubcategory = EsoLog.ConvertEncoding((string)row["furnSubCategory"]);
			this.NickName = EsoLog.ConvertEncoding((string)row["nickname"]);
		}
		else
		{
			this.BindType = GetBindTypeText((int)row["bindType"]);

			if (!string.IsNullOrEmpty(furnCategory))
			{
				var furnSplit = furnCategory.Split(TextArrays.Colon, 2);
				if (furnSplit.Length > 0)
				{
					this.FurnishingCategory = furnSplit[0];
					this.FurnishingSubcategory = furnSplit[1]
						.Split(TextArrays.Parentheses)[0]
						.TrimEnd();
				}
			}

			var quality = EsoLog.ConvertEncoding((string)row["quality"]);
			this.Quality = int.TryParse(quality, NumberStyles.Integer, site.Culture, out var qualityNum)
				? "nfsel".Substring(qualityNum - 1, 1)
				: quality;
			var abilityDesc = EsoLog.ConvertEncoding((string)row["abilityDesc"]);
			this.AddSkillsAndMats(abilityDesc);
		}

		this.FurnishingLimitType = this.GetFurnishingLimitType(row);
		var itemLink = EsoLog.ConvertEncoding((string)row["resultitemLink"]);
		this.ResultItemLink = EsoLog.ExtractItemId(itemLink);
		this.Name = EsoLog.ConvertEncoding((string)row["name"]);
		if (this.Collectible || !TitleExceptions.TryGetValue(this.Id, out var titleName))
		{
			titleName = TitleFactory.SanitizePageName(this.Name, true);
		}

		this.Title = TitleFactory.FromUnvalidated(site[UespNamespaces.Online], titleName);
		this.Disambiguator =
			this.FurnishingCategory.OrdinalICEquals("Mounts") ? "mount" :
			this.FurnishingCategory.OrdinalICEquals("Vanity Pets") ? "pet" :
			this.Collectible ? "collectible" :
			" furnishing";
		this.DisambiguationTitle = TitleFactory.FromUnvalidated(this.Title.Namespace, $"{this.Title.PageName} ({this.Disambiguator})");
	}
	#endregion

	#region Public Static Properties
	public static Dictionary<FurnishingType, string> FurnishingLimitTypes { get; } = new()
	{
		[FurnishingType.None] = string.Empty,
		[FurnishingType.TraditionalFurnishings] = "Traditional Furnishings",
		[FurnishingType.SpecialFurnishings] = "Special Furnishings",
		[FurnishingType.CollectibleFurnishings] = "Collectible Furnishings",
		[FurnishingType.SpecialCollectibles] = "Special Collectibles",
	};

	public static Dictionary<string, string> PageNameExceptions { get; } = new(StringComparer.Ordinal)
	{
		["Dwarven Spider Pet"] = "Dwarven Spider Pet (furnishing)",
		["Frostbane Bear Mount"] = "Frostbane Bear (mount)",
		["Frostbane Bear Pet"] = "Frostbane Bear (pet)",
		["Frostbane Sabre Cat Pet"] = "Frostbane Sabre Cat (pet)",
		["Frostbane Sabre Cat Mount"] = "Frostbane Sabre Cat (mount)",
		["Frostbane Wolf Mount"] = "Frostbane Wolf (mount)",
		["Frostbane Wolf Pet"] = "Frostbane Wolf (pet)",
	};
	#endregion

	#region Public Properties
	public string? Behavior { get; }

	public string? BindType { get; }

	public bool Collectible { get; }

	// Likely to only be accessed once per item, so mapping it to Contains rather than storing a value.
	public bool Deprecated => DeprecatedFurnishings.Contains(this.Id);

	public string? Description { get; }

	public string Disambiguator { get; }

	public Title DisambiguationTitle { get; }

	public string? FurnishingCategory { get; }

	public FurnishingType FurnishingLimitType { get; }

	public string? FurnishingSubcategory { get; }

	public long Id { get; }

	public SortedSet<string> Materials { get; } = new(StringComparer.Ordinal);

	public string Name { get; }

	public string? NickName { get; }

	public string? Quality { get; }

	public string? ResultItemLink { get; }

	public string? Size { get; }

	public SortedSet<string> Skills { get; } = new(StringComparer.Ordinal);

	public Title Title { get; set; } // Settable to deal with conflicts.
	#endregion

	#region Public Static Methods
	public static long GetKey(long keyIn, bool collectible) => collectible ? keyIn << 32 : keyIn;

	public static string IconName(string itemName) => $"ON-icon-furnishing-{itemName.Replace(':', ',')}.png";

	public static string ImageName(string itemName) => $"ON-furnishing-{itemName.Replace(':', ',')}.jpg";
	#endregion

	#region Public Override Methods
	public override string ToString() => $"({this.Id}) {this.Title.PageName}";
	#endregion

	#region Private Static Methods
	private static string? GetBindTypeText(int bindType) => bindType switch
	{
		-1 => null,
		0 => string.Empty,
		1 => "Bind on Pickup",
		2 => "Bind on Equip",
		3 => "Backpack Bind on Pickup",
		_ => throw new InvalidOperationException()
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

	private FurnishingType GetFurnishingLimitType(IDataRecord row)
	{
		var furnishingLimitType = this.Collectible
			? (FurnishingType)(sbyte)row["furnLimitType"]
			: (FurnishingType)row["furnLimitType"];
		if (furnishingLimitType == FurnishingType.None)
		{
			furnishingLimitType = (
				AliveCats.Contains(this.FurnishingCategory!) ||
				AliveCats.Contains(this.FurnishingSubcategory!))
					? this.Collectible
						? FurnishingType.SpecialCollectibles
						: FurnishingType.SpecialFurnishings
					: this.Collectible
						? FurnishingType.CollectibleFurnishings
						: FurnishingType.TraditionalFurnishings;
		}

		return furnishingLimitType;
	}
	#endregion
}