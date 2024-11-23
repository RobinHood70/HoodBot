#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class AllImagesItem : ImageInfoItem, IApiTitle
{
	#region Constructors
	internal AllImagesItem(int ns, string title, string name)
	{
		this.Namespace = ns;
		this.Title = title;
		this.Name = name;
	}
	#endregion

	#region Public Properties
	public string? DescriptionUrl { get; internal set; }

	public string Name { get; internal set; }

	public int Namespace { get; }

	public string Title { get; }

	public string? Url { get; internal set; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}