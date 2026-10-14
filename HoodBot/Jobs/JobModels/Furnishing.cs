namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;

#region Public Enumerations
public enum FurnishingType
{
	None = -1,
	TraditionalFurnishings,
	SpecialFurnishings,
	CollectibleFurnishings,
	SpecialCollectibles
}
#endregion

public sealed class Furnishing(long id, string? behavior, string? bindType, string? description, string furnishingCategory, string furnishingSubcategory, FurnishingType furnishingLimitType, IReadOnlyList<string> materials, string name, string? nickName, string pageName, string? quality, string? resultItemLink, string? size, IReadOnlyList<string> skills)
{
	#region Static Fields
	private static readonly Dictionary<string, FurnishingType> FurnishingTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
	{
		["specialc"] = FurnishingType.SpecialCollectibles,
		["special collectible"] = FurnishingType.SpecialCollectibles,
		["special collectibles"] = FurnishingType.SpecialCollectibles,
		["collectible"] = FurnishingType.CollectibleFurnishings,
		["collectible furnishing"] = FurnishingType.CollectibleFurnishings,
		["collectible furnishings"] = FurnishingType.CollectibleFurnishings,
		["special"] = FurnishingType.SpecialFurnishings,
		["specialf"] = FurnishingType.SpecialFurnishings,
		["special furnishing"] = FurnishingType.SpecialFurnishings,
		["special furnishings"] = FurnishingType.SpecialFurnishings,
		["traditional"] = FurnishingType.TraditionalFurnishings,
		["traditional furnishing"] = FurnishingType.TraditionalFurnishings,
		["traditional furnishings"] = FurnishingType.TraditionalFurnishings
	};
	#endregion

	#region Public Properties
	public string? Behavior { get; } = behavior;

	public string? BindType { get; } = bindType;

	public bool Collectible => this.FurnishingType == FurnishingType.CollectibleFurnishings;

	public string? Description { get; } = description;

	public string Disambiguator { get; } =
		furnishingCategory.OrdinalICEquals("Mounts") ? "mount" :
		furnishingCategory.OrdinalICEquals("Vanity Pets") ? "pet" :
		furnishingLimitType == FurnishingType.CollectibleFurnishings ? "collectible" :
		"furnishing";

	public string FurnishingCategory { get; } = furnishingCategory;

	public FurnishingType FurnishingType { get; } = furnishingLimitType;

	public string FurnishingSubcategory { get; } = furnishingSubcategory;

	public long Id { get; } = id;

	public IReadOnlyList<string> Materials { get; } = materials;

	public string Name { get; } = name;

	public string? NickName { get; } = nickName;

	public string PageName { get; set; } = pageName;

	public string? Quality { get; } = quality;

	public string? ResultItemLink { get; } = resultItemLink;

	public string? Size { get; } = size;

	public IReadOnlyList<string> Skills { get; } = skills;
	#endregion

	#region Public Static Methods
	public static string? GetFurnishingType(FurnishingType furnishingType) => furnishingType switch
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

	public static FurnishingType ParseFurnishingType(string? text) => text is not null && FurnishingTypeSynonyms.TryGetValue(text, out var result)
		? result
		: FurnishingType.None;
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name.OrdinalEquals(this.PageName)
		? $"({this.Id}) {this.Name}"
		: $"({this.Id}) {this.Name} => Online:{this.PageName}";
	#endregion
}