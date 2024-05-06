namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionEmailUser : ActionModule<EmailUserInput, EmailUserResult>
	{
		#region Constructors
		public ActionEmailUser(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 113;

		public override string Name => "emailuser";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, EmailUserInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			request
				.Add("target", input.Target)
				.Add("text", input.Text)
				.AddIfNotNull("subject", input.Subject)
				.Add("ccme", input.CCMe)
				.AddHidden("token", input.Token);
		}

		protected override EmailUserResult DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			return new EmailUserResult(
				result: result.MustHaveString("result"),
				message: (string?)result["message"]);
		}
		#endregion
	}
}