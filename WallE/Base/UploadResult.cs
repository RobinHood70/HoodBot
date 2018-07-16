#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class UploadResult
	{
		#region Public Properties
		public IReadOnlyList<string> Duplicates { get; set; } = Array.Empty<string>();

		public string FileKey { get; set; }

		public string FileName { get; set; }

		public ImageInfoItem ImageInfo { get; set; }

		public string Result { get; set; }

		public IReadOnlyDictionary<string, string> Warnings { get; set; }
		#endregion
	}
}
