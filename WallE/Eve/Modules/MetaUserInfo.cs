#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	public class MetaUserInfo : QueryModule<UserInfoInput, UserInfoResult>
	{
		#region Constructors
		public MetaUserInfo(WikiAbstractionLayer wal, UserInfoInput input)
			: base(wal, input, new UserInfoResult())
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "userinfo";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "ui";

		protected override string ModuleType { get; } = "meta";
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

			// Not sure there's much value in this, given that the bot doesn't emit this header, nor does the API do anything with it even if it did, but it's here for completeness in case someone wants to add it and then intercept it again in a custom IClient.
			var token = result["acceptlang"];
			var acceptLanguages = new Dictionary<string, double>();
			if (token != null)
			{
				foreach (var lang in token)
				{
					acceptLanguages.Add((string)lang.AsBCContent("code"), (double)lang["q"]);
				}
			}

			output.AcceptLanguages = acceptLanguages;
			output.BlockExpiry = result["blockexpiry"].AsDate();
			output.BlockId = (long?)result["blockid"] ?? 0;
			output.BlockReason = (string)result["blockreason"];
			output.BlockTimestamp = (DateTime?)result["blockedtimestamp"];
			output.BlockedBy = (string)result["blockedby"];
			output.BlockedById = (long?)result["blockedbyid"] ?? 0;

			token = result["changeablegroups"];
			var changeableGroups = new ChangeableGroupsInfo()
			{
				Add = token.AsReadOnlyList<string>("add"),
				AddSelf = token.AsReadOnlyList<string>("add-self"),
				Remove = token.AsReadOnlyList<string>("remove"),
				RemoveSelf = token.AsReadOnlyList<string>("remove-self"),
			};
			output.ChangeableGroups = changeableGroups;
			output.EditCount = (long?)result["editcount"] ?? -1;
			output.Email = (string)result["email"];
			output.EmailAuthenticated = result["emailauthenticated"].AsDate();
			output.Flags =
				result.GetFlag("anon", UserInfoFlags.Anonymous) |
				result.GetFlag("messages", UserInfoFlags.HasMessage);
			output.Groups = result.AsReadOnlyList<string>("groups");
			output.Id = (long?)result["id"] ?? -1;
			output.ImplicitGroups = result.AsReadOnlyList<string>("implicitgroups");
			output.Name = (string)result["name"];

			var options = result["options"];
			output.Options = options.AsReadOnlyDictionary<string, object>();
			output.PreferencesToken = (string)result["preferencestoken"];

			var rateLimits = new Dictionary<string, RateLimitsItem>();
			token = result["ratelimits"];
			if (token != null)
			{
#pragma warning disable IDE0007 // Use implicit type
				foreach (JProperty entry in token)
#pragma warning restore IDE0007 // Use implicit type
				{
					rateLimits.Add(entry.Name, GetRateLimits(entry.Value));
				}
			}

			output.RateLimits = rateLimits;
			output.RealName = (string)result["realname"];
			output.RegistrationDate = result["registrationdate"].AsDate();
			output.Rights = result.AsReadOnlyList<string>("rights");
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

		private static RateLimitInfo GetRateLimit(JToken value)
		{
			if (value == null)
			{
				return null;
			}

			var rateLimit = new RateLimitInfo()
			{
				Hits = (int)value["hits"],
				Seconds = (int)value["seconds"],
			};
			return rateLimit;
		}
		#endregion
	}
}