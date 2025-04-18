﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class MetaFileRepoInfo(WikiAbstractionLayer wal, FileRepositoryInfoInput input) : ListModule<FileRepositoryInfoInput, FileRepositoryInfoItem>(wal, input)
{
	#region Public Override Properties
	public override int MinimumVersion => 122;

	public override string Name => "filerepoinfo";
	#endregion

	#region Protected Override Properties
	protected override string ModuleType => "meta";

	protected override string Prefix => "fri";

	protected override string ResultName => "repos";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, FileRepositoryInfoInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request.Add("prop", input.Properties);
	}

	protected override FileRepositoryInfoItem? GetItem(JToken result)
	{
		if (result == null)
		{
			return null;
		}

		Dictionary<string, string?> otherInfo = new(StringComparer.Ordinal);
		foreach (var otherNode in result.Children<JProperty>())
		{
			SortedSet<string> ignoreWords = new(StringComparer.Ordinal) { "apiurl", "articleurl", "descBaseUrl", "descriptionCacheExpiry", "displayname", "favicon", "fetchDescription", "initialCapital", "local", "name", "rootUrl", "scriptDirUrl", "scriptExtension", "thumbUrl", "url" };
			if (!ignoreWords.Contains(otherNode.Name))
			{
				otherInfo.Add(otherNode.Name, (string?)otherNode.Value);
			}
		}

		return new FileRepositoryInfoItem(
			name: result.MustHaveString("name"),
			displayName: result.MustHaveString("displayname"),
			rootUrl: result.MustHaveString("rootUrl"),
			apiUrl: result.MustHaveString("apiurl"),
			articleUrl: (string?)result["articleurl"],
			descBaseUrl: (string?)result["descBaseUrl"],
			descCacheExpiry: TimeSpan.FromSeconds((int?)result["descriptionCacheExpiry"] ?? 0),
			favicon: (string?)result["favicon"],
			flags: result.GetFlags(
				("fetchDescription", FileRepositoryFlags.FetchDescription),
				("initialCapital", FileRepositoryFlags.InitialCapital),
				("local", FileRepositoryFlags.Local)),
			otherInfo: otherInfo,
			scriptDirUrl: (string?)result["scriptDirUrl"],
			scriptExt: (string?)result["scriptExtension"],
			thumbUrl: (string?)result["thumbUrl"],
			url: (string?)result["url"]);
	}
	#endregion
}