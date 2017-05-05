#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListAllImages : ListModule<AllImagesInput, AllImagesItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllImages(WikiAbstractionLayer wal, AllImagesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 113;

		public override string Name { get; } = "allimages";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "ai";
		#endregion

		#region Public Static Methods
		public static ListAllImages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListAllImages(wal, input as AllImagesInput);
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

		protected override AllImagesItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new AllImagesItem()
			{
				Name = (string)result["name"],
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
			};
			result.ParseImageInfo(item);
			item.Url = (string)result["url"];
			item.DescriptionUrl = (string)result["descriptionurl"];

			return item;
		}
		#endregion
	}
}