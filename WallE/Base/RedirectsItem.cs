#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class RedirectsItem(int? ns, string? title, long pageId, string? fragment) : IApiTitleOptional
{
	#region Public Properties
	public string? Fragment => fragment;

	public int? Namespace => ns;

	public long PageId => pageId;

	public string? Title => title;
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title ?? FallbackText.NoTitle;
	#endregion
}