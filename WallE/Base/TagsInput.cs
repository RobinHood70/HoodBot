#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

#region Public Enumerations
[Flags]
public enum TagProperties
{
	None = 0,
	Name = 1,
	DisplayName = 1 << 1,
	Description = 1 << 2,
	HitCount = 1 << 3,
	All = Name | DisplayName | Description | HitCount
}
#endregion

public class TagsInput : ILimitableInput
{
	#region Public Properties
	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public TagProperties Properties { get; set; }
	#endregion
}