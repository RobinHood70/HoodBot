namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.IO;
using RobinHood70.WallE.Base;

internal sealed class UploadInputInternal
{
	#region Constructors
	public UploadInputInternal(UploadInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		this.IgnoreWarnings = input.IgnoreWarnings;
		this.Offset = 0;
		this.FileName = input.RemoteFileName;
		this.Token = input.Token;
		if (input.ChunkSize > 0)
		{
			this.Stash = true;
			this.FileSize = input.FileData.Length;
			this.FileData = []; // TODO: This is just to satisfy the null-checking, which doesn't flow into methods called from constructors. Remove if they implement that.
			this.NextChunk(input.FileData, input.ChunkSize);
		}
		else
		{
			this.Stash = false;
			this.FileSize = 0;
			this.Comment = input.Comment;
			this.Text = input.Text;
			this.Watchlist = input.Watchlist;

			var buffer = new byte[32768];
			using MemoryStream fileData = new();
			var readBytes = 0;
			do
			{
				readBytes = input.FileData.Read(buffer, 0, buffer.Length);
				fileData.Write(buffer, 0, readBytes);
			}
			while (readBytes > 0);

			this.FileData = fileData.ToArray();
		}
	}
	#endregion

	#region Public Properties
	public string? Comment { get; set; }

	public string? FileKey { get; set; }

	public string FileName { get; set; }

	public long FileSize { get; set; }

	public bool IgnoreWarnings { get; set; }

	public int Offset { get; set; }

	public bool Stash { get; set; }

	public string? Text { get; set; }

	public string? Token { get; set; }

	public WatchlistOption Watchlist { get; set; }
	#endregion

	#region Internal Properties
	internal byte[] FileData { get; private set; }
	#endregion

	#region Public Methods
	public void FinalChunk(UploadInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		this.Comment = input.Comment;
		this.FileData = [];
		this.FileSize = 0;
		this.IgnoreWarnings = input.IgnoreWarnings;
		this.Offset = 0;
		this.Stash = false;
		this.Text = input.Text;
		this.Watchlist = input.Watchlist;
	}

	public void NextChunk(Stream input, int chunkSize)
	{
		ArgumentNullException.ThrowIfNull(input);
		var copySize = this.FileSize - this.Offset;
		if (copySize > chunkSize)
		{
			copySize = chunkSize;
		}

		var fileData = new byte[copySize];
		var readBytes = input.Read(fileData, 0, (int)copySize); // Safe to cast, since it can't be larger than chunkSize, which is an integer
		if (readBytes != copySize)
		{
			Array.Resize(ref fileData, readBytes);
		}

		this.FileData = fileData;
	}
	#endregion
}