#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RobinHood70.CommonCode;

public class UploadResult
{
	#region Constructors
	internal UploadResult(string result, IReadOnlyList<string> duplicates, IReadOnlyList<DateTime> duplicateVersions, string? fileKey, string? fileName, ImageInfoItem? imageInfo, ReadOnlyDictionary<string, string> warnings)
	{
		this.Result = result;
		this.Duplicates = duplicates;
		this.DuplicateVersions = duplicateVersions;
		this.FileKey = fileKey;
		this.FileName = fileName;
		this.ImageInfo = imageInfo;
		this.Warnings = warnings;
	}
	#endregion

	#region Public Properties
	public IReadOnlyList<string> Duplicates { get; }

	public IReadOnlyList<DateTime> DuplicateVersions { get; }

	public string? FileKey { get; }

	public string? FileName { get; }

	public ImageInfoItem? ImageInfo { get; }

	public string Result { get; }

	public IReadOnlyDictionary<string, string> Warnings { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.FileName ?? Globals.Unknown;
	#endregion
}