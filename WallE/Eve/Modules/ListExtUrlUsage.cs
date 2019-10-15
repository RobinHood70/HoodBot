#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListExtUrlUsage : ListModule<ExternalUrlUsageInput, ExternalUrlUsageItem>, IGeneratorModule
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
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "exturlusage";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "eu";
		#endregion

		#region Public Static Methods
		public static ListExtUrlUsage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListExtUrlUsage(wal, input as ExternalUrlUsageInput, pageSetGenerator);
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