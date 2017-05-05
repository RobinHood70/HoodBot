#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static ProjectGlobals;
	using static WikiCommon.Globals;

	public class ActionLogin : ActionModule<LoginInput, LoginResult>
	{
		#region Constructors
		public ActionLogin(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "login";

		public override string Prefix { get; } = "lg";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;

		protected override StopCheckMethods StopMethods { get; } = StopCheckMethods.None;
		#endregion

		#region Protected Override Methods
		protected override void AddWarning(string from, string text)
		{
			ThrowNullOrWhiteSpace(text, nameof(text));
			if (!text.StartsWith("Main-account login", StringComparison.Ordinal))
			{
				base.AddWarning(from, text);
			}
		}

		protected override void BuildRequestLocal(Request request, LoginInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("name", input.UserName)
				.AddHiddenIfNotNull("password", input.Password)
				.AddIfNotNull("domain", input.Domain)
				.AddHiddenIfNotNull("token", input.Token);
		}

		protected override LoginResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new LoginResult()
			{
				Result = (string)result["result"],
				Reason = (string)result["reason"],
			};
			switch (output.Result)
			{
				case "NeedToken":
					output.Token = (string)result["token"];
					break;
				case "Success":
					output.UserId = (long?)result["lguserid"] ?? -1;
					output.User = (string)result["lgusername"];
					break;
				case "Throttled":
					output.WaitTime = TimeSpan.FromSeconds((int?)result["wait"] ?? 0);
					break;
			}

			return output;
		}
		#endregion
	}
}