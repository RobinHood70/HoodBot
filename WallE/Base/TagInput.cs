#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class TagInput
{
	#region Public Properties
	public IEnumerable<string>? Add { get; set; }

	public IEnumerable<long>? LogIds { get; set; }

	public string? Reason { get; set; }

	public IEnumerable<long>? RecentChangesIds { get; set; }

	public IEnumerable<string>? Remove { get; set; }

	public IEnumerable<long>? RevisionIds { get; set; }

	public string? Token { get; set; }
	#endregion
}