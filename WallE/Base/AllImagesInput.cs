#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using RobinHood70.CommonCode;

#region Public Enumerations
public enum AllImagesSort
{
	Default,
	Name,
	Timestamp
}
#endregion

public class AllImagesInput : ILimitableInput, IGeneratorInput
{
	#region Public Properties
	public DateTime? End { get; set; }

	public Filter FilterBots { get; set; }

	public string? From { get; set; }

	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public int MaximumSize { get; set; } = -1;

	public string? MimeType { get; set; }

	public int MinimumSize { get; set; } = -1;

	public string? Prefix { get; set; }

	public ImageProperties Properties { get; set; }

	public string? Sha1 { get; set; }

	public AllImagesSort SortBy { get; set; }

	public bool SortDescending { get; set; }

	public DateTime? Start { get; set; }

	public string? To { get; set; }

	public string? User { get; set; }
	#endregion
}