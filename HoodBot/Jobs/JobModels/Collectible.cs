namespace RobinHood70.HoodBot.Jobs.JobModels;

using System.Collections.Generic;
using RobinHood70.WikiCommon.Parser;

internal sealed class Collectible(long id, string name, string nickName, string description, string collectibleType, string subCategory, string imageName, string iconName)
{
	#region Public Properties
	public string CollectibleType { get; } = collectibleType;

	public List<string> Crates { get; } = [];

	public string Description { get; } = description;

	public string IconName { get; } = iconName;

	public long Id { get; } = id;

	public string ImageName { get; } = imageName;

	public string Name { get; } = name;

	public IList<IWikiNode>? NewContent { get; private set; }

	public string NickName { get; } = nickName;

	public string SubCategory { get; } = subCategory;

	public string? Tier { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}