namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionLogin : ActionModule<LoginInput, LoginResult>
	{
		#region Constructors
		public ActionLogin(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 0;

		public override string Name => "login";

		public override string Prefix => "lg";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LoginInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			request
				.AddIfNotNull("name", input.UserName)
				.AddHiddenIfNotNull("password", input.Password)
				.AddIfNotNull("domain", input.Domain)
				.AddHiddenIfNotNull("token", input.Token);
		}

		protected override LoginResult DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			return new LoginResult(
				result: result.MustHaveString("result"),
				reason: (string?)result["reason"],
				user: (string?)result["lgusername"],
				userId: (long?)result["lguserid"] ?? -1,
				token: (string?)result["token"],
				waitTime: TimeSpan.FromSeconds((int?)result["wait"] ?? 0));
		}

		protected override bool HandleWarning(string from, string text)
		{
			ArgumentNullException.ThrowIfNull(text);
			return
				text.StartsWith("Main-account login", StringComparison.Ordinal) ||
				base.HandleWarning(from, text);
		}
		#endregion
	}
}