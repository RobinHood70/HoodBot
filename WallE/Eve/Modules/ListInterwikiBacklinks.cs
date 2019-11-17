#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListInterwikiBacklinks : ListModule<InterwikiBacklinksInput, InterwikiBacklinksItem>, IGeneratorModule
	{
		#region Constructors
		public ListInterwikiBacklinks(WikiAbstractionLayer wal, InterwikiBacklinksInput input)
			: this(wal, input, null)
		{
		}

		public ListInterwikiBacklinks(WikiAbstractionLayer wal, InterwikiBacklinksInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 117;

		public override string Name => "iwbacklinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "iwbl";
		#endregion

		#region Public Static Methods
		public static ListInterwikiBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is InterwikiBacklinksInput listInput
				? new ListInterwikiBacklinks(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(InterwikiBacklinksInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, InterwikiBacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("title", input.Title)
				.AddFlags("prop", input.Properties)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override InterwikiBacklinksItem GetItem(JToken result) => new InterwikiBacklinksItem(
			ns: (int)result.MustHave("ns"),
			title: result.MustHaveString("title"),
			pageId: (long)result.MustHave("pageid"),
			isRedirect: result["redirect"].GetBCBool(),
			iwPrefix: (string?)result["iwprefix"],
			iwTitle: (string?)result["iwtitle"]);
		#endregion
	}
}
