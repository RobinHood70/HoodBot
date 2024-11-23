#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class ExternalLinksInput : IPropertyInput, ILimitableInput
{
	#region Public Properties
	public bool ExpandUrl { get; set; }

	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public string? Protocol { get; set; }

	public string? Query { get; set; }
	#endregion
}