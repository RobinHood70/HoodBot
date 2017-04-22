#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListRecentChanges : ListModule<RecentChangesInput, RecentChangesItem>, IGeneratorModule
	{
		#region Static Fields
		private static HashSet<string> knownProps = new HashSet<string>
		{
			"actionhidden", "anon", "bot", "comment", "commenthidden", "logaction", "logid", "logtype", "minor", "new", "newlen", "ns", "old_revid", "oldlen", "pageid", "parsedcomment", "patroltoken", "patrolled", "rcid", "redirect", "revid", "suppressed", "tags", "timestamp", "title", "type", "user", "userhidden", "userid",
		};
		#endregion

		#region Constructors
		public ListRecentChanges(WikiAbstractionLayer wal, RecentChangesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Override Properties
		public override int MinimumVersion { get; } = 109;

		public override string Name { get; } = "recentchanges";

		protected override string BasePrefix { get; } = "rc";
		#endregion

		#region Public Static Methods
		public static ListRecentChanges CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListRecentChanges(wal, input as RecentChangesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RecentChangesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("dir", "newer", input.Start < input.End || input.SortAscending)
				.Add("namespace", input.Namespace)
				.AddIfNotNull(input.ExcludeUser ? "excludeuser" : "user", input.User)
				.AddIfNotNull("tag", input.Tag)
				.AddFlags("type", input.Types)
				.AddFilterOptionPiped("show", "anon", input.FilterAnonymous)
				.AddFilterOptionPiped("show", "bot", input.FilterBot)
				.AddFilterOptionPiped("show", "minor", input.FilterMinor)
				.AddFilterOptionPiped("show", "patrolled", input.FilterPatrolled)
				.AddFilterOptionPiped("show", "redirect", input.FilterRedirects)
				.AddFlags("prop", input.Properties)
				.AddIf("token", TokensInput.Patrol, input.GetPatrolToken && this.SiteVersion < 124)
				.Add("toponly", input.TopOnly)
				.Add("limit", this.Limit);
		}

		protected override RecentChangesItem GetItem(JToken result)
		{
			// Same bug as ListLogEvents
			if (result == null || result["type"] == null)
			{
				return null;
			}

			var item = new RecentChangesItem();
			var logType = (string)result["logtype"];
			var logAction = (string)result["logaction"];
			result.ParseLogEvent(item, logType, logAction, knownProps, false);
			item.Tags = result.AsReadOnlyList<string>("tags");
			item.RecentChangeType = (string)result["type"];
			item.Id = (long?)result["rcid"] ?? 0;
			item.Flags =
				result.GetFlag("bot", RecentChangeFlags.Bot) |
				result.GetFlag("minor", RecentChangeFlags.Minor) |
				result.GetFlag("new", RecentChangeFlags.New) |
				result.GetFlag("redirect", RecentChangeFlags.Redirect);
			item.OldLength = (int?)result["oldlen"] ?? 0;
			item.NewLength = (int?)result["newlen"] ?? 0;
			item.PatrolToken = (string)result["patroltoken"];
			item.RevisionId = (long?)result["revid"] ?? 0;
			item.OldRevisionId = (long?)result["old_revid"] ?? 0;

			return item;
		}
		#endregion
	}
}