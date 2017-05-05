#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListCategoryMembers : ListModule<CategoryMembersInput, CategoryMembersItem>, IGeneratorModule
	{
		#region Constructors
		public ListCategoryMembers(WikiAbstractionLayer wal, CategoryMembersInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "categorymembers";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "cm";
		#endregion

		#region Public Static Methods
		public static ListCategoryMembers CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListCategoryMembers(wal, input as CategoryMembersInput);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, CategoryMembersInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("title", input.Title)
				.AddIf("pageid", input.PageId, input.Title == null)
				.AddFlags("prop", input.Properties)
				.Add("namespace", input.Namespaces)
				.AddFlags("type", input.Type)
				.AddIfPositive("sort", input.Sort)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("startsortkey", input.StartHexSortKey.PackHex(), input.StartHexSortKey != null && this.SiteVersion < 124)
				.AddIf("endsortkey", input.EndHexSortKey.PackHex(), input.EndHexSortKey != null && this.SiteVersion < 124)
				.AddIfNotNullIf("starthexsortkey", input.StartHexSortKey, this.SiteVersion >= 124)
				.AddIfNotNullIf("endhexsortkey", input.EndHexSortKey, this.SiteVersion >= 124)
				.AddIfNotNull("startsortkeyprefix", input.StartSortKeyPrefix)
				.AddIfNotNull("endsortkeyprefix", input.EndSortKeyPrefix)
				.Add("limit", this.Limit);
		}

		protected override CategoryMembersItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new CategoryMembersItem();
			item.GetWikiTitle(result);
			item.SortKey = (string)result["sortkey"];
			item.SortKeyPrefix = (string)result["sortkeyprefix"];
			item.Timestamp = (DateTime?)result["timestamp"];
			item.Type = (string)result["type"];

			return item;
		}
		#endregion
	}
}