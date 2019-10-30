#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionFeedWatchlist : ActionModule<FeedWatchlistInput, CustomResult>
	{
		#region Constructors
		public ActionFeedWatchlist(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name => "feedwatchlist";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization => true;

		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FeedWatchlistInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("feedformat", input.FeedFormat)
				.AddIfPositive("hours", input.Hours > 72 ? 72 : input.Hours)
				.Add("linktosections", input.LinkToSections)
				.Add("allrev", input.AllRevisions)
				.AddIfNotNull("wlowner", input.Owner)
				.AddIfNotNull("wltoken", input.Token)
				.AddIfNotNull("wlexcludeuser", input.ExcludeUser)
				.Add("wltype", input.Types)
				.AddFilterPiped("wlshow", "minor", input.FilterMinor)
				.AddFilterPiped("wlshow", "bot", input.FilterBot)
				.AddFilterPiped("wlshow", "anon", input.FilterAnonymous)
				.AddFilterPiped("wlshow", "patrolled", input.FilterPatrolled)
				.AddFilterPiped("wlshow", "unread", input.FilterUnread)
				.AddIf("linktodiffs", input.LinkToDiffs, this.SiteVersion >= 117 && this.SiteVersion < 124);
		}

		protected override CustomResult DeserializeCustom(string? result) => new CustomResult(result);

		protected override CustomResult DeserializeResult(JToken result) => throw new NotSupportedException();
		#endregion
	}
}
