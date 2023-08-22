#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations

	// Apart from All, these values mirror ImageProperties in case the modules are later merged.
	[Flags]
	public enum StashImageProperties
	{
		None = 0,
		Timestamp = 1,
		CanonicalTitle = 1 << 5,
		Url = 1 << 6,
		Size = 1 << 7,
		Dimensions = 1 << 8,
		Sha1 = 1 << 9,
		Mime = 1 << 10,
		ThumbMime = 1 << 11,
		Metadata = 1 << 13,
		CommonMetadata = 1 << 14,
		ExtMetadata = 1 << 15,
		BitDepth = 1 << 17,
		All = Timestamp | CanonicalTitle | Url | Size | Dimensions | Sha1 | Mime | ThumbMime | Metadata | CommonMetadata | ExtMetadata | BitDepth
	}
	#endregion

	public class StashImageInfoInput : IPropertyInput
	{
		#region Constructors
		public StashImageInfoInput(IEnumerable<string> fileKeys)
		{
			this.FileKeys = fileKeys;
		}
		#endregion

		#region Public Properties
		public IEnumerable<string> FileKeys { get; }

		public StashImageProperties Properties { get; set; }

		public int UrlHeight { get; set; }

		public string? UrlParameter { get; set; }

		public int UrlWidth { get; set; }
		#endregion
	}
}