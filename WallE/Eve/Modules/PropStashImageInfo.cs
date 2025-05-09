﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;
using RobinHood70.WikiCommon.RequestBuilder;

// TODO: Monitor the links below and see if this is ultimately implemented as a list or with Special:UploadStash/$key as a valid page title, then adapt code as needed.
// This behaves more like a List module, but does not support limits or continuation, and is therefore internally treated as just a normal query module. It is not (and should not be made into) a property module internally. The entire PHP version of the module will likely be re-written in the future.
// See https://phabricator.wikimedia.org/T38220 and https://phabricator.wikimedia.org/T89971.
internal sealed class PropStashImageInfo(WikiAbstractionLayer wal, StashImageInfoInput input) : QueryModule<StashImageInfoInput, IList<ImageInfoItem>>(wal, input, null)
{
	#region Public Override Properties
	public override int MinimumVersion => 117;

	public override string Name => "stashimageinfo";
	#endregion

	#region Protected Override Properties
	protected override string ModuleType => "prop";

	protected override string Prefix => "sii";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, StashImageInfoInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		var prop = FlagFilter
			.Check(this.SiteVersion, input.Properties)
			.FilterBefore(123, StashImageProperties.CanonicalTitle | StashImageProperties.CommonMetadata | StashImageProperties.ExtMetadata)
			.Value;
		request
			.AddFlags("prop", prop)
			.Add(this.SiteVersion < 118 ? "sessionkey" : "filekey", input.FileKeys)
			.AddIf("urlwidth", input.UrlWidth, prop.HasAnyFlag(StashImageProperties.Url) && input.UrlWidth > 0)
			.AddIf("urlheight", input.UrlHeight, prop.HasAnyFlag(StashImageProperties.Url) && input.UrlHeight > 0)
			.AddIfNotNullIf("urlparam", input.UrlParameter, this.SiteVersion >= 118);
	}

	protected override void DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		this.Output = [];
		foreach (var item in result)
		{
			var imageInfo = JTokenImageInfo.ParseImageInfo(item, new ImageInfoItem());
			this.Output.Add(imageInfo);
		}
	}
	#endregion
}