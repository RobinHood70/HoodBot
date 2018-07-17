#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropLanguageLinks : PropListModule<LanguageLinksInput, LanguageLinksItem>
	{
		#region Constructors
		public PropLanguageLinks(WikiAbstractionLayer wal, LanguageLinksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "langlinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "ll";
		#endregion

		#region Public Static Methods
		public static PropLanguageLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropLanguageLinks(wal, input as LanguageLinksInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LanguageLinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlagsIf("prop", input.Properties, this.SiteVersion >= 123)
				.AddIf("url", input.Properties.HasFlag(LanguageLinksProperties.Url), this.SiteVersion >= 117 && this.SiteVersion < 123)
				.AddIfNotNull("lang", input.Language)
				.AddIfNotNull("title", input.Title)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIfNotNull("inlangcode", input.InLanguageCode)
				.Add("limit", this.Limit);
		}

		protected override LanguageLinksItem GetItem(JToken result) => result.GetLanguageLink();

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.LanguageLinks);

		protected override void SetResultsOnCurrentPage() => this.Output.LanguageLinks = this.GetResult();
		#endregion
	}
}
