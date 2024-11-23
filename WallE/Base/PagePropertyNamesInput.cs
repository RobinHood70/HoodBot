#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class PagePropertyNamesInput : ILimitableInput
{
	#region Public Properties
	public int Limit { get; set; }

	public int MaxItems { get; set; }
	#endregion
}