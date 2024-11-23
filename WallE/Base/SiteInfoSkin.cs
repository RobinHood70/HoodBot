#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class SiteInfoSkin
{
	#region Constructors
	internal SiteInfoSkin(string code, string name, bool unusable)
	{
		this.Code = code;
		this.Name = name;
		this.Unusable = unusable;
	}
	#endregion

	#region Public Properties
	public string Code { get; }

	public string Name { get; }

	public bool Unusable { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}