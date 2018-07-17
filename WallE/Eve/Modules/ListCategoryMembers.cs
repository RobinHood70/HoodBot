#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListCategoryMembers : ListModule<CategoryMembersInput, CategoryMembersItem>, IGeneratorModule
	{
		#region Static Fields
		private static Dictionary<string, CategoryMemberTypes> typeLookup = new Dictionary<string, CategoryMemberTypes>
		{
			["file"] = CategoryMemberTypes.File,
			["page"] = CategoryMemberTypes.Page,
			["subcat"] = CategoryMemberTypes.Subcat
		};
		#endregion

		#region Constructors
		public ListCategoryMembers(WikiAbstractionLayer wal, CategoryMembersInput input)
			: this(wal, input, null)
		{
		}

		public ListCategoryMembers(WikiAbstractionLayer wal, CategoryMembersInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "categorymembers";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "cm";
		#endregion

		#region Public Static Methods
		public static ListCategoryMembers CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListCategoryMembers(wal, input as CategoryMembersInput, pageSetGenerator);
		#endregion

		#region Protected Override Methods
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

			var item = new CategoryMembersItem
			{
				SortKey = (string)result["sortkey"],
				SortKeyPrefix = (string)result["sortkeyprefix"],
				Timestamp = (DateTime?)result["timestamp"]
			}.GetWikiTitle(result);

			var typeText = (string)result["type"];
			if (typeText != null && typeLookup.TryGetValue(typeText, out var itemType))
			{
				item.Type = itemType;
			}

			return item;
		}
		#endregion
	}
}