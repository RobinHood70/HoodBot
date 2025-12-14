namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;

// Split off into a separate class to reduce the size and complexity of other classes.
internal static class FurnishingFactory
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

	private static readonly Regex IngredientsFinder = new(@"\|cffffffINGREDIENTS\|r\n(?<ingredients>.*)\|", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);

	private static readonly Dictionary<long, string> NameExceptions = new()
	{
		// Capitalization is still mostly algorithmic. This list contains exceptions to the rules Dave implemented in ESOLog.
		[208358] = "Handbook for New Homeowners",
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

	private static readonly Regex SizeFinder = new(@"This is a (?<size>\w+) house item.", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

	private static readonly Regex SkillsFinder = new(@"\|cffffffTO CREATE\|r\n(?<skills>.*)$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
	#endregion

	#region Public Methods
	public static Furnishing FromCollectibleRow(IDataRecord row)
	{
		var id = (long)row["id"];
		var name = NameExceptions.TryGetValue(id, out var correctedName)
			? correctedName
			: RegexLibrary.PruneExcessSpaces(EsoLog.ConvertEncoding((string)row["name"])).Trim();
		var desc = EsoLog.ConvertEncoding((string)row["description"]);
		var size = SizeFromDesc(ref desc);

		return new Furnishing(
			id: id,
			behavior: EsoLog.ConvertEncoding((string)row["tags"]),
			bindType: null,
			description: desc,
			furnishingLimitType: FurnishingType.CollectibleFurnishings,
			furnishingCategory: EsoLog.ConvertEncoding((string)row["furnCategory"]),
			furnishingSubcategory: EsoLog.ConvertEncoding((string)row["furnSubCategory"]),
			materials: [],
			name: name,
			nickName: EsoLog.ConvertEncoding((string)row["nickname"]),
			pageName: TitleFactory.SanitizePageName(name, true),
			quality: null,
			resultItemLink: null,
			size: size,
			skills: []);
	}

	public static Furnishing FromRow(IDataRecord row)
	{
		var furnSplit = EsoLog.ConvertEncoding((string)row["furnCategory"]).Split(TextArrays.Colon, 2);
		var furnishingCategory = furnSplit[0];
		if (furnSplit.Length != 2)
		{
			throw new InvalidOperationException("Furnishing category missing subcategory.");
		}

		var furnishingSubcategory = furnSplit[1].Split(TextArrays.Parentheses, StringSplitOptions.TrimEntries)[0];
		var furnishingLimitType = (FurnishingType)(int)row["furnLimitType"];
		if (furnishingLimitType == FurnishingType.None)
		{
			furnishingLimitType = AliveCats.Overlaps([furnishingCategory, furnishingSubcategory])
				? FurnishingType.SpecialFurnishings
				: FurnishingType.TraditionalFurnishings;
		}

		var id = (int)row["itemId"];
		var desc = EsoLog.ConvertEncoding((string)row["description"])
			.Replace(" |cFFFFFF", "\n:", StringComparison.Ordinal)
			.Replace("|r", string.Empty, StringComparison.Ordinal);
		var size = SizeFromDesc(ref desc);
		var name = NameExceptions.TryGetValue(id, out var correctedName)
			? correctedName
			: RegexLibrary.PruneExcessSpaces(EsoLog.ConvertEncoding((string)row["name"])).Trim();

		// Strictly for recipes.
		var abilityDesc = EsoLog.ConvertEncoding((string)row["abilityDesc"]);
		var materials = GetMaterials(abilityDesc);
		var skills = GetSkills(abilityDesc);

		return new Furnishing(
			id: id,
			behavior: EsoLog.ConvertEncoding((string)row["tags"]),
			bindType: GetBindTypeName((int)row["bindType"]),
			description: desc,
			furnishingLimitType: furnishingLimitType,
			furnishingCategory: furnishingCategory,
			furnishingSubcategory: furnishingSubcategory,
			materials: materials,
			name: name,
			nickName: null,
			pageName: PageNameExceptions.GetValueOrDefault(id, TitleFactory.SanitizePageName(name, true)),
			quality: GetQualityName(EsoLog.ConvertEncoding((string)row["quality"])),
			resultItemLink: EsoLog.ExtractItemId(EsoLog.ConvertEncoding((string)row["resultitemLink"])),
			size: size,
			skills: skills);
	}
	#endregion

	#region Private Methods

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

	// Note: nullifies desc if it's only the standard size phrasing.
	private static string? SizeFromDesc([DisallowNull] ref string? desc)
	{
		var sizeMatch = SizeFinder.Match(desc);
		if (!sizeMatch.Success)
		{
			return null;
		}

		var size = sizeMatch.Groups["size"].Value.UpperFirst(CultureInfo.CurrentCulture);
		if (sizeMatch.Index == 0 && sizeMatch.Length == desc.Length)
		{
			desc = null;
		}

		return size;
	}

	private static List<string> GetMaterials(string? abilityDesc)
	{
		var retval = new List<string>();
		abilityDesc ??= string.Empty;
		var ingrMatch = IngredientsFinder.Match(abilityDesc);
		if (ingrMatch.Success)
		{
			retval.AddRange(ingrMatch.Groups["ingredients"].Value.Split(", ", StringSplitOptions.TrimEntries));
			retval.Sort(StringComparer.Ordinal);
		}

		return retval;
	}

	private static List<string> GetSkills(string? abilityDesc)
	{
		var retval = new List<string>();
		abilityDesc ??= string.Empty;
		var skillMatch = SkillsFinder.Match(abilityDesc);
		if (skillMatch.Success)
		{
			var skills = EsoLog.ColourCode.Replace(skillMatch.Groups["skills"].Value, "${content}");
			retval.AddRange(skills.Split(TextArrays.NewLineChars, StringSplitOptions.TrimEntries));
			retval.Sort(StringComparer.Ordinal);
		}

		return retval;
	}
	#endregion
}