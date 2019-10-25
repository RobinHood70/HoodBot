#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListQueryPage : ListModule<QueryPageInput, QueryPageItem>, IGeneratorModule
	{
		#region Fields
		private readonly string queryPage;
		private bool cached;
		private DateTime? cachedTimestamp;
		private int maxResults;
		#endregion

		#region Constructors
		public ListQueryPage(WikiAbstractionLayer wal, QueryPageInput input)
			: this(wal, input, null)
		{
		}

		public ListQueryPage(WikiAbstractionLayer wal, QueryPageInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator) => this.queryPage = input.Page;
		#endregion

		#region Public Override Properties
		public override string ContinueName { get; } = "offset";

		public override int MinimumVersion { get; } = 118;

		public override string Name { get; } = "querypage";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "qp";
		#endregion

		#region Public Static Methods
		public static ListQueryPage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is QueryPageInput listInput
				? new ListQueryPage(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(QueryPageInput), input.GetType().Name);
		#endregion

		#region Public Methods
		public QueryPageResult AsQueryPageResult() => new QueryPageResult(
			list: this.Output ?? Array.Empty<QueryPageItem>(),
			cached: this.cached,
			cachedTimestamp: this.cachedTimestamp,
			maxResults: this.maxResults);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, QueryPageInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("page", input.Page)
				.Add("limit", this.Limit);
			if (input.Parameters != null)
			{
				request.Prefix = string.Empty;
				foreach (var parameter in input.Parameters)
				{
					request.Add(parameter.Key, parameter.Value);
				}
			}
		}

		protected override void DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			if (result["disabled"] != null)
			{
				this.Wal.AddWarning("querypage-disabled", CurrentCulture(EveMessages.QueryPageDisabled, this.queryPage));
				return;
			}

			this.cached = result["cached"].ToBCBool();
			this.cachedTimestamp = (DateTime?)result["cachedtimestamp"];
			this.maxResults = (int?)result["maxresults"] ?? 0;
			base.DeserializeResult(result.MustHave("results"));
		}

		protected override QueryPageItem? GetItem(JToken result) => result == null
			? null
			: new QueryPageItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				value: (long)result.MustHave("value"),
				databaseResult: result["databaseResult"]?.ToStringDictionary<object?>(),
				timestamp: (DateTime?)result["timestamp"]);
		#endregion
	}
}