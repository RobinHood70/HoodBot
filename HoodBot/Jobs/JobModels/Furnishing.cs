namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

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
	#endregion

	#region Constructors
	public Furnishing(long id, string? abilityDesc, string? behavior, string? bindType, string? description, string furnishingCategory, string? furnishingSubcategory, FurnishingType furnishingLimitType, string name, string? nickName, string pageName, string? quality, string? resultItemLink, string? size)
	{
		this.Behavior = behavior;
		this.BindType = bindType;
		this.Description = description;
		this.FurnishingCategory = furnishingCategory;
		this.FurnishingLimitType = furnishingLimitType;
		this.FurnishingSubcategory = furnishingSubcategory;
		this.Id = id;
		this.Name = name;
		this.NickName = nickName;
		this.PageName = pageName;
		this.Quality = quality;
		this.ResultItemLink = resultItemLink;
		this.Size = size;
		if (abilityDesc is not null)
		{
			this.AddSkillsAndMats(abilityDesc);
		}

		this.Disambiguator =
			furnishingCategory.OrdinalICEquals("Mounts") ? "mount" :
			furnishingCategory.OrdinalICEquals("Vanity Pets") ? "pet" :
			this.Collectible ? "collectible" :
			"furnishing";
	}
	#endregion

	#region Public Properties
	public string? Behavior { get; }

	public string? BindType { get; }

	public bool Collectible => this.FurnishingLimitType == FurnishingType.CollectibleFurnishings;

	public string? Description { get; }

	public string Disambiguator { get; }

	public string? FurnishingCategory { get; }

	public FurnishingType FurnishingLimitType { get; }

	public string? FurnishingSubcategory { get; }

	public long Id { get; }

	public SortedSet<string> Materials { get; } = new(StringComparer.Ordinal);

	public string Name { get; }

	public string? NickName { get; }

	public string PageName { get; set; }

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

	#region Private Methods
	public void AddSkillsAndMats(string abilityDesc)
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