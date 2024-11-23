#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

#region Public Enumerations
[Flags]
public enum PagesWithPropertyProperties
{
	None = 0,
	Ids = 1,
	Title = 1 << 1,
	Value = 1 << 2,
	All = Ids | Title | Value
}
#endregion

public class PagesWithPropertyInput(string propertyName) : ILimitableInput, IGeneratorInput
{
	#region Public Properties
	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public PagesWithPropertyProperties Properties { get; set; }

	public string PropertyName { get; } = propertyName;

	public bool SortDescending { get; set; }
	#endregion
}