namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;
using RobinHood70.WallE.Eve;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class PropImageInfo(WikiAbstractionLayer wal, ImageInfoInput input) : PropListModule<ImageInfoInput, ImageInfoResult, ImageInfoItem>(wal, input, null)
{
	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name => "imageinfo";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "ii";
	#endregion

	#region Public Static Methods
	public static PropImageInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (ImageInfoInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, ImageInfoInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		var prop = FlagFilter
			.Check(this.SiteVersion, input.Properties)
			.FilterBefore(123, ImageProperties.CanonicalTitle | ImageProperties.CommonMetadata | ImageProperties.ExtMetadata)
			.FilterBefore(118, ImageProperties.MediaType)
			.FilterBefore(117, ImageProperties.ParsedComment | ImageProperties.ThumbMime | ImageProperties.UserId)
			.Value;
		request
			.AddFlags("prop", prop)
			.Add("start", input.Start)
			.Add("end", input.End)
			.AddIf("urlwidth", input.UrlWidth, prop.HasAnyFlag(ImageProperties.Url) && input.UrlWidth > 0)
			.AddIf("urlheight", input.UrlHeight, prop.HasAnyFlag(ImageProperties.Url) && input.UrlHeight > 0)
			.AddIf("metadataversion", input.MetadataVersion, input.MetadataVersion > 0 && this.SiteVersion >= 118)
			.AddIfNotNullIf("extmetadatalanguage", input.ExtendedMetadataLanguage, this.SiteVersion >= 123)
			.AddIf("extmetadatamultilang", input.ExtendedMetadataMultilanguage, this.SiteVersion >= 123)
			.AddIf("extmetadatafilter", input.ExtendedMetadataFilter, this.SiteVersion >= 123)
			.AddIfNotNullIf("urlparam", input.UrlParameter, this.SiteVersion >= 118)
			.AddIf("localonly", input.LocalOnly, this.SiteVersion >= 120)
			.AddIf("limit", this.Limit, input.Limit > 1);
	}

	protected override ImageInfoItem GetItem(JToken result) => JTokenImageInfo.ParseImageInfo(result, new ImageInfoItem());

	protected override ImageInfoResult GetNewList(JToken parent)
	{
		// Repo can be null if page has no images or it's a non-file page.
		ArgumentNullException.ThrowIfNull(parent);
		return new ImageInfoResult((string?)parent["imagerepository"]);
	}
	#endregion
}