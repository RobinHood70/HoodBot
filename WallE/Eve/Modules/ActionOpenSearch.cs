﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ActionOpenSearch(WikiAbstractionLayer wal) : ActionModule<OpenSearchInput, IReadOnlyList<OpenSearchItem>>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 125;

	public override string Name => "opensearch";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Get;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, OpenSearchInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
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
		ArgumentNullException.ThrowIfNull(result);

		// 0th term is the search term, so we ignore that.
		if (result is JArray array)
		{
			if (array.Count == 0)
			{
				return [];
			}

			if (array.Count == 4 && array[1] is JArray titles && array[2] is JArray descriptions && array[3] is JArray urls)
			{
				List<OpenSearchItem> output = new(titles.Count);
				for (var i = 0; i < titles.Count; i++)
				{
					output.Add(new OpenSearchItem(
						title: (string?)titles[i],
						description: (string?)descriptions[i],
						uri: (Uri?)urls[i]));
				}

				return output;
			}
		}

		return base.DeserializeCustom(result);
	}

	protected override IReadOnlyList<OpenSearchItem> DeserializeResult(JToken? result) => throw new NotSupportedException();
	#endregion
}