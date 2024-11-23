#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class ErrorItem
{
	#region Constructors
	internal ErrorItem(string code, string info)
	{
		this.Code = code;
		this.Info = info;
	}
	#endregion

	#region Public Properties
	public string Code { get; }

	public string Info { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Code;
	#endregion
}