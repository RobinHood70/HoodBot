namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropInterwikiLinks : PropListModule<InterwikiLinksInput, InterwikiTitleItem>
	{
		#region Constructors
		public PropInterwikiLinks(WikiAbstractionLayer wal, InterwikiLinksInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 117;

		public override string Name => "iwlinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "iw";
		#endregion

		#region Public Static Methods
		public static PropInterwikiLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (InterwikiLinksInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, InterwikiLinksInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.AddIf("url", (input.Properties & InterwikiLinksProperties.Url) != 0, this.SiteVersion < 124)
				.AddFlagsIf("prop", input.Properties, this.SiteVersion >= 124)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("title", input.Title)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override InterwikiTitleItem? GetItem(JToken result, PageItem page) => result == null
			? null
			: new InterwikiTitleItem(result.MustHaveString("prefix"), result.MustHaveBCString("title")!, (Uri?)result["url"]);

		protected override ICollection<InterwikiTitleItem> GetMutableList(PageItem page) => (ICollection<InterwikiTitleItem>)page.InterwikiLinks;
		#endregion
	}
}
