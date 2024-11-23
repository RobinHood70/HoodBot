#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class ManageTagsResult
{
	#region Constructors
	internal ManageTagsResult(string operation, string tag, bool success, IReadOnlyList<string> warnings, long logId)
	{
		this.Operation = operation;
		this.Tag = tag;
		this.Success = success;
		this.Warnings = warnings;
		this.LogId = logId;
	}
	#endregion

	#region Public Properties
	public long LogId { get; }

	public string Operation { get; }

	public bool Success { get; }

	public string Tag { get; }

	public IReadOnlyList<string> Warnings { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Tag;
	#endregion
}