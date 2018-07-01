#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public class ActionSetNotificationTimestamp : ActionModulePageSet<SetNotificationTimestampInput, SetNotificationTimestampItem>
	{
		#region Constructors
		public ActionSetNotificationTimestamp(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 120;

		public override string Name { get; } = "setnotificationtimestamp";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestPageSet(Request request, SetNotificationTimestampInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("entirewatchlist", input.EntireWatchlist)
				.Add("timestamp", input.Timestamp)
				.AddIfPositive("torevid", input.ToRevisionId)
				.AddIfPositive("newerthanrevid", input.NewerThanRevisionId)
				.AddHidden("token", input.Token);
		}

		protected override void DeserializePage(JToken result, SetNotificationTimestampItem page)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(page, nameof(page));
			page.Flags =
				result.GetFlag("invalid", SetNotificationTimestampFlags.Invalid) |
				result.GetFlag("missing", SetNotificationTimestampFlags.Missing) |
				result.GetFlag("known", SetNotificationTimestampFlags.Known) |
				result.GetFlag("notwatched", SetNotificationTimestampFlags.NotWatched);
			page.NotificationTimestamp = result["notificationtimestamp"].AsDate();
			page.RevisionId = (long?)result["revid"] ?? 0;
			this.Pages.Add(page);
		}

		protected override IReadOnlyList<SetNotificationTimestampItem> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));

			// If using entirewatchlist, return a single page with the notification timestamp and faked page data.
			if (result.Type == JTokenType.Object && result["notificationtimestamp"] != null)
			{
				var newPage = new SetNotificationTimestampItem()
				{
					Namespace = 0,
					NotificationTimestamp = result["notificationtimestamp"].AsDate(),
					Title = "::Entire Watchlist::",
				};
				this.Pages.Add(newPage);
				return null;
			}

			return base.DeserializeResult(result);
		}
		#endregion
	}
}
