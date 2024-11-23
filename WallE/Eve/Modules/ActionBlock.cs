namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;
using RobinHood70.WallE.Properties;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionBlock(WikiAbstractionLayer wal) : ActionModule<BlockInput, BlockResult>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 112;

	public override string Name => "block";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, BlockInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("user", input.User)
			.AddIfPositiveIf("userid", input.UserId, this.SiteVersion >= 129)
			.Add("expiry", input.Expiry)
			.AddIfNotNullIf("expiry", input.ExpiryRelative, input.Expiry == null)
			.AddIfNotNull("reason", input.Reason)
			.Add("anononly", input.Flags.HasAnyFlag(BlockFlags.AnonymousOnly))
			.Add("nocreate", input.Flags.HasAnyFlag(BlockFlags.NoCreate))
			.Add("autoblock", input.Flags.HasAnyFlag(BlockFlags.AutoBlock))
			.Add("noemail", input.Flags.HasAnyFlag(BlockFlags.NoEmail))
			.Add("hidename", input.Flags.HasAnyFlag(BlockFlags.Hidden))
			.Add("allowusertalk", input.Flags.HasAnyFlag(BlockFlags.AllowUserTalk))
			.Add("reblock", input.Reblock)
			.Add("watchuser", input.WatchUser)
			.AddHidden("token", input.Token);
	}

	protected override BlockResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new BlockResult(
			user: result.MustHaveString("user"),
			userId: (long)result.MustHave("userID"),
			reason: result.MustHaveString("reason"),
			expiry: result["expiry"].GetNullableDate(),
			id: string.IsNullOrEmpty((string?)result["id"]) ? 0 : (long?)result["id"] ?? 0,
			flags: result.GetFlags(
				("allowusertalk", BlockFlags.AllowUserTalk),
				("anononly", BlockFlags.AnonymousOnly),
				("autoblock", BlockFlags.AutoBlock),
				("hidename", BlockFlags.Hidden),
				("nocreate", BlockFlags.NoCreate),
				("noemail", BlockFlags.NoEmail)),
			watchUser: result["watchuser"].GetBCBool());
	}

	protected override BlockResult DeserializeCustom(string? result) =>
		result?.Contains("must be an instance of Block", StringComparison.OrdinalIgnoreCase) == true
			? throw WikiException.General("reblock-failed", EveMessages.ReblockFailed) // Throw a custom error, since MW 1.25 and under handle this incorrectly.
			: base.DeserializeCustom(result);
	#endregion
}