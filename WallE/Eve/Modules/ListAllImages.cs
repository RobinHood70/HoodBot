namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListAllImages(WikiAbstractionLayer wal, AllImagesInput input, IPageSetGenerator? pageSetGenerator) : ListModule<AllImagesInput, AllImagesItem>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Constructors
		public ListAllImages(WikiAbstractionLayer wal, AllImagesInput input)
			: this(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 113;

		public override string Name => "allimages";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ai";
		#endregion

		#region Public Static Methods
		public static ListAllImages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (AllImagesInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllImagesInput input)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
			request
				//// .AddIf("dir", "newer", input.SortDescending && input.SortBy == AllImagesSort.Timestamp);
				//// does not seem to be necessary, as module appears to handle either term correctly, even though inline comments in it would suggest otherwise.
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