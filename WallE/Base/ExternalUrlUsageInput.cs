#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

#region Public Enumerations
[Flags]
public enum ExtUrlUsageProperties
{
	None = 0,
	Ids = 1,
	Title = 1 << 1,
	Url = 1 << 2,
	All = Ids | Title | Url
}
#endregion

public class ExternalUrlUsageInput : ILimitableInput, IGeneratorInput
{
	#region Public Properties
	public int Limit { get; set; }

	public bool ExpandUrl { get; set; }

	public int MaxItems { get; set; }

	public IEnumerable<int>? Namespaces { get; set; }

	public ExtUrlUsageProperties Properties { get; set; }

	public string? Protocol { get; set; }

	public string? Query { get; set; }
	#endregion
}