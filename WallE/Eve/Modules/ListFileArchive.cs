namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListFileArchive : ListModule<FileArchiveInput, FileArchiveItem>
	{
		#region Constructors
		public ListFileArchive(WikiAbstractionLayer wal, FileArchiveInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 117;

		public override string Name => "filearchive";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "fa";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FileArchiveInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(120, FileArchiveProperties.ArchiveName | FileArchiveProperties.MediaType)
				.FilterBefore(118, FileArchiveProperties.ParsedDescription)
				.Value;
			request
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIfNotNull("sha1", input.Sha1)
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override FileArchiveItem? GetItem(JToken result) => result == null
			? null
			: JTokenImageInfo.ParseImageInfo(result, new FileArchiveItem(
				name: result.MustHaveString("name"),
				fileArchiveId: (long?)result["id"] ?? 0,
				ns: (int?)result["ns"],
				title: (string?)result["title"]));
		#endregion
	}
}