﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionWatch(WikiAbstractionLayer wal) : ActionModulePageSet<WatchInput, WatchItem>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 114;

	public override string Name => "watch";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestPageSet(Request request, WatchInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		if (input.Values != null && this.SiteVersion < 123)
		{
			Debug.Assert(input.ListType == ListType.Titles && input.Values.Count == 1 && this.Generator == null, "Incorrect values sent to < MW 1.23 Watch");
			request.Remove("titles");
			request.Remove("converttitles");
			request.Remove("redirects");
			request.Add("title", input.Values[0]);
		}

		request
			.Add("unwatch", input.Unwatch)
			.AddHidden("token", input.Token);
	}

	protected override WatchItem GetItem(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		var title = result.MustHaveString("title");
		return new WatchItem(
			ns: (int?)result["ns"] ?? this.FindRequiredNamespace(title),
			title: title,
			pageId: (long?)result["pageid"] ?? 0,
			flags: result.GetFlags(
				("missing", WatchFlags.Missing),
				("unwatched", WatchFlags.Unwatched),
				("watched", WatchFlags.Watched)));
	}
	#endregion
}