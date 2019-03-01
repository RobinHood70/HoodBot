#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class MetaUserInfo : QueryModule<UserInfoInput, UserInfoResult>
	{
		#region Constructors
		public MetaUserInfo(WikiAbstractionLayer wal, UserInfoInput input)
			: base(wal, input, new UserInfoResult(), null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "userinfo";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "meta";

		protected override string Prefix { get; } = "ui";
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

		protected override void DeserializeResult(JToken result, UserInfoResult output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));

			output.BlockExpiry = result["blockexpiry"].AsDate();
			output.BlockId = (long?)result["blockid"] ?? 0;
			output.BlockReason = (string)result["blockreason"];
			output.BlockTimestamp = (DateTime?)result["blockedtimestamp"];
			output.BlockedBy = (string)result["blockedby"];
			output.BlockedById = (long?)result["blockedbyid"] ?? 0;

			var token = result["changeablegroups"];
			var changeableGroups = new ChangeableGroupsInfo()
			{
				Add = token?["add"].AsReadOnlyList<string>(),
				AddSelf = token?["add-self"].AsReadOnlyList<string>(),
				Remove = token?["remove"].AsReadOnlyList<string>(),
				RemoveSelf = token?["remove-self"].AsReadOnlyList<string>(),
			};
			output.ChangeableGroups = changeableGroups;
			output.EditCount = (long?)result["editcount"] ?? -1;
			output.Email = (string)result["email"];
			output.EmailAuthenticated = result["emailauthenticated"].AsDate();
			output.Flags =
				result.GetFlag("anon", UserInfoFlags.Anonymous) |
				result.GetFlag("messages", UserInfoFlags.HasMessage);
			output.Groups = result["groups"].AsReadOnlyList<string>();
			output.Id = (long?)result["id"] ?? -1;
			output.ImplicitGroups = result["implicitgroups"].AsReadOnlyList<string>();
			output.Name = (string)result["name"];

			var options = result["options"];
			output.Options = options.AsReadOnlyDictionary<string, object>();
			output.PreferencesToken = (string)result["preferencestoken"];

			var rateLimits = new Dictionary<string, RateLimitsItem>();
			token = result["ratelimits"];
			if (token != null)
			{
				foreach (var entry in token.Children<JProperty>())
				{
					rateLimits.Add(entry.Name, GetRateLimits(entry.Value));
				}
			}

			output.RateLimits = rateLimits;
			output.RealName = (string)result["realname"];
			output.RegistrationDate = result["registrationdate"].AsDate();
			output.Rights = result["rights"].AsReadOnlyList<string>();
			var unreadCount = (string)result["unreadcount"] ?? "-1";
			if (unreadCount.EndsWith("+", StringComparison.Ordinal))
			{
				unreadCount = unreadCount.Substring(0, unreadCount.Length - 1);
			}

			output.UnreadCount = int.Parse(unreadCount, CultureInfo.InvariantCulture);
		}
		#endregion

		#region Private Methods
		private static RateLimitsItem GetRateLimits(JToken value)
		{
			var rateLimits = new RateLimitsItem()
			{
				Anonymous = GetRateLimit(value["anon"]),
				IP = GetRateLimit(value["ip"]),
				Newbie = GetRateLimit(value["newbie"]),
				Subnet = GetRateLimit(value["subnet"]),
				User = GetRateLimit(value["user"]),
			};
			return rateLimits;
		}

		private static RateLimitInfo GetRateLimit(JToken value) => value == null
			? null
			: new RateLimitInfo()
			{
				Hits = (int)value["hits"],
				Seconds = (int)value["seconds"],
			};
		#endregion
	}
}