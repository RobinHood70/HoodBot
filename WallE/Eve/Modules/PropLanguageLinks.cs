namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropLanguageLinks : PropListModule<LanguageLinksInput, LanguageLinksItem>
	{
		#region Constructors
		public PropLanguageLinks(WikiAbstractionLayer wal, LanguageLinksInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "langlinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ll";
		#endregion

		#region Public Static Methods
		public static PropLanguageLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is LanguageLinksInput propInput
				? new PropLanguageLinks(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(LanguageLinksInput), input.GetType().Name);
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

		protected override LanguageLinksItem? GetItem(JToken result, PageItem page) => result.GetLanguageLink();

		protected override ICollection<LanguageLinksItem> GetMutableList(PageItem page) => (ICollection<LanguageLinksItem>)page.LanguageLinks;
		#endregion
	}
}
