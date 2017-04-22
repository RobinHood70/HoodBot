#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	public class ActionFeedWatchlist : ActionModule<FeedWatchlistInput, CustomResult>
	{
		#region Constructors
		public ActionFeedWatchlist(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 109;

		public override string Name { get; } = "feedwatchlist";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization { get; } = true;

		protected override RequestType RequestType { get; } = RequestType.Get;
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
				.AddFilterOptionPiped("wlshow", "minor", input.FilterMinor)
				.AddFilterOptionPiped("wlshow", "bot", input.FilterBot)
				.AddFilterOptionPiped("wlshow", "anon", input.FilterAnonymous)
				.AddFilterOptionPiped("wlshow", "patrolled", input.FilterPatrolled)
				.AddFilterOptionPiped("wlshow", "unread", input.FilterUnread)
				.AddIf("linktodiffs", input.LinkToDiffs, this.SiteVersion >= 117 && this.SiteVersion < 124);
		}

		protected override CustomResult DeserializeCustom(string result) => new CustomResult(result);

		protected override CustomResult DeserializeResult(JToken result) => throw new NotSupportedException();
		#endregion
	}
}
