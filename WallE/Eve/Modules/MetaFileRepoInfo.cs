#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class MetaFileRepoInfo : ListModule<FileRepositoryInfoInput, FileRepositoryInfoItem>
	{
		#region Constructors
		public MetaFileRepoInfo(WikiAbstractionLayer wal, FileRepositoryInfoInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 122;

		public override string Name { get; } = "filerepoinfo";
		#endregion

		#region Public Override Properties
		protected override string ModuleType => "meta";

		protected override string BasePrefix { get; } = "fri";

		protected override string ResultName { get; } = "repos";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FileRepositoryInfoInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("prop", input.Properties);
		}

		protected override FileRepositoryInfoItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new FileRepositoryInfoItem()
			{
				ApiUrl = (string)result["apiurl"],
				ArticleUrl = (string)result["articleurl"],
				DescriptionBaseUrl = (string)result["descBaseUrl"],
				DescriptionCacheExpiry = TimeSpan.FromSeconds((int?)result["descriptionCacheExpiry"] ?? 0),
				DisplayName = (string)result["displayname"],
				Favicon = (string)result["favicon"],
				Flags =
				result.GetFlag("fetchDescription", FileRepositoryFlags.FetchDescription) |
				result.GetFlag("initialCapital", FileRepositoryFlags.InitialCapital) |
				result.GetFlag("local", FileRepositoryFlags.Local),
				Name = (string)result["name"],
				RootUrl = (string)result["rootUrl"],
				ScriptDirectoryUrl = (string)result["scriptDirUrl"],
				ScriptExtension = (string)result["scriptExtension"],
				ThumbUrl = (string)result["thumbUrl"],
				Url = (string)result["url"],
			};
			var otherInfo = new Dictionary<string, string>();
#pragma warning disable IDE0007 // Use implicit type
			foreach (JProperty otherNode in result)
#pragma warning restore IDE0007 // Use implicit type
			{
				var ignoreWords = new SortedSet<string>() { "apiurl", "articleurl", "descBaseUrl", "descriptionCacheExpiry", "displayname", "favicon", "fetchDescription", "initialCapital", "local", "name", "rootUrl", "scriptDirUrl", "scriptExtension", "thumbUrl", "url" };
				if (!ignoreWords.Contains(otherNode.Name))
				{
					otherInfo.Add(otherNode.Name, (string)otherNode.Value);
				}
			}

			item.OtherInfo = otherInfo;

			return item;
		}
		#endregion
	}
}