namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListLogEvents : ListModule<LogEventsInput, LogEventsItem>
	{
		#region Static Fields
		private static readonly HashSet<string> KnownProps = new(StringComparer.Ordinal)
		{
			"action",
			"actionhidden",
			"anon",
			"comment",
			"commenthidden",
			"logid",
			"logpage",
			"ns",
			"pageid",
			"parsedcomment",
			"suppressed",
			"tags",
			"timestamp",
			"title",
			"type",
			"user",
			"userhidden",
			"userid",
		};

		private readonly bool getUserId;
		#endregion

		#region Constructors
		public ListLogEvents(WikiAbstractionLayer wal, LogEventsInput input)
			: base(wal, input)
		{
			this.getUserId = input.Properties.HasAnyFlag(LogEventsProperties.UserId);
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name => "logevents";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "le";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LogEventsInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
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
			if (result == null)
			{
				return null;
			}

			// https://phabricator.wikimedia.org/rSVN112546 / https://github.com/wikimedia/mediawiki-core/commit/bf1e9d76ad4776ad5d9f6f5b662b418bbf4b1acd
			// Versions of MediaWiki prior to 1.24 have a bug where not all log entries are shown. This usually (always?) occurs when an item has dropped off RC or is deleted. When rcprop=tags is specified, however, empty tags entries with no other info may be emitted, so skip to next iteration if that appears to be the case.
			var tags = result["tags"];
			if (tags != null && new List<JToken>(result.Children()).Count == 1 && new List<JToken>(tags.Children()).Count == 0)
			{
				return null;
			}

			LogEventsItem item = new(
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				logPageId: (long?)result["logpage"] ?? 0,
				tags: tags.GetList<string>());
			result.ParseLogEvent(item, string.Empty, KnownProps, this.getUserId);

			return item;
		}
		#endregion
	}
}
