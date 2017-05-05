#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	// MWVERSION: 1.28
	public class ActionResetPassword : ActionModule<ResetPasswordInput, ResetPasswordResult>
	{
		#region Constructors
		public ActionResetPassword(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 127;

		public override string Name { get; } = "resetpassword";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, ResetPasswordInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("user", input.User)
				.AddIfNotNull("email", input.Email)
				.Add("capture", input.Capture)
				.AddHidden("token", input.Token);
		}

		protected override ResetPasswordResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new ResetPasswordResult()
			{
				Status = (string)result["status"],
				Passwords = result["passwords"].AsReadOnlyDictionary<string, string>(),
			};
			return output;
		}
		#endregion
	}
}
