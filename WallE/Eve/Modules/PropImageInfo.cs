namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class PropImageInfo : PropListModule<ImageInfoInput, ImageInfoItem>
	{
		#region Constructors
		public PropImageInfo(WikiAbstractionLayer wal, ImageInfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "imageinfo";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ii";
		#endregion

		#region Public Static Methods
		public static PropImageInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is ImageInfoInput propInput
				? new PropImageInfo(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(ImageInfoInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ImageInfoInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
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
				.AddIf("urlwidth", input.UrlWidth, (prop & ImageProperties.Url) != 0 && input.UrlWidth > 0)
				.AddIf("urlheight", input.UrlHeight, (prop & ImageProperties.Url) != 0 && input.UrlHeight > 0)
				.AddIf("metadataversion", input.MetadataVersion, input.MetadataVersion > 0 && this.SiteVersion >= 118)
				.AddIfNotNullIf("extmetadatalanguage", input.ExtendedMetadataLanguage, this.SiteVersion >= 123)
				.AddIf("extmetadatamultilang", input.ExtendedMetadataMultilanguage, this.SiteVersion >= 123)
				.AddIf("extmetadatafilter", input.ExtendedMetadataFilter, this.SiteVersion >= 123)
				.AddIfNotNullIf("urlparam", input.UrlParameter, this.SiteVersion >= 118)
				.AddIf("localonly", input.LocalOnly, this.SiteVersion >= 120)
				.AddIf("limit", this.Limit, input.Limit > 1);
		}

		protected override void DeserializeParentToPage(JToken parent, PageItem page)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(page, nameof(page));
			page.ImageRepository = (string?)parent["imagerepository"];
		}

		protected override ImageInfoItem GetItem(JToken result, PageItem page) => JTokenImageInfo.ParseImageInfo(result, new ImageInfoItem());

		protected override ICollection<ImageInfoItem> GetMutableList(PageItem page) => (ICollection<ImageInfoItem>)page.ImageInfoEntries;
		#endregion
	}
}