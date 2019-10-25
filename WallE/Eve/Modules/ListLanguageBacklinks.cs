#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListLanguageBacklinks : ListModule<LanguageBacklinksInput, LanguageBacklinksItem>, IGeneratorModule
	{
		#region Constructors
		public ListLanguageBacklinks(WikiAbstractionLayer wal, LanguageBacklinksInput input)
			: this(wal, input, null)
		{
		}

		public ListLanguageBacklinks(WikiAbstractionLayer wal, LanguageBacklinksInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 118;

		public override string Name { get; } = "langbacklinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "lbl";
		#endregion

		#region Public Static Methods
		public static ListLanguageBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is LanguageBacklinksInput listInput
				? new ListLanguageBacklinks(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(LanguageBacklinksInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LanguageBacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("lang", input.Language)
				.AddIfNotNull("title", input.Title)
				.AddFlags("prop", input.Properties)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override LanguageBacklinksItem? GetItem(JToken result) => result == null
			? null
			: new LanguageBacklinksItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				pageId: (long)result.MustHave("pageid"),
				isRedirect: result["redirect"].ToBCBool(),
				langCode: (string?)result["lllang"],
				langTitle: (string?)result["lltitle"]);
		#endregion
	}
}
