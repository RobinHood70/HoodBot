namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListExtUrlUsage : ListModule<ExternalUrlUsageInput, ExternalUrlUsageItem>, IGeneratorModule
	{
		#region Constructors
		public ListExtUrlUsage(WikiAbstractionLayer wal, ExternalUrlUsageInput input)
			: this(wal, input, null)
		{
		}

		public ListExtUrlUsage(WikiAbstractionLayer wal, ExternalUrlUsageInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "exturlusage";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "eu";
		#endregion

		#region Public Static Methods
		public static ListExtUrlUsage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is ExternalUrlUsageInput listInput
				? new ListExtUrlUsage(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(ExternalUrlUsageInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ExternalUrlUsageInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.AddIfNotNull("protocol", input.Protocol)
				.AddIfNotNull("query", input.Query)
				.Add("namespace", input.Namespaces)
				.Add("expandurl", input.ExpandUrl)
				.Add("limit", this.Limit);
		}

		protected override ExternalUrlUsageItem? GetItem(JToken result) => result == null
			? null
			: new ExternalUrlUsageItem((int?)result["ns"], (string?)result["title"], (long?)result["pageid"] ?? 0, (string?)result["url"]);
		#endregion
	}
}