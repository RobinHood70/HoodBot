#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllRevisions : ListModule<AllRevisionsInput, AllRevisionsItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input)
			: this(wal, input, null)
		{
		}

		public ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "allrevisions";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "arv";
		#endregion

		#region Public Static Methods
		public static ListAllRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is AllRevisionsInput listInput
				? new ListAllRevisions(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(AllRevisionsInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllRevisionsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.BuildRevisions(input, this.SiteVersion)
				.Add("namespace", input.Namespaces)
				.Add("generatetitles", input.GenerateTitles);
		}

		protected override AllRevisionsItem? GetItem(JToken result) => result == null
			? null
			: new AllRevisionsItem(
			ns: (int)result.MustHave("ns"),
			title: result.MustHaveString("title"),
			pageId: (long)result.MustHave("pageid"),
			revisions: result.GetRevisions());
		#endregion
	}
}
