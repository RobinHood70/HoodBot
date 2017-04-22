#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static Properties.EveMessages;
	using static RobinHood70.Globals;

	public class ActionBlock : ActionModule<BlockInput, BlockResult>
	{
		#region Constructors
		public ActionBlock(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "block";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
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
				.Add("anononly", input.AnonymousOnly)
				.Add("nocreate", input.NoCreate)
				.Add("autoblock", input.AutoBlock)
				.Add("noemail", input.NoEmail)
				.Add("hidename", input.HideName)
				.Add("allowusertalk", input.AllowUserTalk)
				.Add("reblock", input.Reblock)
				.Add("watchuser", input.WatchUser)
				.AddHidden("token", input.Token);
		}

		protected override BlockResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new BlockResult()
			{
				User = (string)result["user"],
				UserId = (long)result["userID"],
				Expiry = result["expiry"].AsDate(),
			};
			var id = (string)result["id"];
			output.Id = string.IsNullOrEmpty(id) ? 0 : (long)result["id"];
			output.Reason = (string)result["reason"];
			output.Flags =
				result.GetFlag("allowusertalk", BlockUserFlags.AllowUserTalk) |
				result.GetFlag("anononly", BlockUserFlags.AnonymousOnly) |
				result.GetFlag("autoblock", BlockUserFlags.AutoBlock) |
				result.GetFlag("hidename", BlockUserFlags.HideName) |
				result.GetFlag("nocreate", BlockUserFlags.NoCreate) |
				result.GetFlag("noemail", BlockUserFlags.NoEmail) |
				result.GetFlag("watchuser", BlockUserFlags.WatchUser);

			return output;
		}

		protected override BlockResult DeserializeCustom(string result)
		{
			// Throw a custom error, since MW 1.25 and under handle this incorrectly.
			if (result != null && result.Contains("must be an instance of Block"))
			{
				throw WikiException.General("reblock-failed", ReblockFailed);
			}

			return base.DeserializeCustom(result);
		}
		#endregion
	}
}