namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListPrefixSearch : ListModule<PrefixSearchInput, WikiTitleItem>, IGeneratorModule
	{
		#region Constructors
		public ListPrefixSearch(WikiAbstractionLayer wal, PrefixSearchInput input)
			: this(wal, input, null)
		{
		}

		public ListPrefixSearch(WikiAbstractionLayer wal, PrefixSearchInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 123;

		public override string Name => "prefixsearch";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ps";
		#endregion

		#region Public Static Methods
		public static ListPrefixSearch CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (PrefixSearchInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PrefixSearchInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.AddIfNotNull("search", input.Search)
				.Add("namespace", input.Namespaces)
				.Add("limit", this.Limit);
		}

		protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();
		#endregion
	}
}