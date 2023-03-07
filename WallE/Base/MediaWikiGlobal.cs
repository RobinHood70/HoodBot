#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;

	#region Public Enumerations
	[Flags]
	public enum ImageProperties
	{
		None = 0,
		Timestamp = 1,
		User = 1 << 1,
		UserId = 1 << 2,
		Comment = 1 << 3,
		ParsedComment = 1 << 4,
		CanonicalTitle = 1 << 5,
		Url = 1 << 6,
		Size = 1 << 7,
		//// Dimensions = 1 << 8,
		Sha1 = 1 << 9,
		Mime = 1 << 10,
		ThumbMime = 1 << 11,
		MediaType = 1 << 12,
		Metadata = 1 << 13,
		CommonMetadata = 1 << 14,
		ExtMetadata = 1 << 15,
		ArchiveName = 1 << 16,
		BitDepth = 1 << 17,
		All = Timestamp | User | UserId | Comment | ParsedComment | CanonicalTitle | Url | Size | Sha1 | Mime | ThumbMime | MediaType | Metadata | CommonMetadata | ExtMetadata | ArchiveName | BitDepth
	}

	public enum WatchlistOption
	{
		Preferences, // Preferences is put first, since it's wiki's default
		NoChange,
		Unwatch,
		Watch
	}

	[Flags]
	public enum WatchlistTypes
	{
		None = 0,
		Edit = 1,
		External = 1 << 1,
		New = 1 << 2,
		Log = 1 << 3,
		Categorize = 1 << 4,
		All = Edit | External | New | Log | Categorize
	}
	#endregion

	/// <summary>Contains enumerations and methods that are specific to MediaWiki and available globally.</summary>
	public static class MediaWikiGlobal
	{
		#region Public Constants
		public const int DiffToCurrent = 0;
		public const int DiffToNext = -1;
		public const int DiffToPrevious = -2;
		#endregion

		#region Public Methods

		[return: NotNullIfNotNull(nameof(revision))]
		public static string? GetDiffToValue(int? revision) => revision switch
		{
			null => null,
			DiffToNext => "next",
			DiffToPrevious => "prev",
			DiffToCurrent => "cur",
			_ => revision.Value.ToString(CultureInfo.InvariantCulture),
		};
		#endregion
	}
}