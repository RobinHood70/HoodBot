#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListFileArchive : ListModule<FileArchiveInput, FileArchiveItem>
	{
		#region Constructors
		public ListFileArchive(WikiAbstractionLayer wal, FileArchiveInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "filearchive";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "fa";
		#endregion

		#region Public Override Methods
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

			var item = new FileArchiveItem();
			result.ParseImageInfo(item);
			item.PageId = (long?)result["id"] ?? 0;
			item.Name = (string)result["name"];
			item.Namespace = (int?)result["ns"];
			item.Title = (string)result["title"];

			return item;
		}
		#endregion
	}
}