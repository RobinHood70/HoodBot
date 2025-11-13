#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.ComponentModel;
using System.IO;
using RobinHood70.WallE.Properties;

public class UploadInput(string remoteName, Stream fileData)
{
	#region Public Properties
	public int ChunkSize { get; set; }

	public string? Comment { get; set; }

	public Stream FileData { get; } = fileData;

	public bool IgnoreWarnings { get; set; }

	public string RemoteFileName { get; } = remoteName;

	[Localizable(false)]
	public string? Text { get; set; }

	public string? Token { get; set; }

	public WatchlistOption Watchlist
	{
		get;
		set
		{
			if (value == WatchlistOption.Unwatch)
			{
				throw new ArgumentOutOfRangeException(paramName: nameof(value), message: Messages.UploadUnwatchInvalid);
			}

			field = value;
		}
	}
	#endregion
}