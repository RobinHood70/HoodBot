#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	public class ActionEmailUser : ActionModule<EmailUserInput, EmailUserResult>
	{
		#region Constructors
		public ActionEmailUser(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 113;

		public override string Name { get; } = "emailuser";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, EmailUserInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("target", input.Target)
				.Add("text", input.Text)
				.AddIfNotNull("subject", input.Subject)
				.Add("ccme", input.CCMe)
				.AddHidden("token", input.Token);
		}

		protected override EmailUserResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new EmailUserResult()
			{
				Result = (string)result["result"],
				Message = (string)result["message"],
			};
			return output;
		}
		#endregion
	}
}
