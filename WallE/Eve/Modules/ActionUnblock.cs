namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

// MWVERSION: 1.29
internal sealed class ActionUnblock(WikiAbstractionLayer wal) : ActionModule<UnblockInput, UnblockResult>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 112;

	public override string Name => "unblock";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, UnblockInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfPositive("id", input.Id)
			.AddIfPositive("userid", input.UserId)
			.AddIfNotNull("user", input.User)
			.AddIfNotNull("reason", input.Reason)
			.Add("tags", input.Tags)
			.AddHidden("token", input.Token);
	}

	protected override UnblockResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);

		string user;
		long userId;
		var userNode = result.MustHave("user");
		if (userNode.Type == JTokenType.Object)
		{
			// Deals with https://phabricator.wikimedia.org/T45518 in MW 1.18 and early versions of 1.19/1.20
			user = userNode.MustHaveString("mName");
			userId = (long?)userNode["mId"] ?? 0;
		}
		else
		{
			user = (string?)userNode ?? string.Empty;
			userId = (long?)result["userid"] ?? 0;
		}

		return new UnblockResult(
			id: (long)result.MustHave("id"),
			user: user,
			userId: userId,
			reason: (string?)result["reason"]);
	}
	#endregion
}