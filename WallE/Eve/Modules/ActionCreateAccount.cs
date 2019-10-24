#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionCreateAccount : ActionModuleValued<CreateAccountInput, CreateAccountResult>
	{
		#region Constructors
		public ActionCreateAccount(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 121;

		public override string Name { get; } = "createaccount";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CreateAccountInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("name", input.Name)
				.AddIfNotNull("password", input.Password)
				.AddIfNotNull("domain", input.Domain)
				.AddIfNotNull("email", input.Email)
				.AddIfNotNull("realname", input.RealName)
				.Add("mailpassword", input.MailPassword)
				.AddIfNotNull("reason", input.Reason)
				.AddIfNotNull("language", input.Language)
				.AddHiddenIfNotNull("token", input.Token)
				.AddHidden(input.CaptchaSolution);
		}

		protected override CreateAccountResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var resultText = result.MustHaveString("result");
			resultText = resultText == "needtoken" ? "NeedToken" : resultText.UpperFirst();
			return new CreateAccountResult(
				result: resultText,
				captchaData: result["captcha"].ToStringDictionary<string>(),
				token: (string?)result["token"],
				userId: (long?)result["userid"] ?? 0,
				userName: (string?)result["username"],
				warnings: result["warnings"].GetWarnings());
		}
		#endregion
	}
}