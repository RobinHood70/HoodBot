namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class MetaUserInfo : QueryModule<UserInfoInput, UserInfoResult>
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
			input.ThrowNull();
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(124, UserInfoProperties.UnreadCount)
				.FilterBefore(118, UserInfoProperties.ImplicitGroups | UserInfoProperties.RegistrationDate)
				.FilterBefore(117, UserInfoProperties.AcceptLang)
				.Value;
			request.NotNull().AddFlags("prop", prop);
		}

		protected override void DeserializeResult(JToken? result)
		{
			result.ThrowNull();
			var token = result["changeablegroups"];
			var changeableGroups = token == null ? null : new ChangeableGroupsInfo(
				add: token.MustHave("add").GetList<string>(),
				addSelf: token.MustHave("add-self").GetList<string>(),
				remove: token.MustHave("remove").GetList<string>(),
				removeSelf: token.MustHave("remove-self").GetList<string>());

			Dictionary<string, RateLimitsItem?> rateLimits = new(System.StringComparer.Ordinal);
			if (result["ratelimits"] is JToken rateLimitsNode)
			{
				foreach (var entry in rateLimitsNode.Children<JProperty>())
				{
					rateLimits.Add(entry.Name, GetRateLimits(entry.Value));
				}
			}

			this.Output = new UserInfoResult(
				baseUser: result.GetUser(),
				changeableGroups: changeableGroups,
				email: (string?)result["email"],
				emailAuthenticated: result["emailauthenticated"].GetNullableDate(),
				flags: result.GetFlags(
					("anon", UserInfoFlags.Anonymous),
					("messages", UserInfoFlags.HasMessage)),
				options: result["options"].GetStringDictionary<object>(),
				preferencesToken: (string?)result["preferencestoken"],
				rateLimits: rateLimits,
				realName: (string?)result["realname"],
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

		[return: NotNullIfNotNull("value")]
		private static RateLimitInfo? GetRateLimit(JToken? value) => value == null
			? null
			: new RateLimitInfo(
				hits: (int)value.MustHave("hits"),
				seconds: (int)value.MustHave("seconds"));
		#endregion
	}
}