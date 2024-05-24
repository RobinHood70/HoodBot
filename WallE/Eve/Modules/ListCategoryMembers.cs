namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListCategoryMembers(WikiAbstractionLayer wal, CategoryMembersInput input, IPageSetGenerator? pageSetGenerator) : ListModule<CategoryMembersInput, CategoryMembersItem>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Static Fields
		private static readonly Dictionary<string, CategoryMemberTypes> TypeLookup = new(StringComparer.Ordinal)
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
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "categorymembers";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "cm";
		#endregion

		#region Public Static Methods
		public static ListCategoryMembers CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (CategoryMembersInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CategoryMembersInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
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
				.AddIfNotNullIf("startsortkey", PackHex(input.StartHexSortKey), this.SiteVersion < 124)
				.AddIfNotNullIf("endsortkey", PackHex(input.EndHexSortKey), this.SiteVersion < 124)
				.AddIfNotNullIf("starthexsortkey", input.StartHexSortKey, this.SiteVersion >= 124)
				.AddIfNotNullIf("endhexsortkey", input.EndHexSortKey, this.SiteVersion >= 124)
				.AddIfNotNull("startsortkeyprefix", input.StartSortKeyPrefix)
				.AddIfNotNull("endsortkeyprefix", input.EndSortKeyPrefix)
				.Add("limit", this.Limit);
		}

		protected override CategoryMembersItem GetItem(JToken result)
		{
			ArgumentNullException.ThrowIfNull(result);
			var typeText = (string?)result["type"];
			if (typeText == null || !TypeLookup.TryGetValue(typeText, out var itemType))
			{
				itemType = CategoryMemberTypes.None;
			}

			CategoryMembersItem item = new(
				(int)result.MustHave("ns"),
				result.MustHaveString("title"),
				(long?)result["pageid"] ?? 0,
				(string?)result["sortkey"],
				(string?)result["sortkeyprefix"],
				(DateTime?)result["timestamp"],
				itemType);

			return item;
		}
		#endregion

		#region Private Static Methods
		[return: NotNullIfNotNull(nameof(hexValue))]
		private static string? PackHex(string? hexValue)
		{
			if (hexValue == null)
			{
				return null;
			}

			if ((hexValue.Length & 1) == 1)
			{
				hexValue = "0" + hexValue;
			}

			var hexLength2 = hexValue.Length >> 1;
			var retval = new byte[hexLength2];
			for (var i = 0; i < hexLength2; i++)
			{
				retval[i] = Convert.ToByte(hexValue.Substring(i << 1, 2), 16);
			}

			return Encoding.UTF8.GetString(retval);
		}
		#endregion
	}
}