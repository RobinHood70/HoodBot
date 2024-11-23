namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionUserRights(WikiAbstractionLayer wal) : ActionModule<UserRightsInput, UserRightsResult>(wal)
{
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
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("user", input.User)
			.AddIfPositiveIf("userid", input.UserId, this.SiteVersion >= 123)
			.Add("add", input.Add)
			.Add("remove", input.Remove)
			.AddIfNotNull("reason", input.Reason)
			.AddHidden("token", input.Token);
	}

	protected override UserRightsResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new UserRightsResult(
			user: result.MustHaveString("user"),
			userId: (long?)result["userid"] ?? 0,
			added: result["added"].GetList<string>(),
			removed: result["removed"].GetList<string>());
	}
	#endregion
}