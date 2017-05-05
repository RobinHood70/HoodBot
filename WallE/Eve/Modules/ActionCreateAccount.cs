#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	public class ActionCreateAccount : ActionModule<CreateAccountInput, CreateAccountResult>
	{
		#region Fields
		private string token;
		#endregion

		#region Constructors
		public ActionCreateAccount(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, string> CaptchaData { get; private set; } = EmptyReadOnlyDictionary<string, string>();

		public Dictionary<string, string> CaptchaSolution { get; } = new Dictionary<string, string>();
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
				.AddHidden("token", this.token);

			foreach (var kvp in this.CaptchaSolution)
			{
				request.AddHidden(kvp.Key, kvp.Value);
			}
		}

		protected override CreateAccountResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new CreateAccountResult()
			{
				UserName = (string)result["username"],
				UserId = (long?)result["userid"] ?? 0,
				Result = (string)result["result"],
				Warnings = result["warnings"].GetWarnings(),
			};

			// Only replace it if non-null, as we seem to be gettina NeedToken/NeedCaptcha loop otherwise.
			if (result["token"] != null)
			{
				this.token = (string)result["token"];
			}

			this.CaptchaData = result.AsReadOnlyDictionary<string, string>("captcha");
			return output;
		}
		#endregion
	}
}