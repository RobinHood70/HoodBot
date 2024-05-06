namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListLanguageBacklinks : ListModule<LanguageBacklinksInput, LanguageBacklinksItem>, IGeneratorModule
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
		public override int MinimumVersion => 118;

		public override string Name => "langbacklinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "lbl";
		#endregion

		#region Public Static Methods
		public static ListLanguageBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (LanguageBacklinksInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LanguageBacklinksInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
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
				isRedirect: result["redirect"].GetBCBool(),
				langCode: (string?)result["lllang"],
				langTitle: (string?)result["lltitle"]);
		#endregion
	}
}