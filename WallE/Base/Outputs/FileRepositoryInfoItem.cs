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
		#region Constructors
		internal FileRepositoryInfoItem(string name, string displayName, string rootUrl, string apiUrl, string? articleUrl, string? descBaseUrl, TimeSpan descCacheExpiry, string? favicon, FileRepositoryFlags flags, IReadOnlyDictionary<string, string?> otherInfo, string? scriptDirUrl, string? scriptExt, string? thumbUrl, string? url)
		{
			this.Name = name;
			this.DisplayName = displayName;
			this.RootUrl = rootUrl;
			this.ApiUrl = apiUrl;
			this.ArticleUrl = articleUrl;
			this.DescriptionBaseUrl = descBaseUrl;
			this.DescriptionCacheExpiry = descCacheExpiry;
			this.Favicon = favicon;
			this.Flags = flags;
			this.OtherInfo = otherInfo;
			this.ScriptDirectoryUrl = scriptDirUrl;
			this.ScriptExtension = scriptExt;
			this.ThumbUrl = thumbUrl;
			this.Url = url;
		}
		#endregion

		#region Public Properties
		public string ApiUrl { get; }

		public string? ArticleUrl { get; }

		public string? DescriptionBaseUrl { get; }

		public TimeSpan DescriptionCacheExpiry { get; }

		public string DisplayName { get; }

		public string? Favicon { get; }

		public FileRepositoryFlags Flags { get; }

		public string Name { get; }

		public IReadOnlyDictionary<string, string?> OtherInfo { get; }

		public string RootUrl { get; }

		public string? ScriptDirectoryUrl { get; }

		public string? ScriptExtension { get; }

		public string? ThumbUrl { get; }

		public string? Url { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName;
		#endregion
	}
}
