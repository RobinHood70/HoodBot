#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class ListUserContribs : ListModule<UserContributionsInput, UserContributionsItem>, IGeneratorModule
	{
		#region Fields
		private string continueName;
		#endregion

		#region Constructors
		public ListUserContribs(WikiAbstractionLayer wal, UserContributionsInput input)
			: base(wal, input)
		{
			var userList = input.Users.AsReadOnlyList();
			this.continueName = this.SiteVersion < 114 || (input.UserPrefix == null && userList.Count == 1 && this.SiteVersion < 123) ? "start" : "continue";
		}
		#endregion

		#region Protected Internal Override Properties
		public override string ContinueName => this.continueName;

		public override int MinimumVersion { get; } = 109;

		public override string Name { get; } = "usercontribs";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "uc";
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

		protected override UserContributionsItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new UserContributionsItem();
			item.GetWikiTitle(result);
			item.Flags =
				result.GetFlag("commenthidden", UserContributionFlags.CommentHidden) |
				result.GetFlag("minor", UserContributionFlags.Minor) |
				result.GetFlag("new", UserContributionFlags.New) |
				result.GetFlag("patrolled", UserContributionFlags.Patrolled) |
				result.GetFlag("suppressed", UserContributionFlags.Suppressed) |
				result.GetFlag("texthidden", UserContributionFlags.TextHidden) |
				result.GetFlag("top", UserContributionFlags.Top) |
				result.GetFlag("userhidden", UserContributionFlags.UserHidden);
			item.UserId = (long)result["userid"];
			item.User = (string)result["user"];
			item.RevisionId = (long?)result["revid"] ?? 0;
			item.ParentId = (long?)result["parentid"] ?? 0;
			item.Timestamp = (DateTime?)result["timestamp"];
			item.Comment = (string)result["comment"];
			item.ParsedComment = (string)result["parsedcomment"];
			item.Size = (int?)result["size"] ?? 0;
			item.SizeDifference = (int?)result["sizediff"] ?? 0;
			item.Tags = result.AsReadOnlyList<string>("tags");

			return item;
		}
		#endregion
	}
}