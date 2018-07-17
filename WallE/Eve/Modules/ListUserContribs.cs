#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListUserContribs : ListModule<UserContributionsInput, UserContributionsItem>
	{
		#region Fields
		private readonly string continueName;
		#endregion

		#region Constructors
		public ListUserContribs(WikiAbstractionLayer wal, UserContributionsInput input)
			: this(wal, input, null)
		{
		}

		public ListUserContribs(WikiAbstractionLayer wal, UserContributionsInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator) => this.continueName = this.SiteVersion < 114 || (input.UserPrefix == null && input.Users.HasItems() && this.SiteVersion < 123) ? "start" : "continue";
		#endregion

		#region Public Override Properties
		public override string ContinueName => this.continueName;

		public override int MinimumVersion { get; } = 109;

		public override string Name { get; } = "usercontribs";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "uc";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UserContributionsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(120, UserContribsProperties.SizeDiff)
				.Value;
			request
				.Add("start", input.Start)
				.Add("end", input.End)
				.Add("user", input.Users)
				.AddIfNotNull("userprefix", input.UserPrefix)
				.AddIf("dir", "newer", input.SortAscending)
				.Add("namespace", input.Namespaces)
				.AddFlags("prop", prop)
				.AddFilterPiped("show", "minor", input.FilterMinor)
				.AddFilterPipedIf("show", "new", input.FilterNew, this.SiteVersion >= 123)
				.AddFilterPiped("show", "patrolled", input.FilterPatrolled)
				.AddFilterPipedIf("show", "top", input.FilterTop, this.SiteVersion >= 123)
				.AddIfNotNull("tag", input.Tag)
				.Add("toponly", input.FilterTop == Filter.Only && this.SiteVersion >= 118 && this.SiteVersion < 123)
				.Add("limit", this.Limit);
		}

		protected override UserContributionsItem GetItem(JToken result) => result == null
			? null
			: new UserContributionsItem
			{
				Flags =
					result.GetFlag("commenthidden", UserContributionFlags.CommentHidden) |
					result.GetFlag("minor", UserContributionFlags.Minor) |
					result.GetFlag("new", UserContributionFlags.New) |
					result.GetFlag("patrolled", UserContributionFlags.Patrolled) |
					result.GetFlag("suppressed", UserContributionFlags.Suppressed) |
					result.GetFlag("texthidden", UserContributionFlags.TextHidden) |
					result.GetFlag("top", UserContributionFlags.Top) |
					result.GetFlag("userhidden", UserContributionFlags.UserHidden),
				UserId = (long)result["userid"],
				User = (string)result["user"],
				RevisionId = (long?)result["revid"] ?? 0,
				ParentId = (long?)result["parentid"] ?? 0,
				Timestamp = (DateTime?)result["timestamp"],
				Comment = (string)result["comment"],
				ParsedComment = (string)result["parsedcomment"],
				Size = (int?)result["size"] ?? 0,
				SizeDifference = (int?)result["sizediff"] ?? 0,
				Tags = result["tags"].AsReadOnlyList<string>()
			}.GetWikiTitle(result);
		#endregion
	}
}