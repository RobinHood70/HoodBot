﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionUndelete(WikiAbstractionLayer wal) : ActionModule<UndeleteInput, UndeleteResult>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 112;

	public override string Name => "undelete";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, UndeleteInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("title", input.Title)
			.AddIfNotNull("reason", input.Reason)
			.AddIf("tags", input.Tags, this.SiteVersion >= 125)
			.Add("timestamps", input.Timestamps)
			.AddIf("fileids", input.FileIds, this.SiteVersion >= 124)
			.AddIfPositive("watchlist", input.Watchlist)
			.AddHidden("token", input.Token);
	}

	protected override UndeleteResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		var title = result.MustHaveString("title");
		return new UndeleteResult(
			ns: this.FindRequiredNamespace(title),
			title: title,
			revisions: (int)result.MustHave("revisions"),
			fileVersions: (int)result.MustHave("fileversions"),
			reason: result.MustHaveString("reason"));
	}
	#endregion
}