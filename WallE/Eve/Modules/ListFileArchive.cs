#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListFileArchive : ListModule<FileArchiveInput, FileArchiveItem>
	{
		#region Constructors
		public ListFileArchive(WikiAbstractionLayer wal, FileArchiveInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "filearchive";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "fa";
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

		protected override FileArchiveItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			// Not using GetWikiTitle because PageId uses non-standard name.
			var item = new FileArchiveItem
			{
				Name = (string)result["name"],
				Namespace = (int?)result["ns"],
				PageId = (long?)result["id"] ?? 0,
				Title = (string)result["title"]
			};
			result.ParseImageInfo(item);

			return item;
		}
		#endregion
	}
}