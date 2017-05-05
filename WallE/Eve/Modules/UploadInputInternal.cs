#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.IO;
	using Base;
	using static WikiCommon.Globals;

	public class UploadInputInternal
	{
		#region Constructors
		public UploadInputInternal(UploadInput input)
		{
			ThrowNull(input, nameof(input));
			this.Comment = input.Comment;
			this.FileName = input.RemoteFileName;
			this.IgnoreWarnings = input.IgnoreWarnings;
			this.Text = input.Text;
			this.Watchlist = input.Watchlist;
			this.Token = input.Token;

			var buffer = new byte[32768];
			using (var retval = new MemoryStream())
			{
				var readBytes = 0;
				do
				{
					readBytes = input.FileData.Read(buffer, 0, buffer.Length);
					retval.Write(buffer, 0, readBytes);
				}
				while (readBytes > 0);

				this.FileData = retval.ToArray();
			}
		}

		public UploadInputInternal()
		{
		}
		#endregion

		#region Public Properties
		public string Comment { get; set; }

		public string FileKey { get; set; }

		public string FileName { get; set; }

		public long FileSize { get; set; }

		public bool IgnoreWarnings { get; set; }

		public int Offset { get; set; }

		public bool Stash { get; set; }

		public string Text { get; set; }

		public string Token { get; set; }

		public WatchlistOption Watchlist { get; set; }
		#endregion

		#region Internal Properties
		internal byte[] FileData { get; private set; }
		#endregion

		#region Public Methods
		public void FinalChunk(UploadInput input)
		{
			ThrowNull(input, nameof(input));
			this.Comment = input.Comment;
			this.FileData = null;
			this.FileSize = 0;
			this.IgnoreWarnings = input.IgnoreWarnings;
			this.Offset = 0;
			this.Stash = false;
			this.Text = input.Text;
			this.Watchlist = input.Watchlist;
		}

		public void InitialChunk(UploadInput input)
		{
			ThrowNull(input, nameof(input));
			this.IgnoreWarnings = input.IgnoreWarnings;
			this.Offset = 0;
			this.Stash = true;
			this.FileName = input.RemoteFileName;
			this.FileSize = input.FileData.Length;
			this.Token = input.Token;
		}

		public void NextChunk(Stream input, int chunkSize)
		{
			ThrowNull(input, nameof(input));
			var readBytes = 0;
			var copySize = this.FileSize - this.Offset;
			if (copySize > chunkSize)
			{
				copySize = chunkSize;
			}

			var fileData = new byte[copySize];
			readBytes = input.Read(fileData, 0, (int)copySize); // Safe to cast, since it can't be larger than chunkSize, which is an integer
			if (readBytes != copySize)
			{
				Array.Resize(ref fileData, readBytes);
			}

			this.FileData = fileData;
		}
		#endregion
	}
}
