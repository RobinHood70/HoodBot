namespace RobinHood70.Robby.Pages
{
	using System;

	public class FileRevision
	{
		public FileRevision(int bitDepth, int fileSize, int height, int width, string comment, string mimeType, string user, DateTime? timestamp, Uri uri)
		{
			this.BitDepth = bitDepth;
			this.Comment = comment;
			this.FileSize = fileSize;
			this.Uri = uri;
			this.Height = height;
			this.MimeType = mimeType;
			this.Timestamp = timestamp;
			this.User = user;
			this.Width = width;
		}

		public int BitDepth { get; }

		public string Comment { get; }

		public int FileSize { get; }

		public Uri Uri { get; }

		public int Height { get; }

		public string MimeType { get; }

		public DateTime? Timestamp { get; }

		public string User { get; }

		public int Width { get; }
	}
}
