namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;

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
		public static PropImageInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (ImageInfoInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ImageInfoInput input)
		{
			var prop = FlagFilter
				.Check(this.SiteVersion, input.NotNull().Properties)
				.FilterBefore(123, ImageProperties.CanonicalTitle | ImageProperties.CommonMetadata | ImageProperties.ExtMetadata)
				.FilterBefore(118, ImageProperties.MediaType)
				.FilterBefore(117, ImageProperties.ParsedComment | ImageProperties.ThumbMime | ImageProperties.UserId)
				.Value;
			request
				.NotNull()
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

		protected override void DeserializeParentToPage(JToken parent, PageItem page) => page
			.NotNull()
			.ImageRepository = (string?)parent
				.NotNull()["imagerepository"];

		protected override ImageInfoItem GetItem(JToken result, PageItem page) => JTokenImageInfo.ParseImageInfo(result, new ImageInfoItem());

		protected override ICollection<ImageInfoItem> GetMutableList(PageItem page) => (ICollection<ImageInfoItem>)page.ImageInfoEntries;
		#endregion
	}
}