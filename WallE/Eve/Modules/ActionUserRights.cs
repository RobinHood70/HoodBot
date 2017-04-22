#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	public class ActionUserRights : ActionModule<UserRightsInput, UserRightsResult>
	{
		#region Constructors
		public ActionUserRights(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 116;

		public override string Name { get; } = "userrights";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
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
			var output = new UserRightsResult()
			{
				User = (string)result["user"],
				UserId = (long)result["userid"],
				Added = result["added"].AsReadOnlyList<string>(),
				Removed = result["removed"].AsReadOnlyList<string>(),
			};
			return output;
		}
		#endregion
	}
}
