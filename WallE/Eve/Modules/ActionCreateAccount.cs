namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Globalization;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionCreateAccount(WikiAbstractionLayer wal) : ActionModule<CreateAccountInput, CreateAccountResult>(wal)
	{
		#region Public Override Properties
		public override int MinimumVersion => 121;

		public override string Name => "createaccount";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CreateAccountInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
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

		protected override CreateAccountResult DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			var resultText = result.MustHaveString("result");
			resultText = string.Equals(resultText, "needtoken", StringComparison.Ordinal) ? "NeedToken" : resultText.UpperFirst(CultureInfo.InvariantCulture);
			return new CreateAccountResult(
				result: resultText,
				captchaData: result["captcha"].GetStringDictionary<string>(),
				token: (string?)result["token"],
				userId: (long?)result["userid"] ?? 0,
				userName: (string?)result["username"],
				warnings: result["warnings"].GetWarnings());
		}
		#endregion
	}
}