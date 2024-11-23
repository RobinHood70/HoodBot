#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

#region Public Enumerations
[Flags]
public enum FileArchiveProperties
{
	None = 0,
	Sha1 = 1,
	Timestamp = 1 << 1,
	User = 1 << 2,
	Size = 1 << 3,
	Dimensions = 1 << 4,
	Description = 1 << 5,
	ParsedDescription = 1 << 6,
	Mime = 1 << 7,
	MediaType = 1 << 8,
	Metadata = 1 << 9,
	BitDepth = 1 << 10,
	ArchiveName = 1 << 11,
	All = Sha1 | Timestamp | User | Size | Dimensions | Description | ParsedDescription | Mime | MediaType | Metadata | BitDepth | ArchiveName
}
#endregion

public class FileArchiveInput : ILimitableInput
{
	#region Public Properties
	public string? From { get; set; }

	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public string? Prefix { get; set; }

	public FileArchiveProperties Properties { get; set; }

	public string? Sha1 { get; set; }

	public bool SortDescending { get; set; }

	public string? To { get; set; }
	#endregion
}