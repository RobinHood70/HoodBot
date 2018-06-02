#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static Properties.EveMessages;
	using static WikiCommon.Globals;

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
			: base(wal, input) => this.queryPage = input.Page;
		#endregion

		#region Protected Internal Override Properties
		public override string ContinueName { get; } = "offset";

		public override int MinimumVersion { get; } = 118;

		public override string Name { get; } = "querypage";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "qp";
		#endregion

		#region Public Static Methods
		public static ListQueryPage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListQueryPage(wal, input as QueryPageInput);
		#endregion

		#region Public Methods
		public QueryPageResult AsQueryPageTitleCollection() =>
			new QueryPageResult(this.Output)
			{
				Cached = this.cached,
				CachedTimestamp = this.cachedTimestamp,
				MaxResults = this.maxResults,
			};
		#endregion

		#region Public Override Methods
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

		protected override QueryPageItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new QueryPageItem()
			{
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
				Timestamp = (DateTime?)result["timestamp"],
				Value = (string)result["value"],
				DatabaseResults = result.AsReadOnlyDictionary<string, string>("databaseResults"),
			};
			return item;
		}
		#endregion
	}
}
