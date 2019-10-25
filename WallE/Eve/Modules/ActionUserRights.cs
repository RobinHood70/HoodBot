#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionUserRights : ActionModuleValued<UserRightsInput, UserRightsResult>
	{
		#region Constructors
		public ActionUserRights(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 116;

		public override string Name => "userrights";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UserRightsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("user", input.User)
				.AddIfPositiveIf("userid", input.UserId, this.SiteVersion >= 123)
				.Add("add", input.Add)
				.Add("remove", input.Remove)
				.AddIfNotNull("reason", input.Reason)
				.AddHidden("token", input.Token);
		}

		protected override UserRightsResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new UserRightsResult(
				user: result.MustHaveString("user"),
				userId: (long?)result["userid"] ?? 0,
				added: result["added"].ToReadOnlyList<string>(),
				removed: result["removed"].ToReadOnlyList<string>());
		}
		#endregion
	}
}
