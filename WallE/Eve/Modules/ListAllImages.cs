#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllImages : ListModule<AllImagesInput, AllImagesItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllImages(WikiAbstractionLayer wal, AllImagesInput input)
			: this(wal, input, null)
		{
		}

		public ListAllImages(WikiAbstractionLayer wal, AllImagesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 113;

		public override string Name { get; } = "allimages";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "ai";
		#endregion

		#region Public Static Methods
		public static ListAllImages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is AllImagesInput listInput
				? new ListAllImages(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(AllImagesInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllImagesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));

			// request.AddIf("dir", "newer", input.SortDescending && input.SortBy == AllImagesSort.Timestamp); does not seem to be necessary, as module appears to handle either term correctly, even though inline comments in it would suggest otherwise.
			request
				.AddIfPositive("sort", input.SortBy)
				.AddIf("dir", "descending", input.SortDescending) // && input.SortBy != AllImagesSort.Timestamp
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddFlags("prop", input.Properties & ~(ImageProperties.ArchiveName | ImageProperties.ThumbMime)) // AllImages does not support the ArchiveName or ThumbMime properties, so ignore those if they were specified.
				.AddIfNotNull("prefix", input.Prefix)
				.AddIf("minsize", input.MinimumSize, input.MinimumSize >= 0)
				.AddIf("maxsize", input.MaximumSize, input.MaximumSize >= 0)
				.AddIfNotNull("sha1", input.Sha1)
				.AddIfNotNull("user", input.User)
				.AddFilterText("filterbots", "bots", "nobots", input.FilterBots)
				.AddIfNotNull("mime", input.MimeType?.ToLowerInvariant())
				.Add("limit", this.Limit);
		}

		protected override AllImagesItem? GetItem(JToken result) => result == null
			? null
			: JTokenImageInfo.ParseImageInfo(result, new AllImagesItem((int)result.MustHave("ns"), result.MustHaveString("title"), result.MustHaveString("name")));
		#endregion
	}
}