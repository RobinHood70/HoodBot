namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropInterwikiLinks(WikiAbstractionLayer wal, InterwikiLinksInput input) : PropListModule<InterwikiLinksInput, InterwikiLinksResult, InterwikiTitleItem>(wal, input, null)
	{
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
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
			request
				.AddIf("url", input.Properties.HasAnyFlag(InterwikiLinksProperties.Url), this.SiteVersion < 124)
				.AddFlagsIf("prop", input.Properties, this.SiteVersion >= 124)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("title", input.Title)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override InterwikiTitleItem? GetItem(JToken result) => result == null
			? null
			: new InterwikiTitleItem(result.MustHaveString("prefix"), result.MustHaveBCString("title")!, (Uri?)result["url"]);

		protected override InterwikiLinksResult GetNewList(JToken parent) => [];
		#endregion
	}
}