#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	[Flags]
	public enum ImageInfoFlags
	{
		None = 0,
		Anonymous = 1,
		CommentHidden = 1 << 1,
		FileHidden = 1 << 2,
		Suppressed = 1 << 3,
		UserHidden = 1 << 4
	}

	public class ImageInfoItem
	{
		#region Public Properties
		public string ArchiveName { get; set; }

		public int BitDepth { get; set; }

		public string CanonicalTitle { get; set; }

		public string Comment { get; set; }

		public IReadOnlyDictionary<string, object> CommonMetadata { get; set; }

		public string DescriptionUri { get; set; }

		public float Duration { get; set; }

		public IReadOnlyDictionary<string, ExtendedMetadataItem> ExtendedMetadata { get; set; }

		public ImageInfoFlags Flags { get; set; }

		public int Height { get; set; }

		public string MediaType { get; set; }

		public IReadOnlyDictionary<string, object> Metadata { get; set; }

		public string MimeType { get; set; }

		public long PageCount { get; set; }

		public string ParsedComment { get; set; }

		public string Sha1 { get; set; }

		public int Size { get; set; }

		public string ThumbError { get; set; }

		public int ThumbHeight { get; set; }

		public string ThumbMime { get; set; }

		public string ThumbUri { get; set; }

		public int ThumbWidth { get; set; }

		public DateTime? Timestamp { get; set; }

		public string UploadWarningHtml { get; set; }

		public string Uri { get; set; }

		public long UserId { get; set; }

		public string User { get; set; }

		public int Width { get; set; }
		#endregion
	}
}
