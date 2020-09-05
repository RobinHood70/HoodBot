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
		public string? ArchiveName { get; internal set; }

		public int BitDepth { get; internal set; }

		public string? CanonicalTitle { get; internal set; }

		public string? Comment { get; internal set; }

		public IReadOnlyDictionary<string, object> CommonMetadata { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

		public string? DescriptionUri { get; internal set; }

		public float Duration { get; internal set; }

		public IReadOnlyDictionary<string, ExtendedMetadataItem> ExtendedMetadata { get; } = new Dictionary<string, ExtendedMetadataItem>(StringComparer.Ordinal);

		public ImageInfoFlags Flags { get; internal set; }

		public int Height { get; internal set; }

		public string? MediaType { get; internal set; }

		public IReadOnlyDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

		public string? MimeType { get; internal set; }

		public long PageCount { get; internal set; }

		public string? ParsedComment { get; internal set; }

		public string? Sha1 { get; internal set; }

		public int Size { get; internal set; }

		public string? ThumbError { get; internal set; }

		public int ThumbHeight { get; internal set; }

		public string? ThumbMime { get; internal set; }

		public string? ThumbUri { get; internal set; }

		public int ThumbWidth { get; internal set; }

		public DateTime? Timestamp { get; internal set; }

		public string? UploadWarningHtml { get; internal set; }

		public string? Uri { get; internal set; }

		public long UserId { get; internal set; }

		public string? User { get; internal set; }

		public int Width { get; internal set; }
		#endregion
	}
}
