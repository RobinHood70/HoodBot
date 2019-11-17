#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class MetaUserInfo : QueryModule<UserInfoInput, UserInfoResult>
	{
		#region Constructors
		public MetaUserInfo(WikiAbstractionLayer wal, UserInfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "userinfo";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "meta";

		protected override string Prefix => "ui";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UserInfoInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(124, UserInfoProperties.UnreadCount)
				.FilterBefore(118, UserInfoProperties.ImplicitGroups | UserInfoProperties.RegistrationDate)
				.FilterBefore(117, UserInfoProperties.AcceptLang)
				.Value;
			request.AddFlags("prop", prop);
		}

		protected override void DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var token = result["changeablegroups"];
			var changeableGroups = token == null ? null : new ChangeableGroupsInfo(
				add: token.MustHave("add").GetList<string>(),
				addSelf: token.MustHave("add-self").GetList<string>(),
				remove: token.MustHave("remove").GetList<string>(),
				removeSelf: token.MustHave("remove-self").GetList<string>());

			var rateLimits = new Dictionary<string, RateLimitsItem?>();
			if (result["ratelimits"] is JToken rateLimitsNode)
			{
				foreach (var entry in rateLimitsNode.Children<JProperty>())
				{
					rateLimits.Add(entry.Name, GetRateLimits(entry.Value));
				}
			}

			this.Output = new UserInfoResult(
				id: (long)result.MustHave("id"),
				name: result.MustHaveString("name"),
				blockExpiry: result["blockexpiry"].GetNullableDate(),
				blockId: (long?)result["blockid"] ?? 0,
				blockReason: (string?)result["blockreason"],
				blockTimestamp: (DateTime?)result["blockedtimestamp"],
				blockedBy: (string?)result["blockedby"],
				blockedById: (long?)result["blockedbyid"] ?? 0,
				changeableGroups: changeableGroups,
				editCount: (long?)result["editcount"] ?? -1,
				email: (string?)result["email"],
				emailAuthenticated: result["emailauthenticated"].GetNullableDate(),
				flags: result.GetFlags(
					("anon", UserInfoFlags.Anonymous),
					("messages", UserInfoFlags.HasMessage)),
				groups: result["groups"].GetList<string>(),
				implicitGroups: result["implicitgroups"].GetList<string>(),
				options: result["options"].GetStringDictionary<object>(),
				preferencesToken: (string?)result["preferencestoken"],
				rateLimits: rateLimits,
				realName: (string?)result["realname"],
				registrationDate: result["registrationdate"].GetNullableDate(),
				rights: result["rights"].GetList<string>(),
				unreadText: (string?)result["unreadcount"]);
		}
		#endregion

		#region Private Methods
		private static RateLimitsItem? GetRateLimits(JToken value) => value == null
			? null
			: new RateLimitsItem(
				anonymous: GetRateLimit(value["anon"]),
				ip: GetRateLimit(value["ip"]),
				newbie: GetRateLimit(value["newbie"]),
				subnet: GetRateLimit(value["subnet"]),
				user: GetRateLimit(value["user"]));

		private static RateLimitInfo? GetRateLimit(JToken? value) => value == null
			? null
			: new RateLimitInfo(
				hits: (int)value.MustHave("hits"),
				seconds: (int)value.MustHave("seconds"));
		#endregion
	}
}