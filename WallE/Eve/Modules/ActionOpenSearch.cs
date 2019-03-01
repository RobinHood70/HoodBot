#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionOpenSearch : ActionModule<OpenSearchInput, IReadOnlyList<OpenSearchItem>>
	{
		#region Constructors
		public ActionOpenSearch(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "opensearch";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, OpenSearchInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("search", input.Search)
				.Add("namespace", input.Namespaces)
				.AddIfPositive("limit", input.Limit)
				.AddIfPositive("profile", input.Profile)
				.Add("suggest", input.Suggest)
				.Add("redirects", input.Redirects);
		}

		protected override IReadOnlyList<OpenSearchItem> DeserializeCustom(JToken result)
		{
			ThrowNull(result, nameof(result));
			var api = result.First;
			if ((string)api != "api")
			{
				base.DeserializeCustom(result);
			}

			var titles = (JArray)api.Next;
			var descriptions = (JArray)titles.Next;
			var urls = (JArray)descriptions.Next;

			var output = new List<OpenSearchItem>(titles.Count);
			for (var i = 0; i < titles.Count; i++)
			{
				var search = new OpenSearchItem()
				{
					Title = (string)titles[i],
					Description = (string)descriptions[i],
					Uri = (Uri)urls[i],
				};
				output.Add(search);
			}

			return output;
		}

		protected override IReadOnlyList<OpenSearchItem> DeserializeResult(JToken result) => throw new NotSupportedException();
		#endregion
	}
}
