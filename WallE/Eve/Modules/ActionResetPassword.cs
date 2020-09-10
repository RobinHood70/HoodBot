#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.28
	internal sealed class ActionResetPassword : ActionModule<ResetPasswordInput, ResetPasswordResult>
	{
		#region Constructors
		public ActionResetPassword(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 127;

		public override string Name => "resetpassword";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
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

		protected override ResetPasswordResult DeserializeResult(JToken? result)
		{
			ThrowNull(result, nameof(result));
			return new ResetPasswordResult(
				status: result.MustHaveString("status"),
				passwords: result["passwords"].GetStringDictionary<string>());
		}
		#endregion
	}
}
