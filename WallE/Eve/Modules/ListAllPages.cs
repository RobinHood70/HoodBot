#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ListAllPages : ListModule<AllPagesInput, WikiTitleItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllPages(WikiAbstractionLayer wal, AllPagesInput input)
			: this(wal, input, null)
		{
		}

		public ListAllPages(WikiAbstractionLayer wal, AllPagesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 0;

		public override string Name => "allpages";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ap";
		#endregion

		#region Public Static Methods
		public static ListAllPages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is AllPagesInput listInput
				? new ListAllPages(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(AllPagesInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllPagesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.AddFilterText("prfiltercascade", "cascading", "noncascading", input.FilterCascading)
				.AddFilterText("filterlanglinks", "withlanglinks", "withoutlanglinks", input.FilterLanguageLinks)
				.AddFilterText("filterredir", "redirects", "nonredirects", input.FilterRedirects)
				.AddIf("maxsize", input.MaximumSize, input.MaximumSize >= 0)
				.AddIf("minsize", input.MinimumSize, input.MinimumSize >= 0)
				.Add("namespace", input.Namespace)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("prlevel", input.ProtectionLevel)
				.AddIfNotNull("prtype", input.ProtectionType)
				.AddFilterText("prexpiry", "indefinite", "definite", input.FilterIndefinite)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();
		#endregion
	}
}
