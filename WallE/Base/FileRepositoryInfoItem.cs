#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum FileRepositoryFlags
	{
		None = 0,
		FetchDescription = 1,
		InitialCapital = 1 << 1,
		Local = 1 << 2
	}
	#endregion

	public class FileRepositoryInfoItem
	{
		#region Public Properties
		public string ApiUrl { get; set; }

		public string ArticleUrl { get; set; }

		public string DescriptionBaseUrl { get; set; }

		public TimeSpan DescriptionCacheExpiry { get; set; }

		public string DisplayName { get; set; }

		public string Favicon { get; set; }

		public FileRepositoryFlags Flags { get; set; }

		public string Name { get; set; }

		public IReadOnlyDictionary<string, string> OtherInfo { get; set; }

		public string RootUrl { get; set; }

		public string ScriptDirectoryUrl { get; set; }

		public string ScriptExtension { get; set; }

		public string ThumbUrl { get; set; }

		public string Url { get; set; }
		#endregion
	}
}
