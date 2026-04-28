namespace RobinHood70.HoodBot.Jobs.JobModels;

using System.Collections.Generic;
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

internal sealed class Furnishing(long id, string? behavior, string? bindType, string? description, string furnishingCategory, string furnishingSubcategory, FurnishingType furnishingLimitType, IReadOnlyList<string> materials, string name, string? nickName, string pageName, string? quality, string? resultItemLink, string? size, IReadOnlyList<string> skills)
{
	#region Public Properties
	public string? Behavior { get; } = behavior;

	public string? BindType { get; } = bindType;

	public bool Collectible => this.FurnishingLimitType == FurnishingType.CollectibleFurnishings;

	public string? Description { get; } = description;

	public string Disambiguator { get; } =
		furnishingCategory.OrdinalICEquals("Mounts") ? "mount" :
		furnishingCategory.OrdinalICEquals("Vanity Pets") ? "pet" :
		furnishingLimitType == FurnishingType.CollectibleFurnishings ? "collectible" :
		"furnishing";

	public string FurnishingCategory { get; } = furnishingCategory;

	public FurnishingType FurnishingLimitType { get; } = furnishingLimitType;

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

	#region Public Override Methods
	public override string ToString() => this.Name.OrdinalEquals(this.PageName)
		? $"({this.Id}) {this.Name}"
		: $"({this.Id}) {this.Name} => Online:{this.PageName}";
	#endregion
}