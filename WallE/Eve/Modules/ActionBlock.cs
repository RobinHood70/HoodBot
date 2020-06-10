#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ActionBlock : ActionModule<BlockInput, BlockResult>
	{
		#region Constructors
		public ActionBlock(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

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
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("user", input.User)
				.AddIfPositiveIf("userid", input.UserId, this.SiteVersion >= 129)
				.Add("expiry", input.Expiry)
				.AddIfNotNullIf("expiry", input.ExpiryRelative, input.Expiry == null)
				.AddIfNotNull("reason", input.Reason)
				.Add("anononly", input.Flags.HasFlag(BlockFlags.AnonymousOnly))
				.Add("nocreate", input.Flags.HasFlag(BlockFlags.NoCreate))
				.Add("autoblock", input.Flags.HasFlag(BlockFlags.AutoBlock))
				.Add("noemail", input.Flags.HasFlag(BlockFlags.NoEmail))
				.Add("hidename", input.Flags.HasFlag(BlockFlags.Hidden))
				.Add("allowusertalk", input.Flags.HasFlag(BlockFlags.AllowUserTalk))
				.Add("reblock", input.Reblock)
				.Add("watchuser", input.WatchUser)
				.AddHidden("token", input.Token);
		}

		protected override BlockResult DeserializeResult(JToken? result)
		{
			ThrowNull(result, nameof(result));
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

		protected override BlockResult DeserializeCustom(string? result)
		{
			// Throw a custom error, since MW 1.25 and under handle this incorrectly.
			if (result != null && result.Contains("must be an instance of Block", StringComparison.OrdinalIgnoreCase))
			{
				throw WikiException.General("reblock-failed", EveMessages.ReblockFailed);
			}

			return base.DeserializeCustom(result);
		}
		#endregion
	}
}