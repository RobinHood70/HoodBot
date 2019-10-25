#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListWatchlist : ListModule<WatchlistInput, WatchlistItem>, IGeneratorModule
	{
		#region Static Fields
		private static readonly HashSet<string> KnownProps = new HashSet<string>
		{
			"actionhidden", "anon", "bot", "comment", "commenthidden", "logaction", "logid", "logtype", "minor", "new", "newlen", "notificationtimestamp", "ns", "old_revid", "oldlen", "pageid", "parsedcomment", "patrolled", "revid", "suppressed", "timestamp", "title", "type", "unpatrolled", "user", "userhidden", "userid",
		};
		#endregion

		#region Fields
		private readonly string continueName;
		#endregion

		#region Constructors
		public ListWatchlist(WikiAbstractionLayer wal, WatchlistInput input)
			: this(wal, input, null)
		{
		}

		public ListWatchlist(WikiAbstractionLayer wal, WatchlistInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator) => this.continueName = this.SiteVersion < 123 ? "start" : "continue";
		#endregion

		#region Public Override Properties
		public override string ContinueName => this.continueName;

		public override int MinimumVersion { get; } = 109;

		public override string Name { get; } = "watchlist";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "wl";
		#endregion

		#region Public Static Methods
		public static ListWatchlist CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is WatchlistInput listInput
				? new ListWatchlist(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(WatchlistInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, WatchlistInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(117, WatchlistProperties.UserId)
				.FilterBefore(118, WatchlistProperties.LogInfo)
				.Value;
			request
				.Add("allrev", input.AllRevisions)
				.Add("start", input.Start)
				.Add("end", input.End)
				.Add("namespace", input.Namespaces)
				.AddIfNotNull(input.ExcludeUser ? "excludeuser" : "user", input.User)
				.AddIf("dir", "newer", input.SortAscending)
				.AddFlags("prop", prop)
				.AddFilterPiped("show", "minor", input.FilterMinor)
				.AddFilterPiped("show", "bot", input.FilterBot)
				.AddFilterPiped("show", "anon", input.FilterAnonymous)
				.AddFilterPiped("show", "patrolled", input.FilterPatrolled)
				.AddFilterPipedIf("show", "unread", input.FilterUnread, this.SiteVersion >= 124)
				.AddIf("type", input.Type, this.SiteVersion >= 122)
				.AddIfNotNull("owner", input.Owner)
				.AddHiddenIfNotNull("token", input.Token)
				.Add("limit", this.Limit);
		}

		protected override WatchlistItem? GetItem(JToken result)
		{
			// Same bug as in ListLogEvents.cs but a slightly different manifestation. It appears only when LogInfo is requested and since there are no tags here, the result node will be a null-valued node.
			if (result == null || result.Type == JTokenType.Null)
			{
				return null;
			}

			var item = new WatchlistItem(
				watchlistType: result.MustHaveString("type"),
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				flags: result.GetFlags(
					("bot", WatchlistFlags.Bot),
					("minor", WatchlistFlags.Minor),
					("new", WatchlistFlags.New),
					("patrolled", WatchlistFlags.Patrolled),
					("unpatrolled", WatchlistFlags.Unpatrolled)),
				newLength: (int?)result["newlen"] ?? -1,
				oldLength: (int?)result["oldlen"] ?? -1,
				oldRevisionId: (long?)result["old_revid"] ?? 0,
				revisionId: (long?)result["revid"] ?? 0);
			result.ParseLogEvent(item, "log", KnownProps, false);

			return item;
		}
		#endregion
	}
}
