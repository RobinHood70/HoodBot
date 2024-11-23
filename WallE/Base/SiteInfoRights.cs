#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.CommonCode;

public class SiteInfoRights
{
	#region Constructors
	internal SiteInfoRights(string? text, string? url)
	{
		this.Text = text;
		this.Url = url;
	}
	#endregion

	#region Public Properties
	public string? Text { get; }

	public string? Url { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Text.Ellipsis(30) ?? Globals.Unknown;
	#endregion
}