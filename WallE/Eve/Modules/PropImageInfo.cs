#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropImageInfo : PropListModule<ImageInfoInput, ImageInfoItem>
	{
		#region Constructors
		public PropImageInfo(WikiAbstractionLayer wal, ImageInfoInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "imageinfo";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "ii";
		#endregion

		#region Public Static Methods
		public static PropImageInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropImageInfo(wal, input as ImageInfoInput);
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
				.AddIf("urlwidth", input.UrlWidth, prop.HasFlag(ImageProperties.Url) && input.UrlWidth > 0)
				.AddIf("urlheight", input.UrlHeight, prop.HasFlag(ImageProperties.Url) && input.UrlHeight > 0)
				.AddIf("metadataversion", input.MetadataVersion, input.MetadataVersion > 0 && this.SiteVersion >= 118)
				.AddIfNotNullIf("extmetadatalanguage", input.ExtendedMetadataLanguage, this.SiteVersion >= 123)
				.AddIf("extmetadatamultilang", input.ExtendedMetadataMultilanguage, this.SiteVersion >= 123)
				.AddIf("extmetadatafilter", input.ExtendedMetadataFilter, this.SiteVersion >= 123)
				.AddIfNotNullIf("urlparam", input.UrlParameter, this.SiteVersion >= 118)
				.AddIf("localonly", input.LocalOnly, this.SiteVersion >= 120)
				.AddIf("limit", this.Limit, input.Limit > 1);
		}

		protected override void DeserializeParent(JToken parent, PageItem output)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(output, nameof(output));
			if (parent["imagerepository"] != null)
			{
				output.ImageRepository = (string)parent["imagerepository"];
			}
		}

		protected override ImageInfoItem GetItem(JToken result) => result.ParseImageInfo();

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.ImageInfoEntries);

		protected override void SetResultsOnCurrentPage() => this.Output.ImageInfoEntries = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}