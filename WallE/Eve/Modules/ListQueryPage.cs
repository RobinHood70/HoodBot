#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Properties.EveMessages;
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

		public ListQueryPage(WikiAbstractionLayer wal, QueryPageInput input, IPageSetGenerator pageSetGenerator)
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
		public static ListQueryPage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListQueryPage(wal, input as QueryPageInput, pageSetGenerator);
		#endregion

		#region Public Methods
		public QueryPageResult AsQueryPageResult() =>
			new QueryPageResult(this.Output)
			{
				Cached = this.cached,
				CachedTimestamp = this.cachedTimestamp,
				MaxResults = this.maxResults,
			};
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

		protected override void DeserializeResult(JToken result, IList<QueryPageItem> output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));
			if (result["disabled"] != null)
			{
				this.Wal.AddWarning("querypage-disabled", CurrentCulture(QueryPageDisabled, this.queryPage));
				return;
			}

			this.cached = result["cached"].AsBCBool();
			this.cachedTimestamp = (DateTime?)result["cachedtimestamp"];
			this.maxResults = (int?)result["maxresults"] ?? 0;

			base.DeserializeResult(result, output);
		}

		protected override QueryPageItem GetItem(JToken result) => result == null
			? null
			: new QueryPageItem()
			{
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
				Timestamp = (DateTime?)result["timestamp"],
				Value = (string)result["value"],
				DatabaseResults = result["databaseResults"].AsReadOnlyDictionary<string, string>()
			};
		#endregion
	}
}
