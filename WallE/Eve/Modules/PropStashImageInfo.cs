#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// TODO: Monitor the links below and see if this is ultimately implemented as a list or with Special:UploadStash/$key as a valid page title, then adapt code as needed.
	// This behaves more like a List module, and is therefore internally treated as such. It is not (and should not be made into) a property module internally. The entire PHP version of the module will likely be re-written in the future. See https://phabricator.wikimedia.org/T38220 and https://phabricator.wikimedia.org/T89971.
	internal class PropStashImageInfo : ListModule<StashImageInfoInput, ImageInfoItem>
	{
		#region Constructors
		public PropStashImageInfo(WikiAbstractionLayer wal, StashImageInfoInput input)
		: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "stashimageinfo";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "prop";

		protected override string Prefix { get; } = "sii";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, StashImageInfoInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(123, StashImageProperties.CanonicalTitle | StashImageProperties.CommonMetadata | StashImageProperties.ExtMetadata)
				.Value;
			request
				.AddFlags("prop", prop)
				.Add(this.SiteVersion < 118 ? "sessionkey" : "filekey", input.FileKeys)
				.AddIf("urlwidth", input.UrlWidth, prop.HasFlag(StashImageProperties.Url) && input.UrlWidth > 0)
				.AddIf("urlheight", input.UrlHeight, prop.HasFlag(StashImageProperties.Url) && input.UrlHeight > 0)
				.AddIfNotNullIf("urlparam", input.UrlParameter, this.SiteVersion >= 118);
		}

		protected override ImageInfoItem GetItem(JToken result) => result.ParseImageInfo();
		#endregion
	}
}
