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

internal sealed class Furnishing
{
	#region Constructors
	public Furnishing(long id, string? behavior, string? bindType, string? description, string furnishingCategory, string furnishingSubcategory, FurnishingType furnishingLimitType, IReadOnlyList<string> materials, string name, string? nickName, string pageName, string? quality, string? resultItemLink, string? size, IReadOnlyList<string> skills)
	{
		this.Behavior = behavior;
		this.BindType = bindType;
		this.Description = description;
		this.Disambiguator =
		furnishingCategory.OrdinalICEquals("Mounts") ? "mount" :
		furnishingCategory.OrdinalICEquals("Vanity Pets") ? "pet" :
		furnishingLimitType == FurnishingType.CollectibleFurnishings ? "collectible" :
		"furnishing";
		this.FurnishingCategory = furnishingCategory;
		this.FurnishingLimitType = furnishingLimitType;
		this.FurnishingSubcategory = furnishingSubcategory;
		this.Id = id;
		this.Materials = materials;
		this.Name = name;
		this.NickName = nickName;
		this.PageName = pageName;
		this.Quality = quality;
		this.ResultItemLink = resultItemLink;
		this.Size = size;
		this.Skills = skills;
	}
	#endregion

	#region Public Properties
	public string? Behavior { get; }

	public string? BindType { get; }

	public bool Collectible => this.FurnishingLimitType == FurnishingType.CollectibleFurnishings;

	public string? Description { get; }

	public string Disambiguator { get; }

	public string FurnishingCategory { get; }

	public FurnishingType FurnishingLimitType { get; }

	public string FurnishingSubcategory { get; }

	public long Id { get; }

	public IReadOnlyList<string> Materials { get; }

	public string Name { get; }

	public string? NickName { get; }

	public string PageName { get; set; }

	public string? Quality { get; }

	public string? ResultItemLink { get; }

	public string? Size { get; }

	public IReadOnlyList<string> Skills { get; }
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