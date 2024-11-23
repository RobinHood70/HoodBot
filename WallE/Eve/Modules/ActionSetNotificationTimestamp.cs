namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionSetNotificationTimestamp(WikiAbstractionLayer wal) : ActionModulePageSet<SetNotificationTimestampInput, SetNotificationTimestampItem>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 120;

	public override string Name => "setnotificationtimestamp";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestPageSet(Request request, SetNotificationTimestampInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("entirewatchlist", input.EntireWatchlist)
			.Add("timestamp", input.Timestamp)
			.AddIfPositive("torevid", input.ToRevisionId)
			.AddIfPositive("newerthanrevid", input.NewerThanRevisionId)
			.AddHidden("token", input.Token);
	}

	protected override SetNotificationTimestampItem GetItem(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new SetNotificationTimestampItem(
			ns: (int)result.MustHave("ns"),
			title: result.MustHaveString("title"),
			pageId: (long?)result["pageid"] ?? 0,
			flags: result.GetFlags(
				("invalid", SetNotificationTimestampFlags.Invalid),
				("missing", SetNotificationTimestampFlags.Missing),
				("known", SetNotificationTimestampFlags.Known),
				("notwatched", SetNotificationTimestampFlags.NotWatched)),
			notificationTimestamp: result["notificationtimestamp"].GetNullableDate(),
			revId: (long?)result["revid"] ?? 0);
	}

	protected override void DeserializeResult(JToken result, IList<SetNotificationTimestampItem> pages)
	{
		ArgumentNullException.ThrowIfNull(result);

		// If using entirewatchlist, return a single page with the notification timestamp and faked page data.
		if (result.Type == JTokenType.Object && result["notificationtimestamp"] != null)
		{
			pages.Add(new SetNotificationTimestampItem(
				ns: 0,
				title: "::Entire Watchlist::",
				pageId: 0,
				flags: SetNotificationTimestampFlags.None,
				notificationTimestamp: result["notificationtimestamp"].GetNullableDate(),
				revId: 0));
		}
		else
		{
			base.DeserializeResult(result, pages);
		}
	}
	#endregion
}