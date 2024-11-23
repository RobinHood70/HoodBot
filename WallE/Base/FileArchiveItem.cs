#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class FileArchiveItem : ImageInfoItem, IApiTitleOptional
{
	#region Constructors
	// MW 1.17: only name is guaranteed; MW 1.18+: namespace and title are guaranteed.
	internal FileArchiveItem(string name, long fileArchiveId, int? ns, string? title)
	{
		this.Name = name;
		this.FileArchiveId = fileArchiveId;
		this.Namespace = ns;
		this.Title = title;
	}
	#endregion

	#region Public Properties
	public string Name { get; }

	public int? Namespace { get; }

	public long FileArchiveId { get; }

	public string? Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}