namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using RobinHood70.WallE.Base;

/// <summary>Specifies the status of an upload operation to MediaWiki.</summary>
public enum UploadStatus
{
	/// <summary>The upload status is unknown. Most likely, this is the result of a stashed upload gone wrong or a new status having been added in a later version of MediaWiki. See ResultText for the original value.</summary>
	Unknown = 0,

	/// <summary>The upload succeeded.</summary>
	Success,

	/// <summary>The upload failed with warnings. The upload may succeed if tried again with <c>ignoreWarnings</c> set to <see langword="true"/>.</summary>
	Warning,

	/// <summary>The upload failed. Retries will also likely fail, regardless of <c>ignoreWarnings</c>.</summary>
	Failure,

	/// <summary>The upload was aborted before being sent to MediaWiki, either because the file could not be found or editing is disabled.</summary>
	Aborted,
}

/// <summary>Represents the result of a file upload operation.</summary>
public sealed class UploadOutput
{
	/// <summary>Initializes a new instance of the <see cref="UploadOutput"/> class.</summary>
	/// <param name="status">The status of the upload.</param>
	/// <param name="fileName">The pagename of the file.</param>
	public UploadOutput(UploadStatus status, string fileName)
	{
		ArgumentNullException.ThrowIfNull(fileName);
		this.Duplicates = [];
		this.FileName = fileName;
		this.Result = status.ToString();
		this.Status = status;
		this.Warnings = ImmutableDictionary<string, string>.Empty;
	}

	/// <summary>Initializes a new instance of the <see cref="UploadOutput"/> class.</summary>
	/// <param name="result">A WallE result object.</param>
	public UploadOutput(UploadResult result)
	{
		ArgumentNullException.ThrowIfNull(result);
		this.Duplicates = result.Duplicates;
		this.FileName = result.FileName;
		this.Result = result.Result;
		this.Warnings = result.Warnings;
		this.Status = this.Result switch
		{
			"Success" => UploadStatus.Success,
			"Warning" => UploadStatus.Warning,
			"Failure" => UploadStatus.Failure,
			_ => UploadStatus.Unknown,
		};
	}

	#region Public Properties

	/// <summary>Gets a read-only list of exact duplicates of this file. The file names do not include the namespace.</summary>
	public IReadOnlyList<string> Duplicates { get; }

	/// <summary>Gets the name of the uploaded file, or <see langword="null"/> if not available.</summary>
	public string? FileName { get; }

	/// <summary>Gets the result of the upload operation as text.</summary>
	public string Result { get; }

	/// <summary>Gets the result of the upload operation as an enumerated value for easy comparison.</summary>
	public UploadStatus Status { get; }

	/// <summary>Gets a read-only collection of any warning messages returned by the upload attempt.</summary>
	/// <remarks>Each warning consists of a language-agnostic key and a language-specific message.</remarks>
	public IReadOnlyDictionary<string, string> Warnings { get; }
	#endregion
}