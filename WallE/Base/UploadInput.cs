#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using RobinHood70.WallE.Properties;
	using static RobinHood70.WikiCommon.Globals;

	public class UploadInput
	{
		#region Fields
		private WatchlistOption watchlist;
		#endregion

		#region Constructors
		public UploadInput(string remoteName, Stream fileData)
		{
			this.FileData = fileData;
			this.RemoteFileName = remoteName;
		}
		#endregion

		#region Public Properties
		public int ChunkSize { get; set; }

		public string? Comment { get; set; }

		public Stream FileData { get; }

		public bool IgnoreWarnings { get; set; }

		public string RemoteFileName { get; }

		[Localizable(false)]
		public string? Text { get; set; }

		public string? Token { get; set; }

		public WatchlistOption Watchlist
		{
			get => this.watchlist;
			set
			{
				if (value == WatchlistOption.Unwatch)
				{
					throw new ArgumentOutOfRangeException(nameof(value), CurrentCulture(Messages.UploadUnwatchInvalid));
				}

				this.watchlist = value;
			}
		}
		#endregion
	}
}