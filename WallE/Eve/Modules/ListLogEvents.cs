#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListLogEvents : ListModule<LogEventsInput, LogEventsItem>
	{
		#region Static Fields
		private static readonly HashSet<string> KnownProps = new HashSet<string>
		{
			"action", "actionhidden", "anon", "comment", "commenthidden", "logid", "logpage", "ns", "pageid", "parsedcomment", "suppressed", "tags", "timestamp", "title", "type", "user", "userhidden", "userid",
		};

		private readonly bool getUserId;
		#endregion

		#region Constructors
		public ListLogEvents(WikiAbstractionLayer wal, LogEventsInput input)
			: base(wal, input) => this.getUserId = input.Properties.HasFlag(LogEventsProperties.UserId);
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 109;

		public override string Name { get; } = "logevents";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "le";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LogEventsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.AddIfNotNull("type", input.Type)
				.AddIfNotNull("action", input.Action)
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("dir", "newer", input.Start < input.End || input.SortDescending)
				.AddIfNotNull("user", input.User)
				.AddIfNotNull("title", input.Title)
				.Add("namespace", input.Namespace)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("tag", input.Tag)
				.Add("limit", this.Limit);
		}

		protected override LogEventsItem? GetItem(JToken result)
		{
			// https://github.com/wikimedia/mediawiki-core/commit/bf1e9d76ad4776ad5d9f6f5b662b418bbf4b1acd
			// Versions of MediaWiki prior to 1.24 have a bug where not all log entries are shown. This usually (always?) occurs when an item has dropped off RC or is deleted. When rcprop=tags is specified, however, empty tags entries with no other info may be emitted, so skip to next iteration if that appears to be the case. Uses "type" for validity-checking, since that should always be emitted, no matter what.
			if (result == null || result["type"] == null)
			{
				return null;
			}

			var item = new LogEventsItem((int)result.NotNull("ns"), result.StringNotNull("title"), (long?)result["pageid"] ?? 0);
			var logType = (string?)result["type"];
			var logAction = (string?)result["action"];
			result.ParseLogEvent(item, logType, logAction, KnownProps, this.getUserId);
			item.LogPageId = (long?)result["logpage"] ?? 0;
			item.Tags = result["tags"].AsReadOnlyList<string>();

			return item;
		}
		#endregion
	}
}
